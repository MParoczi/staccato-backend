using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domain.Services;
using DomainModels.Enums;
using EntityModels.Entities;
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

public class ExportsControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly Guid SeedInstrumentId = Guid.NewGuid();

    // ── Factory ───────────────────────────────────────────────────────────

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var mockBlob = new Mock<IAzureBlobService>();
        mockBlob
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("blob-path");

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
                services.AddSingleton<IPasswordHasher, ExportsTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, ExportsNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, ExportsNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, ExportsNoOpSystemStylePresetSeeder>();

                services.RemoveAll<IAzureBlobService>();
                services.AddSingleton<IAzureBlobService>(mockBlob.Object);

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                    options =>
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

    // ── Seed helpers ──────────────────────────────────────────────────────

    private static async Task SeedInstrumentAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Instruments.Add(new InstrumentEntity
        {
            Id = SeedInstrumentId,
            Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    ///     Registers a user, seeds an instrument and notebook, and returns
    ///     an authenticated client plus the notebook ID.
    /// </summary>
    private static async Task<(HttpClient Client, Guid NotebookId)> SetupAsync(
        WebApplicationFactory<Program> factory)
    {
        await SeedInstrumentAsync(factory);

        var client = CreateClient(factory);
        var email = $"{Guid.NewGuid()}@test.com";
        var regResp = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            DisplayName = "Test User",
            Password = "Password1!"
        });
        regResp.EnsureSuccessStatusCode();
        var authBody = await regResp.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authBody!.AccessToken);

        // Need a default style preset for notebook creation
        await SeedDefaultPresetAsync(factory);

        // Create a notebook to export
        var nbResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Export Test Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#123456"
        });
        nbResp.EnsureSuccessStatusCode();
        var nbJson = JsonDocument.Parse(await nbResp.Content.ReadAsStringAsync());
        var notebookId = Guid.Parse(nbJson.RootElement.GetProperty("id").GetString()!);

        return (client, notebookId);
    }

    private static async Task SeedDefaultPresetAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.SystemStylePresets.AnyAsync()) return;

        var moduleTypes = Enum.GetNames<ModuleType>();
        var stylesJson = JsonSerializer.Serialize(moduleTypes.Select(mt => new
        {
            moduleType = mt,
            backgroundColor = "#ffffff",
            borderColor = "#cccccc",
            borderStyle = "Solid",
            borderWidth = 1,
            borderRadius = 4,
            headerBgColor = "#eeeeee",
            headerTextColor = "#111111",
            bodyTextColor = "#222222",
            fontFamily = "Default"
        }).ToList());

        db.SystemStylePresets.Add(new SystemStylePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Classic",
            DisplayOrder = 1,
            IsDefault = true,
            StylesJson = stylesJson
        });

        await db.SaveChangesAsync();
    }

    // ── POST /exports ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateExport_Returns202WithExportId()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        var resp = await client.PostAsJsonAsync("/exports", new
        {
            notebookId
        });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("exportId", out var idProp));
        Assert.True(Guid.TryParse(idProp.GetString(), out _));
        Assert.Equal("Pending", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateExport_WithActiveExport_Returns409()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        // First export succeeds
        var resp1 = await client.PostAsJsonAsync("/exports", new { notebookId });
        Assert.Equal(HttpStatusCode.Accepted, resp1.StatusCode);

        // Second export for same notebook conflicts
        var resp2 = await client.PostAsJsonAsync("/exports", new { notebookId });

        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
        var json = JsonDocument.Parse(await resp2.Content.ReadAsStringAsync());
        Assert.Equal("ACTIVE_EXPORT_EXISTS", json.RootElement.GetProperty("code").GetString());
    }

    // ── GET /exports/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetExport_Returns200WithExportDetails()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        var createResp = await client.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var exportId = createJson.RootElement.GetProperty("exportId").GetString()!;

        var resp = await client.GetAsync($"/exports/{exportId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(exportId, json.RootElement.GetProperty("id").GetString());
        Assert.Equal(notebookId.ToString(), json.RootElement.GetProperty("notebookId").GetString());
    }

    [Fact]
    public async Task GetExport_OtherUser_Returns403()
    {
        using var factory = CreateFactory();
        var (client1, notebookId) = await SetupAsync(factory);

        // Create export as user 1
        var createResp = await client1.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var exportId = createJson.RootElement.GetProperty("exportId").GetString()!;

        // Register user 2
        var client2 = CreateClient(factory);
        var regResp = await client2.PostAsJsonAsync("/auth/register", new
        {
            Email = $"{Guid.NewGuid()}@test.com",
            DisplayName = "Other User",
            Password = "Password1!"
        });
        regResp.EnsureSuccessStatusCode();
        var auth2 = await regResp.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth2!.AccessToken);

        // Try to access user 1's export as user 2
        var resp = await client2.GetAsync($"/exports/{exportId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /exports/{id}/download ────────────────────────────────────────

    [Fact]
    public async Task DownloadExport_NotReady_Returns404()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        var createResp = await client.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var exportId = createJson.RootElement.GetProperty("exportId").GetString()!;

        // Immediately try to download (status is Pending, not Ready)
        var resp = await client.GetAsync($"/exports/{exportId}/download");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── GET /exports ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetExports_ReturnsUserExportsList()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        // Create an export
        var createResp = await client.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();

        var resp = await client.GetAsync("/exports");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }

    // ── DELETE /exports/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteExport_Returns204()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        var createResp = await client.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var exportId = createJson.RootElement.GetProperty("exportId").GetString()!;

        var resp = await client.DeleteAsync($"/exports/{exportId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteExport_OtherUser_Returns403()
    {
        using var factory = CreateFactory();
        var (client1, notebookId) = await SetupAsync(factory);

        var createResp = await client1.PostAsJsonAsync("/exports", new { notebookId });
        createResp.EnsureSuccessStatusCode();
        var createJson = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var exportId = createJson.RootElement.GetProperty("exportId").GetString()!;

        // Register user 2
        var client2 = CreateClient(factory);
        var regResp = await client2.PostAsJsonAsync("/auth/register", new
        {
            Email = $"{Guid.NewGuid()}@test.com",
            DisplayName = "Other User",
            Password = "Password1!"
        });
        regResp.EnsureSuccessStatusCode();
        var auth2 = await regResp.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth2!.AccessToken);

        var resp = await client2.DeleteAsync($"/exports/{exportId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── POST /exports with lessonIds ────────────────────────────────────

    [Fact]
    public async Task CreateExport_WithValidLessonIds_Returns202()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        // Create lessons
        var lesson1Resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons",
            new { title = "Lesson 1" });
        lesson1Resp.EnsureSuccessStatusCode();
        var lesson1Id = JsonDocument.Parse(await lesson1Resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var lesson2Resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons",
            new { title = "Lesson 2" });
        lesson2Resp.EnsureSuccessStatusCode();
        var lesson2Id = JsonDocument.Parse(await lesson2Resp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PostAsJsonAsync("/exports", new
        {
            notebookId,
            lessonIds = new[] { lesson1Id, lesson2Id }
        });

        Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(Guid.TryParse(json.RootElement.GetProperty("exportId").GetString(), out _));
    }

    [Fact]
    public async Task CreateExport_WithInvalidLessonIds_Returns400()
    {
        using var factory = CreateFactory();
        var (client, notebookId) = await SetupAsync(factory);

        var resp = await client.PostAsJsonAsync("/exports", new
        {
            notebookId,
            lessonIds = new[] { Guid.NewGuid().ToString() }
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_LESSON_IDS", json.RootElement.GetProperty("code").GetString());
    }

    private record AuthBody(string AccessToken, int ExpiresIn);
}

// ── No-op seeders ─────────────────────────────────────────────────────────────

file sealed class ExportsNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class ExportsNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class ExportsNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class ExportsTestPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}
