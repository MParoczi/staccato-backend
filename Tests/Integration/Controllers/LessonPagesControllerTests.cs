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

public class LessonPagesControllerTests
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
                services.AddSingleton<IPasswordHasher, LessonPagesTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, LessonPagesNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, LessonPagesNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, LessonPagesNoOpSystemStylePresetSeeder>();

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

    private static async Task<string> CreateNotebookAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/notebooks", new
        {
            title = "Test Notebook",
            instrumentId = SeedInstrumentId,
            pageSize = "A4",
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

    // ── POST /lessons/{id}/pages ─────────────────────────────────────────

    [Fact]
    public async Task AddPage_Returns201WithEnvelopeAndNullWarning()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, _) = await CreateLessonAsync(client, notebookId);

        var resp = await client.PostAsync($"/lessons/{lessonId}/pages", null);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(2, doc.RootElement.GetProperty("data").GetProperty("pageNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("warning").ValueKind);
    }

    [Fact]
    public async Task AddPage_At10Pages_Returns201WithNullWarning()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, _) = await CreateLessonAsync(client, notebookId);

        // Add pages 2 through 9 (lesson already has page 1, so we need 8 more to reach 9)
        for (var i = 0; i < 8; i++)
            await client.PostAsync($"/lessons/{lessonId}/pages", null);

        // Add 10th page — still under soft limit (lesson currently has 9 pages)
        var resp = await client.PostAsync($"/lessons/{lessonId}/pages", null);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(10, doc.RootElement.GetProperty("data").GetProperty("pageNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("warning").ValueKind);
    }

    [Fact]
    public async Task AddPage_Over10Pages_Returns200WithWarning()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, _) = await CreateLessonAsync(client, notebookId);

        // Add pages 2 through 10 (lesson has page 1, add 9 more to reach 10)
        for (var i = 0; i < 9; i++)
            await client.PostAsync($"/lessons/{lessonId}/pages", null);

        // Add 11th page — over soft limit (lesson currently has 10 pages)
        var resp = await client.PostAsync($"/lessons/{lessonId}/pages", null);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(11, doc.RootElement.GetProperty("data").GetProperty("pageNumber").GetInt32());
        Assert.Equal("This lesson has reached the recommended maximum of 10 pages.",
            doc.RootElement.GetProperty("warning").GetString());
    }

    [Fact]
    public async Task AddPage_LessonNotFound_Returns404()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsync($"/lessons/{Guid.NewGuid()}/pages", null);

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task AddPage_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (lessonId, _) = await CreateLessonAsync(ownerClient, notebookId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.PostAsync($"/lessons/{lessonId}/pages", null);

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /lessons/{id}/pages ──────────────────────────────────────────

    [Fact]
    public async Task GetPages_Returns200WithOrderedListAndModuleCount()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, _) = await CreateLessonAsync(client, notebookId);

        // Add second page
        await client.PostAsync($"/lessons/{lessonId}/pages", null);

        var resp = await client.GetAsync($"/lessons/{lessonId}/pages");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        Assert.Equal(2, arr.GetArrayLength());
        Assert.Equal(1, arr[0].GetProperty("pageNumber").GetInt32());
        Assert.Equal(2, arr[1].GetProperty("pageNumber").GetInt32());
        Assert.Equal(0, arr[0].GetProperty("moduleCount").GetInt32());
    }

    [Fact]
    public async Task GetPages_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (lessonId, _) = await CreateLessonAsync(ownerClient, notebookId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.GetAsync($"/lessons/{lessonId}/pages");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── DELETE /lessons/{lessonId}/pages/{pageId} ────────────────────────

    [Fact]
    public async Task DeletePage_Returns204()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, _) = await CreateLessonAsync(client, notebookId);

        // Add a second page so we can delete one
        var addResp = await client.PostAsync($"/lessons/{lessonId}/pages", null);
        var addDoc = JsonDocument.Parse(await addResp.Content.ReadAsStringAsync());
        var secondPageId = addDoc.RootElement.GetProperty("data").GetProperty("id").GetString()!;

        var resp = await client.DeleteAsync($"/lessons/{lessonId}/pages/{secondPageId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        // Verify page is gone
        var listResp = await client.GetAsync($"/lessons/{lessonId}/pages");
        var listDoc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        Assert.Equal(1, listDoc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task DeletePage_LastPage_Returns400WithCode()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId, firstPageId) = await CreateLessonAsync(client, notebookId);

        var resp = await client.DeleteAsync($"/lessons/{lessonId}/pages/{firstPageId}");

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("LAST_PAGE_DELETION", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task DeletePage_BelongsToDifferentLesson_Returns404()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);
        var (lessonId1, _) = await CreateLessonAsync(client, notebookId);
        var (_, lesson2FirstPageId) = await CreateLessonAsync(client, notebookId);

        // Try to delete lesson2's page via lesson1's URL
        var resp = await client.DeleteAsync($"/lessons/{lessonId1}/pages/{lesson2FirstPageId}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DeletePage_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);
        var (lessonId, firstPageId) = await CreateLessonAsync(ownerClient, notebookId);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.DeleteAsync($"/lessons/{lessonId}/pages/{firstPageId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Auth ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllEndpoints_WithoutAuth_Return401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);
        var id = Guid.NewGuid();

        var endpoints = new[]
        {
            () => client.GetAsync($"/lessons/{id}/pages"),
            () => client.PostAsync($"/lessons/{id}/pages", null),
            () => client.DeleteAsync($"/lessons/{id}/pages/{Guid.NewGuid()}")
        };

        foreach (var endpoint in endpoints)
        {
            var resp = await endpoint();
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }

    private record AuthBody(string AccessToken, int ExpiresIn);
}

// ── No-op seeders ─────────────────────────────────────────────────────────────

file sealed class LessonPagesNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonPagesNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonPagesNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonPagesTestPasswordHasher : IPasswordHasher
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
