using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Integration.Controllers;

/// <summary>
///     End-to-end integration tests for the <c>/users</c> endpoints.
/// </summary>
public class UsersControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] AllModuleTypes =
    [
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice",
        "Example", "Important", "Tip", "Homework", "Question",
        "ChordTablature", "FreeText"
    ];

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var mockBlob = new Mock<IAzureBlobService>();
        mockBlob
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.blob.core.windows.net/container/avatars/test");
        mockBlob
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = TestJwtSecret,
                    ["Jwt:Issuer"] = "test",
                    ["Jwt:Audience"] = "test",
                    ["Jwt:AccessTokenExpiryMinutes"] = "15",
                    ["Jwt:RefreshTokenExpiryDays"] = "7",
                    ["Jwt:RememberMeExpiryDays"] = "30",
                    ["Google:ClientId"] = "test.apps.googleusercontent.com",
                    ["AzureBlob:ConnectionString"] = "UseDevelopmentStorage=true",
                    ["AzureBlob:ContainerName"] = "test",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                    ["RateLimit:AuthMaxRequests"] = "1000",
                    ["RateLimit:AuthWindowSeconds"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
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

                services.RemoveAll<IPasswordHasher>();
                services.AddSingleton<IPasswordHasher, UsersTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, UsersNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, UsersNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, UsersNoOpSystemStylePresetSeeder>();

                services.RemoveAll<IAzureBlobService>();
                services.AddSingleton<IAzureBlobService>(mockBlob.Object);

                // AddAuth reads configuration eagerly at startup before ConfigureAppConfiguration
                // overrides are applied. PostConfigure overrides the JWT validation parameters
                // so tokens generated with the test key are accepted.
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
                    options.TokenValidationParameters.ValidIssuer = "test";
                    options.TokenValidationParameters.ValidAudience = "test";
                });
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    private static object MakeStylesPayload()
    {
        return AllModuleTypes.Select(t => new { moduleType = t, stylesJson = "{}" }).ToList();
    }

    /// <summary>Registers a unique user and returns (client, accessToken).</summary>
    private static async Task<(HttpClient Client, string Token)> RegisterAsync(WebApplicationFactory<Program> factory)
    {
        var client = CreateClient(factory);
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            DisplayName = "Test User",
            Password = "Password1!"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return (client, body.AccessToken);
    }

    // ── GET /users/me ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_Returns200WithCorrectShape()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("email", out _));
        Assert.True(root.TryGetProperty("firstName", out _));
        Assert.True(root.TryGetProperty("language", out var lang));
        Assert.Equal("en", lang.GetString());
    }

    // ── PUT /users/me ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_Returns200AndPersists()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.PutAsJsonAsync("/users/me", new
        {
            firstName = "Updated",
            lastName = "Name",
            language = "hu",
            defaultPageSize = (string?)null,
            defaultInstrumentId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("Updated", root.GetProperty("firstName").GetString());
        Assert.Equal("hu", root.GetProperty("language").GetString());

        // Verify persisted with a second GET
        var get = await client.GetAsync("/users/me");
        var getJson = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("Updated", getJson.RootElement.GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithMissingField_Returns400()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        // Missing required Language field
        var response = await client.PutAsJsonAsync("/users/me", new
        {
            firstName = "Test",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── DELETE /users/me ──────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleDeletion_Returns204WithScheduledDeletionAt()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.DeleteAsync("/users/me");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify scheduledDeletionAt is now set
        var get = await client.GetAsync("/users/me");
        var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        var deletionAt = json.RootElement.GetProperty("scheduledDeletionAt");
        Assert.NotEqual(JsonValueKind.Null, deletionAt.ValueKind);
    }

    [Fact]
    public async Task ScheduleDeletion_WhenAlreadyScheduled_Returns409()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        await client.DeleteAsync("/users/me");
        var response = await client.DeleteAsync("/users/me");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("ACCOUNT_DELETION_ALREADY_SCHEDULED", body!.Code);
    }

    // ── POST /users/me/cancel-deletion ────────────────────────────────────

    [Fact]
    public async Task CancelDeletion_Returns204()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        await client.DeleteAsync("/users/me");
        var response = await client.PostAsync("/users/me/cancel-deletion", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await client.GetAsync("/users/me");
        var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("scheduledDeletionAt").ValueKind);
    }

    [Fact]
    public async Task CancelDeletion_WhenNotScheduled_Returns400()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.PostAsync("/users/me/cancel-deletion", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("ACCOUNT_DELETION_NOT_SCHEDULED", body!.Code);
    }

    // ── PUT /users/me/avatar ─────────────────────────────────────────────

    [Fact]
    public async Task UploadAvatar_InvalidMimeType_Returns400()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "File", "test.pdf");

        var response = await client.PutAsync("/users/me/avatar", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_ExactlyTwoMegabytes_Returns200()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[2_097_152]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "File", "avatar.png");

        var response = await client.PutAsync("/users/me/avatar", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_OneByteOverLimit_Returns400()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[2_097_153]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "File", "avatar.png");

        var response = await client.PutAsync("/users/me/avatar", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /users/me/presets ─────────────────────────────────────────────

    [Fact]
    public async Task GetPresets_Returns200WithEmptyArray()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.GetAsync("/users/me/presets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }

    // ── POST /users/me/presets ────────────────────────────────────────────

    [Fact]
    public async Task CreatePreset_Returns201()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var response = await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "My Preset",
            styles = MakeStylesPayload()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("My Preset", json.RootElement.GetProperty("name").GetString());
        Assert.Equal(12, json.RootElement.GetProperty("styles").GetArrayLength());
    }

    [Fact]
    public async Task CreatePreset_DuplicateName_Returns409()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "My Preset",
            styles = MakeStylesPayload()
        });

        var response = await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "My Preset",
            styles = MakeStylesPayload()
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = JsonSerializer.Deserialize<ErrorBody>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        Assert.Equal("DUPLICATE_PRESET_NAME", body!.Code);
    }

    // ── PUT /users/me/presets/{id} ────────────────────────────────────────

    [Fact]
    public async Task UpdatePreset_Returns200()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var create = await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "Original",
            styles = MakeStylesPayload()
        });
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/users/me/presets/{id}", new
        {
            name = "Renamed",
            styles = (object?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Renamed", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdatePreset_OtherUser_Returns403()
    {
        using var factory = CreateFactory();

        // User A creates a preset
        var (clientA, _) = await RegisterAsync(factory);
        var create = await clientA.PostAsJsonAsync("/users/me/presets", new
        {
            name = "User A Preset",
            styles = MakeStylesPayload()
        });
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        // User B tries to update it
        var (clientB, _) = await RegisterAsync(factory);
        var response = await clientB.PutAsJsonAsync($"/users/me/presets/{id}", new
        {
            name = "Hijacked",
            styles = (object?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── DELETE /users/me/presets/{id} ─────────────────────────────────────

    [Fact]
    public async Task DeletePreset_Returns204()
    {
        using var factory = CreateFactory();
        var (client, _) = await RegisterAsync(factory);

        var create = await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "To Delete",
            styles = MakeStylesPayload()
        });
        var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/users/me/presets/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var list = await client.GetAsync("/users/me/presets");
        var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }

    private record AuthBody(string AccessToken, int ExpiresIn);

    private record ErrorBody(string Code, string Message);
}

file sealed class UsersTestPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return $"TEST:{password}";
    }

    public bool Verify(string password, string hash)
    {
        return hash == $"TEST:{password}";
    }
}

file sealed class UsersNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class UsersNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class UsersNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}