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

/// <summary>
///     End-to-end integration tests for the <c>/notebooks</c> endpoints.
/// </summary>
public class NotebooksControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // The 12 ModuleType names expected in every notebook
    private static readonly string[] AllModuleTypes =
    [
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice", "Example",
        "Important", "Tip", "Homework", "Question", "ChordTablature", "FreeText"
    ];

    // Seeded instrument for all tests that need one
    private static readonly Guid SeedInstrumentId = Guid.NewGuid();

    // ── Factory ───────────────────────────────────────────────────────────

    private static WebApplicationFactory<Program> CreateFactory()
    {
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
                services.AddSingleton<IPasswordHasher, NotebooksTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, NotebooksNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, NotebooksNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, NotebooksNoOpSystemStylePresetSeeder>();

                services.RemoveAll<IAzureBlobService>();
                services.AddSingleton<IAzureBlobService>(new Mock<IAzureBlobService>().Object);

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

    private static async Task SeedColorfulPresetAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stylesJson = JsonSerializer.Serialize(
            AllModuleTypes.Select(mt => new
            {
                moduleType = mt,
                backgroundColor = "#ffffff",
                borderColor = "#000000",
                borderStyle = "None",
                borderWidth = 0,
                borderRadius = 0,
                headerBgColor = "#eeeeee",
                headerTextColor = "#333333",
                bodyTextColor = "#000000",
                fontFamily = "Default"
            }).ToList());

        db.SystemStylePresets.Add(new SystemStylePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Colorful",
            DisplayOrder = 2,
            IsDefault = true,
            StylesJson = stylesJson
        });

        await db.SaveChangesAsync();
    }

    /// <summary>Seeds Classic as a non-default system preset and returns its ID.</summary>
    private static async Task<Guid> SeedClassicPresetAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var presetId = Guid.NewGuid();
        var stylesJson = JsonSerializer.Serialize(
            AllModuleTypes.Select(mt => new
            {
                moduleType = mt,
                backgroundColor = "#fdfaf4",
                borderColor = "#c9a84c",
                borderStyle = "Solid",
                borderWidth = 1,
                borderRadius = 2,
                headerBgColor = "#8b6914",
                headerTextColor = "#fffde7",
                bodyTextColor = "#2c1810",
                fontFamily = "Serif"
            }).ToList());

        db.SystemStylePresets.Add(new SystemStylePresetEntity
        {
            Id = presetId,
            Name = "Classic",
            DisplayOrder = 1,
            IsDefault = false,
            StylesJson = stylesJson
        });

        await db.SaveChangesAsync();
        return presetId;
    }

    /// <summary>Registers a unique user and returns an authenticated client.</summary>
    private static async Task<HttpClient> RegisterAsync(WebApplicationFactory<Program> factory)
    {
        var client = CreateClient(factory);
        var email = $"{Guid.NewGuid()}@test.com";
        var resp = await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            DisplayName = "Test User",
            Password = "Password1!"
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<AuthBody>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private static object BuildStylesPayload()
    {
        return AllModuleTypes.Select(mt => new
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
        }).ToList();
    }

    // ── GET /notebooks ────────────────────────────────────────────────────

    [Fact]
    public async Task GetNotebooks_Returns200WithEmptyArray_WhenNoNotebooks()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.GetAsync("/notebooks");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetNotebooks_Returns200OrderedByCreatedAtAsc()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        // Create two notebooks; ordering must be deterministic
        var r1 = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Alpha",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#111111"
        });
        r1.EnsureSuccessStatusCode();

        var r2 = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Beta",
            instrumentId = SeedInstrumentId,
            pageSize = "A5",
            coverColor = "#222222"
        });
        r2.EnsureSuccessStatusCode();

        var listResp = await client.GetAsync("/notebooks");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

        var json = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal("Alpha", json.RootElement[0].GetProperty("title").GetString());
        Assert.Equal("Beta", json.RootElement[1].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetNotebooks_Returns401_WhenUnauthenticated()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.GetAsync("/notebooks");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── POST /notebooks ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateNotebook_Returns201WithTwelveStyles_WhenNoStylesProvided()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "My Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ff0000"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("id", out _));
        Assert.Equal("My Notebook", root.GetProperty("title").GetString());
        Assert.Equal("A4", root.GetProperty("pageSize").GetString());
        Assert.Equal("#ff0000", root.GetProperty("coverColor").GetString());

        Assert.True(root.TryGetProperty("styles", out var styles));
        Assert.Equal(12, styles.GetArrayLength());
    }

    [Fact]
    public async Task CreateNotebook_Returns201WithProvidedStyles()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Custom Styled",
            instrumentId = SeedInstrumentId,
            pageSize = "A5",
            coverColor = "#0000ff",
            styles = BuildStylesPayload()
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Custom Styled", root.GetProperty("title").GetString());
        Assert.Equal(12, root.GetProperty("styles").GetArrayLength());
    }

    [Fact]
    public async Task CreateNotebook_Returns400_WhenTitleMissing()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ff0000"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateNotebook_Returns400_WhenCoverColorInvalid()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "not-a-hex"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateNotebook_Returns422_WhenInstrumentIdNotFound()
    {
        using var factory = CreateFactory();
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Test",
            instrumentId = Guid.NewGuid(), // does not exist
            pageSize = "A4",
            coverColor = "#ff0000"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    // ── GET /notebooks/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetNotebook_Returns200WithStyles()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Detail Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#abcdef"
        });
        createResp.EnsureSuccessStatusCode();
        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var notebookId = created.RootElement.GetProperty("id").GetString()!;

        var resp = await client.GetAsync($"/notebooks/{notebookId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Detail Test", root.GetProperty("title").GetString());
        Assert.Equal(notebookId, root.GetProperty("id").GetString());
        Assert.Equal(12, root.GetProperty("styles").GetArrayLength());
    }

    [Fact]
    public async Task GetNotebook_Returns403_WhenNotOwnedByUser()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);

        // User A creates a notebook
        var clientA = await RegisterAsync(factory);
        var createResp = await clientA.PostAsJsonAsync("/notebooks", new
        {
            title = "User A Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#123456"
        });
        createResp.EnsureSuccessStatusCode();
        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var notebookId = created.RootElement.GetProperty("id").GetString()!;

        // User B tries to access it
        var clientB = await RegisterAsync(factory);
        var resp = await clientB.GetAsync($"/notebooks/{notebookId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetNotebook_Returns404_WhenNotFound()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.GetAsync($"/notebooks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── PUT /notebooks/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotebook_Returns200WithUpdatedValues()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Original Title",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#111111"
        });
        createResp.EnsureSuccessStatusCode();
        var created = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var notebookId = created.RootElement.GetProperty("id").GetString()!;

        var updateResp = await client.PutAsJsonAsync($"/notebooks/{notebookId}", new
        {
            title = "Updated Title",
            coverColor = "#aabbcc"
        });

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var json = JsonDocument.Parse(await updateResp.Content.ReadAsStringAsync());
        Assert.Equal("Updated Title", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("#aabbcc", json.RootElement.GetProperty("coverColor").GetString());
    }

    [Fact]
    public async Task UpdateNotebook_Returns400_WhenPageSizeIncluded()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var updateResp = await client.PutAsJsonAsync($"/notebooks/{Guid.NewGuid()}", new
        {
            title = "Test",
            coverColor = "#ffffff",
            pageSize = "A5" // immutable field — must return 400
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
    }

    [Fact]
    public async Task UpdateNotebook_Returns400_WhenInstrumentIdIncluded()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var updateResp = await client.PutAsJsonAsync($"/notebooks/{Guid.NewGuid()}", new
        {
            title = "Test",
            coverColor = "#ffffff",
            instrumentId = Guid.NewGuid() // immutable field — must return 400
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
    }

    [Fact]
    public async Task UpdateNotebook_Returns403_WhenNotOwnedByUser()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);

        var clientA = await RegisterAsync(factory);
        var createResp = await clientA.PostAsJsonAsync("/notebooks", new
        {
            title = "User A Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#123456"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var clientB = await RegisterAsync(factory);
        var updateResp = await clientB.PutAsJsonAsync($"/notebooks/{notebookId}", new
        {
            title = "Hijacked",
            coverColor = "#ffffff"
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateResp.StatusCode);
    }

    [Fact]
    public async Task UpdateNotebook_Returns404_WhenNotFound()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var updateResp = await client.PutAsJsonAsync($"/notebooks/{Guid.NewGuid()}", new
        {
            title = "Test",
            coverColor = "#ffffff"
        });

        Assert.Equal(HttpStatusCode.NotFound, updateResp.StatusCode);
    }

    // ── DELETE /notebooks/{id} ────────────────────────────────────────────

    [Fact]
    public async Task DeleteNotebook_Returns204()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "To Delete",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ff0000"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var deleteResp = await client.DeleteAsync($"/notebooks/{notebookId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verify notebook is gone
        var getResp = await client.GetAsync($"/notebooks/{notebookId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteNotebook_Returns409_WhenActiveExportExists()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        // Create notebook
        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Export Active",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ff0000"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookIdStr = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
        var notebookId = Guid.Parse(notebookIdStr);

        // Get current user ID from profile
        var profileResp = await client.GetAsync("/users/me");
        var userId = Guid.Parse(
            JsonDocument.Parse(await profileResp.Content.ReadAsStringAsync())
                .RootElement.GetProperty("id").GetString()!);

        // Seed an active (Pending) export directly
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PdfExports.Add(new PdfExportEntity
            {
                Id = Guid.NewGuid(),
                NotebookId = notebookId,
                UserId = userId,
                Status = ExportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var deleteResp = await client.DeleteAsync($"/notebooks/{notebookId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
    }

    // ── GET /notebooks/{id}/styles ────────────────────────────────────────

    [Fact]
    public async Task GetStyles_Returns200WithTwelveItems()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Styled",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.GetAsync($"/notebooks/{notebookId}/styles");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(12, json.RootElement.GetArrayLength());
    }

    // ── PUT /notebooks/{id}/styles ────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateStyles_Returns200WithUpdatedStyles()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Style Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        // Build replacement styles with distinct color
        var newStyles = AllModuleTypes.Select(mt => new
        {
            moduleType = mt,
            backgroundColor = "#123456",
            borderColor = "#654321",
            borderStyle = "Solid",
            borderWidth = 2,
            borderRadius = 8,
            headerBgColor = "#aabbcc",
            headerTextColor = "#112233",
            bodyTextColor = "#334455",
            fontFamily = "Default"
        }).ToList();

        var putResp = await client.PutAsJsonAsync($"/notebooks/{notebookId}/styles", newStyles);

        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
        var json = JsonDocument.Parse(await putResp.Content.ReadAsStringAsync());
        Assert.Equal(12, json.RootElement.GetArrayLength());
        // Verify one of the updated colours persisted
        var first = json.RootElement[0];
        Assert.Equal("#123456", first.GetProperty("backgroundColor").GetString());
    }

    [Fact]
    public async Task BulkUpdateStyles_Returns400_WhenNotTwelveItems()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        // Only 3 style items — invalid
        var badStyles = AllModuleTypes.Take(3).Select(mt => new
        {
            moduleType = mt,
            backgroundColor = "#ffffff",
            borderColor = "#000000",
            borderStyle = "None",
            borderWidth = 0,
            borderRadius = 0,
            headerBgColor = "#eeeeee",
            headerTextColor = "#333333",
            bodyTextColor = "#000000",
            fontFamily = "Default"
        }).ToList();

        var resp = await client.PutAsJsonAsync($"/notebooks/{Guid.NewGuid()}/styles", badStyles);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── POST /notebooks/{id}/styles/apply-preset/{presetId} ───────────────

    [Fact]
    public async Task ApplyPreset_Returns200_WithSystemPreset()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var classicId = await SeedClassicPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Preset Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PostAsync(
            $"/notebooks/{notebookId}/styles/apply-preset/{classicId}", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(12, json.RootElement.GetArrayLength());
        // Classic uses Serif — verify it was applied
        Assert.Equal("Serif", json.RootElement[0].GetProperty("fontFamily").GetString());
    }

    [Fact]
    public async Task ApplyPreset_Returns200_WithUserSavedPreset()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        // Create a notebook
        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "User Preset Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        // Create a user-saved preset (StyleEntryDto format: {moduleType, stylesJson})
        var userStylesJson = JsonSerializer.Serialize(
            AllModuleTypes.Select(mt => new
            {
                moduleType = mt,
                stylesJson =
                    @"{""backgroundColor"":""#aabbcc"",""borderColor"":""#000"",""borderStyle"":""None"",""borderWidth"":0,""borderRadius"":0,""headerBgColor"":""#eee"",""headerTextColor"":""#333"",""bodyTextColor"":""#000"",""fontFamily"":""Monospace""}"
            }).ToList());

        var presetResp = await client.PostAsJsonAsync("/users/me/presets", new
        {
            name = "My Custom Preset",
            styles = JsonSerializer.Deserialize<object>(userStylesJson)
        });
        presetResp.EnsureSuccessStatusCode();
        var presetId = JsonDocument.Parse(await presetResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PostAsync(
            $"/notebooks/{notebookId}/styles/apply-preset/{presetId}", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(12, json.RootElement.GetArrayLength());
        Assert.Equal("Monospace", json.RootElement[0].GetProperty("fontFamily").GetString());
    }

    [Fact]
    public async Task ApplyPreset_Returns403_WhenNotebookNotOwnedByUser()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var classicId = await SeedClassicPresetAsync(factory);

        // User A creates notebook
        var clientA = await RegisterAsync(factory);
        var createResp = await clientA.PostAsJsonAsync("/notebooks", new
        {
            title = "User A Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        // User B tries to apply a preset to User A's notebook
        var clientB = await RegisterAsync(factory);
        var resp = await clientB.PostAsync(
            $"/notebooks/{notebookId}/styles/apply-preset/{classicId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task ApplyPreset_Returns404_WhenPresetNotFound()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var createResp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Test",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
            coverColor = "#ffffff"
        });
        createResp.EnsureSuccessStatusCode();
        var notebookId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PostAsync(
            $"/notebooks/{notebookId}/styles/apply-preset/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private record AuthBody(string AccessToken, int ExpiresIn);
}

// ── No-op seeders ─────────────────────────────────────────────────────────────

file sealed class NotebooksNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class NotebooksNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class NotebooksNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class NotebooksTestPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return $"hashed:{password}";
    }

    public bool Verify(string password, string hash)
    {
        return hash == $"hashed:{password}";
    }
}