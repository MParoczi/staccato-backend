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

public class ModulesControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private static readonly Guid SeedInstrumentId = Guid.NewGuid();

    private static readonly string[] AllModuleTypes =
    [
        "Title", "Breadcrumb", "Subtitle", "Theory", "Practice", "Example",
        "Important", "Tip", "Homework", "Question", "ChordTablature", "FreeText"
    ];

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
                services.AddSingleton<IPasswordHasher, ModulesTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, ModulesNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, ModulesNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, ModulesNoOpSystemStylePresetSeeder>();

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

    private static async Task<string> CreateNotebookAsync(HttpClient client, string pageSize = "A4")
    {
        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Test Notebook",
            instrumentId = SeedInstrumentId,
            pageSize,
            coverColor = "#ffffff"
        });
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private static async Task<(string LessonId, string FirstPageId)> CreateLessonAsync(
        HttpClient client, string notebookId)
    {
        var resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Test Lesson"
        });
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var lessonId = doc.RootElement.GetProperty("id").GetString()!;
        var firstPageId = doc.RootElement.GetProperty("pages")[0].GetProperty("id").GetString()!;
        return (lessonId, firstPageId);
    }

    private static object MakeCreateModuleBody(
        string moduleType = "Theory", int gridX = 0, int gridY = 0,
        int gridWidth = 18, int gridHeight = 10, int zIndex = 0)
    {
        return new
        {
            moduleType,
            gridX,
            gridY,
            gridWidth,
            gridHeight,
            zIndex,
            content = new object[0]
        };
    }

    // ── POST /pages/{pageId}/modules ────────────────────────────────────

    [Fact]
    public async Task CreateModule_HappyPath_Returns201()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 2, 5, 18, 10, 0));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("Theory", doc.RootElement.GetProperty("moduleType").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("gridX").GetInt32());
        Assert.Equal(5, doc.RootElement.GetProperty("gridY").GetInt32());
        Assert.Equal(18, doc.RootElement.GetProperty("gridWidth").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("gridHeight").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("zIndex").GetInt32());
        Assert.NotEqual(Guid.Empty.ToString(), doc.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task CreateModule_TooSmall_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // Theory min: 8x5, sending 6x3
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 0, 0, 6, 3));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_TOO_SMALL", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateModule_OutOfBounds_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // A4 grid: 42x59. Position 40 + width 8 = 48 > 42
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 40, 0, 8, 5));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_OUT_OF_BOUNDS", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateModule_Overlap_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // Create first module
        await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 0, 0, 20, 10));

        // Create overlapping module
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 10, 5, 15, 8));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_OVERLAP", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateModule_DuplicateTitle_Returns409()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, pageId) = await CreateLessonAsync(client, notebookId);

        // Create first Title module
        await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Title", 0, 0, 20, 4));

        // Add a second page and try another Title
        await client.PostAsync($"/lessons/{lessonId}/pages", null);
        var pagesResp = await client.GetAsync($"/lessons/{lessonId}/pages");
        var pagesDoc = JsonDocument.Parse(await pagesResp.Content.ReadAsStringAsync());
        var secondPageId = pagesDoc.RootElement[1].GetProperty("id").GetString()!;

        var resp = await client.PostAsJsonAsync($"/pages/{secondPageId}/modules",
            MakeCreateModuleBody("Title", 0, 0, 20, 4));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("DUPLICATE_TITLE_MODULE", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CreateModule_OtherUsersPage_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (_, pageId) = await CreateLessonAsync(ownerClient, notebookId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody());

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CreateModule_WithoutAuth_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.PostAsJsonAsync($"/pages/{Guid.NewGuid()}/modules",
            MakeCreateModuleBody());

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CreateModule_BreadcrumbContentNotEmpty_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // Send non-empty content — FluentValidation catches this (content must be empty array)
        var body = new
        {
            moduleType = "Breadcrumb",
            gridX = 0,
            gridY = 0,
            gridWidth = 20,
            gridHeight = 3,
            zIndex = 0,
            content = new[] { new { type = "Text", spans = new object[0] } }
        };
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules", body);

        // FluentValidation returns 400 for non-empty content on POST
        Assert.True(
            resp.StatusCode == HttpStatusCode.BadRequest ||
            resp.StatusCode == HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateModule_ValidBoundaryPlacement_Returns201()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // A4: 42x59. gridX(34) + gridWidth(8) = 42, exactly at boundary
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 34, 54, 8, 5));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── PUT /modules/{moduleId} ─────────────────────────────────────────

    private static async Task<string> CreateModuleAndGetId(
        HttpClient client, string pageId,
        string moduleType = "Theory", int gridX = 0, int gridY = 0,
        int gridWidth = 18, int gridHeight = 10)
    {
        var resp = await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody(moduleType, gridX, gridY, gridWidth, gridHeight));
        resp.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task UpdateModule_HappyPathWithContent_Returns200()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        var body = new
        {
            moduleType = "Theory",
            gridX = 2,
            gridY = 5,
            gridWidth = 18,
            gridHeight = 12,
            zIndex = 1,
            content = new object[]
            {
                new { type = "SectionHeading", spans = new[] { new { text = "Title", bold = false } } },
                new { type = "Text", spans = new[] { new { text = "Paragraph.", bold = false } } }
            }
        };
        var resp = await client.PutAsJsonAsync($"/modules/{moduleId}", body);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("gridX").GetInt32());
        Assert.Equal(12, doc.RootElement.GetProperty("gridHeight").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("zIndex").GetInt32());
        var content = doc.RootElement.GetProperty("content");
        Assert.Equal(2, content.GetArrayLength());
    }

    [Fact]
    public async Task UpdateModule_InvalidBuildingBlock_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        var body = new
        {
            moduleType = "Theory",
            gridX = 0, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0,
            content = new object[]
            {
                new { type = "ChordProgression", chords = new object[0] }
            }
        };
        var resp = await client.PutAsJsonAsync($"/modules/{moduleId}", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_BUILDING_BLOCK", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModule_ModuleTypeMismatch_Returns400()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId); // Theory

        var body = new
        {
            moduleType = "Practice",
            gridX = 0, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0,
            content = new object[0]
        };
        var resp = await client.PutAsJsonAsync($"/modules/{moduleId}", body);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_TYPE_IMMUTABLE", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModule_BreadcrumbWithContent_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId, "Breadcrumb", 0, 0, 20, 3);

        var body = new
        {
            moduleType = "Breadcrumb",
            gridX = 0, gridY = 0, gridWidth = 20, gridHeight = 3, zIndex = 0,
            content = new object[]
            {
                new { type = "Text", spans = new[] { new { text = "test", bold = false } } }
            }
        };
        var resp = await client.PutAsJsonAsync($"/modules/{moduleId}", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("BREADCRUMB_CONTENT_NOT_EMPTY", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModule_OtherUsersModule_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (_, pageId) = await CreateLessonAsync(ownerClient, notebookId);
        var moduleId = await CreateModuleAndGetId(ownerClient, pageId);

        var otherClient = await RegisterAsync(factory);
        var body = new
        {
            moduleType = "Theory",
            gridX = 0, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0,
            content = new object[0]
        };
        var resp = await otherClient.PutAsJsonAsync($"/modules/{moduleId}", body);

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateModule_NotFound_Returns404()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var body = new
        {
            moduleType = "Theory",
            gridX = 0, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0,
            content = new object[0]
        };
        var resp = await client.PutAsJsonAsync($"/modules/{Guid.NewGuid()}", body);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── PATCH /modules/{moduleId}/layout ────────────────────────────────

    [Fact]
    public async Task UpdateModuleLayout_HappyPath_Returns200WithContentUnchanged()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        var body = new { gridX = 5, gridY = 5, gridWidth = 20, gridHeight = 12, zIndex = 3 };
        var resp = await client.PatchAsJsonAsync($"/modules/{moduleId}/layout", body);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(5, doc.RootElement.GetProperty("gridX").GetInt32());
        Assert.Equal(5, doc.RootElement.GetProperty("gridY").GetInt32());
        Assert.Equal(20, doc.RootElement.GetProperty("gridWidth").GetInt32());
        Assert.Equal(12, doc.RootElement.GetProperty("gridHeight").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("zIndex").GetInt32());
        // Content unchanged
        var content = doc.RootElement.GetProperty("content");
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        Assert.Equal(0, content.GetArrayLength());
    }

    [Fact]
    public async Task UpdateModuleLayout_OutOfBounds_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        // A4: 42x59. 40 + 8 = 48 > 42
        var body = new { gridX = 40, gridY = 0, gridWidth = 8, gridHeight = 5, zIndex = 0 };
        var resp = await client.PatchAsJsonAsync($"/modules/{moduleId}/layout", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_OUT_OF_BOUNDS", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModuleLayout_Overlap_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // Create two non-overlapping modules
        await CreateModuleAndGetId(client, pageId, "Theory", 0, 0, 20, 10);
        var moduleId2 = await CreateModuleAndGetId(client, pageId, "Theory", 20, 0, 18, 10);

        // Move module2 to overlap module1
        var body = new { gridX = 5, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0 };
        var resp = await client.PatchAsJsonAsync($"/modules/{moduleId2}/layout", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_OVERLAP", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModuleLayout_TooSmall_Returns422()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        // Theory min: 8x5, resizing to 4x3
        var body = new { gridX = 0, gridY = 0, gridWidth = 4, gridHeight = 3, zIndex = 0 };
        var resp = await client.PatchAsJsonAsync($"/modules/{moduleId}/layout", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("MODULE_TOO_SMALL", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateModuleLayout_OtherUsersModule_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (_, pageId) = await CreateLessonAsync(ownerClient, notebookId);
        var moduleId = await CreateModuleAndGetId(ownerClient, pageId);

        var otherClient = await RegisterAsync(factory);
        var body = new { gridX = 5, gridY = 5, gridWidth = 18, gridHeight = 10, zIndex = 0 };
        var resp = await otherClient.PatchAsJsonAsync($"/modules/{moduleId}/layout", body);

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateModuleLayout_NotFound_Returns404()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var body = new { gridX = 0, gridY = 0, gridWidth = 18, gridHeight = 10, zIndex = 0 };
        var resp = await client.PatchAsJsonAsync($"/modules/{Guid.NewGuid()}/layout", body);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── GET /pages/{pageId}/modules ────────────────────────────────────

    [Fact]
    public async Task GetModules_ReturnsModulesWithCorrectOrdering()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        // Create modules at different positions
        await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 20, 10, 18, 10)); // gridY=10, gridX=20
        await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 0, 0, 18, 10)); // gridY=0, gridX=0
        await client.PostAsJsonAsync($"/pages/{pageId}/modules",
            MakeCreateModuleBody("Theory", 0, 10, 18, 10)); // gridY=10, gridX=0

        var resp = await client.GetAsync($"/pages/{pageId}/modules");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        Assert.Equal(3, arr.GetArrayLength());
        // Ordered by GridY asc then GridX asc
        Assert.Equal(0, arr[0].GetProperty("gridY").GetInt32());
        Assert.Equal(0, arr[0].GetProperty("gridX").GetInt32());
        Assert.Equal(10, arr[1].GetProperty("gridY").GetInt32());
        Assert.Equal(0, arr[1].GetProperty("gridX").GetInt32());
        Assert.Equal(10, arr[2].GetProperty("gridY").GetInt32());
        Assert.Equal(20, arr[2].GetProperty("gridX").GetInt32());
        // Verify content is deserialized
        Assert.Equal(JsonValueKind.Array, arr[0].GetProperty("content").ValueKind);
    }

    [Fact]
    public async Task GetModules_EmptyPage_ReturnsEmptyArray()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);

        var resp = await client.GetAsync($"/pages/{pageId}/modules");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetModules_OtherUsersPage_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (_, pageId) = await CreateLessonAsync(ownerClient, notebookId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.GetAsync($"/pages/{pageId}/modules");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task GetModules_WithoutAuth_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.GetAsync($"/pages/{Guid.NewGuid()}/modules");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── DELETE /modules/{moduleId} ──────────────────────────────────────

    [Fact]
    public async Task DeleteModule_HappyPath_Returns204AndModuleGone()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (_, pageId) = await CreateLessonAsync(client, notebookId);
        var moduleId = await CreateModuleAndGetId(client, pageId);

        var resp = await client.DeleteAsync($"/modules/{moduleId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        // Verify module is gone via GET
        var getResp = await client.GetAsync($"/pages/{pageId}/modules");
        var doc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task DeleteModule_OtherUsersModule_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (_, pageId) = await CreateLessonAsync(ownerClient, notebookId);
        var moduleId = await CreateModuleAndGetId(ownerClient, pageId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.DeleteAsync($"/modules/{moduleId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteModule_NotFound_Returns404()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);

        var resp = await client.DeleteAsync($"/modules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteModule_WithoutAuth_Returns401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.DeleteAsync($"/modules/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    private record AuthBody(string AccessToken, int ExpiresIn);
}

// ── No-op seeders ─────────────────────────────────────────────────────────────

file sealed class ModulesNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class ModulesNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class ModulesNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class ModulesTestPasswordHasher : IPasswordHasher
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
