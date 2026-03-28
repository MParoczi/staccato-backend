using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Integration.Controllers;

/// <summary>
///     End-to-end integration tests for the <c>/auth</c> endpoints.
///     Each test creates its own <see cref="WebApplicationFactory{Program}" /> with an isolated
///     InMemory database and a fast password hasher so BCrypt doesn't slow the suite.
/// </summary>
public class AuthControllerTests
{
    // ── helpers ───────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static WebApplicationFactory<Program> CreateFactory(int rateLimit = 100) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"]               = "test-secret-key-must-be-at-least-32-chars!!",
                    ["Jwt:Issuer"]                  = "test",
                    ["Jwt:Audience"]                = "test",
                    ["Jwt:AccessTokenExpiryMinutes"] = "15",
                    ["Jwt:RefreshTokenExpiryDays"]  = "7",
                    ["Jwt:RememberMeExpiryDays"]    = "30",
                    ["Google:ClientId"]             = "test.apps.googleusercontent.com",
                    ["AzureBlob:ConnectionString"]  = "UseDevelopmentStorage=true",
                    ["AzureBlob:ContainerName"]     = "test",
                    ["Cors:AllowedOrigins:0"]       = "http://localhost:3000",
                    ["RateLimit:AuthMaxRequests"]   = rateLimit.ToString(),
                    ["RateLimit:AuthWindowSeconds"] = "60",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace the SQL Server DbContextOptions with an InMemory equivalent.
                // Using UseInternalServiceProvider prevents EF Core from scanning the DI
                // container for provider services (which would find both SqlServer and InMemory
                // and throw "Only a single database provider can be registered").
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                var internalProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                var dbName = Guid.NewGuid().ToString();
                services.AddSingleton<DbContextOptions<AppDbContext>>(
                    new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .UseInternalServiceProvider(internalProvider)
                        .Options);

                // Replace BCrypt with a fast hasher so tests don't take 10+ seconds.
                services.RemoveAll<IPasswordHasher>();
                services.AddSingleton<IPasswordHasher, TestPasswordHasher>();

                // Replace seeders with no-ops so chord/instrument data files are never touched.
                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, NoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, NoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, NoOpSystemStylePresetSeeder>();
            });
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies     = false,
        });

    /// <summary>Registers a user and returns the raw <c>staccato_refresh</c> cookie value.</summary>
    private static async Task<string> RegisterAndGetCookie(
        HttpClient client, string email, string password = "Password1!")
    {
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email       = email,
            DisplayName = "Test User",
            Password    = password,
        });
        response.EnsureSuccessStatusCode();
        return ExtractRefreshCookieValue(response)!;
    }

    /// <summary>Logs in and returns the raw <c>staccato_refresh</c> cookie value.</summary>
    private static async Task<string> LoginAndGetCookie(
        HttpClient client, string email, string password = "Password1!", bool rememberMe = false)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email      = email,
            Password   = password,
            RememberMe = rememberMe,
        });
        response.EnsureSuccessStatusCode();
        return ExtractRefreshCookieValue(response)!;
    }

    private static string? ExtractRefreshCookieValue(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("staccato_refresh="));
        if (setCookie is null) return null;
        // Format: staccato_refresh=<value>; path=/; httponly; ...
        return setCookie.Split(';')[0].Split('=', 2)[1];
    }

    private static DateTime? ExtractCookieExpiry(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("staccato_refresh="));
        if (setCookie is null) return null;
        var expiresPart = setCookie.Split(';')
            .Select(p => p.Trim())
            .FirstOrDefault(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));
        if (expiresPart is null) return null;
        var dateStr = expiresPart["expires=".Length..];
        return DateTime.TryParse(dateStr, out var dt) ? dt : null;
    }

    private static HttpRequestMessage WithCookie(HttpMethod method, string url, string cookieValue)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("Cookie", $"staccato_refresh={cookieValue}");
        return req;
    }

    private record ErrorBody(string Code, string Message);
    private record AuthBody(string AccessToken, int ExpiresIn);

    // ── Register ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns201WithCookie()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email       = "register-valid@example.com",
            DisplayName = "Test User",
            Password    = "Password1!",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.True(body.ExpiresIn > 0);

        Assert.NotNull(ExtractRefreshCookieValue(response));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string email = "dup@example.com";

        await RegisterAndGetCookie(client, email);

        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email       = email,
            DisplayName = "Another User",
            Password    = "Password1!",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", body!.Code);
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        // Missing required fields → FluentValidation fires
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email       = "not-an-email",
            DisplayName = "",
            Password    = "short",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("errors", out _),
            "FluentValidation should return an 'errors' object");
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithCookie()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string email = "login-valid@example.com";
        await RegisterAndGetCookie(client, email);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email    = email,
            Password = "Password1!",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(ExtractRefreshCookieValue(response));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string email = "login-wrong@example.com";
        await RegisterAndGetCookie(client, email);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email    = email,
            Password = "WrongPassword!",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("INVALID_CREDENTIALS", body!.Code);
    }

    [Fact]
    public async Task Login_RememberMe_SetsLongerCookie()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string email = "login-rememberme@example.com";
        await RegisterAndGetCookie(client, email);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            Email      = email,
            Password   = "Password1!",
            RememberMe = true,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expiry = ExtractCookieExpiry(response);
        Assert.NotNull(expiry);
        Assert.True((expiry!.Value - DateTime.UtcNow).TotalDays > 25,
            "RememberMe cookie should expire ~30 days out");
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidCookie_Returns200WithNewCookie()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        var oldCookie = await RegisterAndGetCookie(client, "refresh-valid@example.com");

        var response = await client.SendAsync(
            WithCookie(HttpMethod.Post, "/auth/refresh", oldCookie));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var newCookie = ExtractRefreshCookieValue(response);
        Assert.NotNull(newCookie);
        Assert.NotEqual(oldCookie, newCookie);
    }

    [Fact]
    public async Task Refresh_MissingCookie_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var response = await client.PostAsync("/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("INVALID_TOKEN", body!.Code);
    }

    [Fact]
    public async Task Refresh_RevokedToken_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        var originalCookie = await RegisterAndGetCookie(client, "refresh-revoked@example.com");

        // Use the token once → it becomes revoked, new token issued
        await client.SendAsync(WithCookie(HttpMethod.Post, "/auth/refresh", originalCookie));

        // Present the revoked (original) token again → theft detection
        var response = await client.SendAsync(
            WithCookie(HttpMethod.Post, "/auth/refresh", originalCookie));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("INVALID_TOKEN", body!.Code);
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ValidCookie_Returns204ClearedCookie()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        var cookie = await RegisterAndGetCookie(client, "logout-valid@example.com");

        var response = await client.SendAsync(
            WithCookie(HttpMethod.Delete, "/auth/logout", cookie));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Cookie should be cleared (Set-Cookie with empty value or max-age=0)
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var refreshCookie = setCookies.FirstOrDefault(c => c.StartsWith("staccato_refresh"));
            if (refreshCookie is not null)
            {
                var cookieValue = refreshCookie.Split(';')[0].Split('=', 2)[1];
                Assert.True(string.IsNullOrEmpty(cookieValue),
                    "Cookie value should be empty after logout");
            }
        }
    }

    [Fact]
    public async Task Logout_MissingCookie_Returns204()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var response = await client.DeleteAsync("/auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Rate Limiting ─────────────────────────────────────────────────────

    [Fact]
    public async Task RateLimit_ExceedsLimit_Returns429()
    {
        // Use a dedicated factory with a limit of 2 so we only need 3 requests
        using var factory = CreateFactory(rateLimit: 2);
        var client = CreateClient(factory);

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 3; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/auth/login", new
            {
                Email    = "ratelimit@example.com",
                Password = "any-password",
            });
        }

        Assert.Equal((HttpStatusCode)429, lastResponse!.StatusCode);
    }

    // ── Localization ──────────────────────────────────────────────────────

    [Fact]
    public async Task Localization_HungarianHeader_ReturnsHuMessage()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        const string email = "localization@example.com";
        await RegisterAndGetCookie(client, email);

        // Register again with the same email → 409; request in Hungarian
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/register");
        request.Headers.Add("Accept-Language", "hu");
        request.Content = JsonContent.Create(new
        {
            Email       = email,
            DisplayName = "Test User",
            Password    = "Password1!",
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", body!.Code);
        Assert.Equal("Ezzel az e-mail c\u00edmmel m\u00e1r l\u00e9tezik fi\u00f3k.", body.Message);
    }
}

/// <summary>
///     Fast password hasher for integration tests — avoids BCrypt's intentional 500ms delay.
///     Never use in production.
/// </summary>
file sealed class TestPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"TEST:{password}";
    public bool Verify(string password, string hash) => hash == $"TEST:{password}";
}

// No-op seeder stubs so the chord/instrument JSON files are never touched during tests.
file sealed class NoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class NoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class NoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
