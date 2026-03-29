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

public class LessonsControllerTests
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
                services.AddSingleton<IPasswordHasher, LessonsTestPasswordHasher>();

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, LessonsNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, LessonsNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, LessonsNoOpSystemStylePresetSeeder>();

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

    // ── POST /notebooks/{id}/lessons ─────────────────────────────────────

    [Fact]
    public async Task CreateLesson_Returns201WithDetailIncludingFirstPage()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Guitar Basics"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("Guitar Basics", root.GetProperty("title").GetString());
        Assert.Equal(notebookId, root.GetProperty("notebookId").GetString());

        var pages = root.GetProperty("pages");
        Assert.Equal(1, pages.GetArrayLength());
        Assert.Equal(1, pages[0].GetProperty("pageNumber").GetInt32());
        Assert.Equal(0, pages[0].GetProperty("moduleCount").GetInt32());
    }

    [Fact]
    public async Task CreateLesson_EmptyTitle_Returns400()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateLesson_TitleExceeds200Chars_Returns400()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = new string('A', 201)
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateLesson_TitleExactly200Chars_Returns201()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = new string('A', 200)
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateLesson_NotebookNotFound_Returns404()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.PostAsJsonAsync($"/notebooks/{Guid.NewGuid()}/lessons", new
        {
            title = "Test"
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CreateLesson_OtherUsersNotebook_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /notebooks/{id}/lessons ──────────────────────────────────────

    [Fact]
    public async Task GetLessons_Returns200WithOrderedListAndPageCount()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "Lesson A" });
        await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "Lesson B" });
        await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "Lesson C" });

        var resp = await client.GetAsync($"/notebooks/{notebookId}/lessons");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var arr = doc.RootElement;
        Assert.Equal(3, arr.GetArrayLength());
        Assert.Equal("Lesson A", arr[0].GetProperty("title").GetString());
        Assert.Equal("Lesson B", arr[1].GetProperty("title").GetString());
        Assert.Equal("Lesson C", arr[2].GetProperty("title").GetString());
        Assert.Equal(1, arr[0].GetProperty("pageCount").GetInt32());
    }

    [Fact]
    public async Task GetLessons_EmptyNotebook_Returns200WithEmptyArray()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.GetAsync($"/notebooks/{notebookId}/lessons");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetLessons_OtherUsersNotebook_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.GetAsync($"/notebooks/{notebookId}/lessons");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /lessons/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetLesson_Returns200WithDetailAndPages()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var createResp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Detail Test"
        });
        var createDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var lessonId = createDoc.RootElement.GetProperty("id").GetString()!;

        var resp = await client.GetAsync($"/lessons/{lessonId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("Detail Test", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("pages").GetArrayLength());
    }

    [Fact]
    public async Task GetLesson_NotFound_Returns404()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.GetAsync($"/lessons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task GetLesson_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var createResp = await ownerClient.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Owned"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.GetAsync($"/lessons/{lessonId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── PUT /lessons/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLesson_Returns200WithUpdatedDetail()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var createResp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Original"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PutAsJsonAsync($"/lessons/{lessonId}", new
        {
            title = "Updated"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal("Updated", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateLesson_EmptyTitle_Returns400()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var createResp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Test"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var resp = await client.PutAsJsonAsync($"/lessons/{lessonId}", new
        {
            title = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateLesson_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var createResp = await ownerClient.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Owned"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.PutAsJsonAsync($"/lessons/{lessonId}", new
        {
            title = "Hijack"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── DELETE /lessons/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteLesson_Returns204AndCascades()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var createResp = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "To Delete"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var deleteResp = await client.DeleteAsync($"/lessons/{lessonId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await client.GetAsync($"/lessons/{lessonId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteLesson_NotFound_Returns404()
    {
        using var factory = CreateFactory();
        var client = await RegisterAsync(factory);

        var resp = await client.DeleteAsync($"/lessons/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteLesson_OtherUsersLesson_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var createResp = await ownerClient.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new
        {
            title = "Owned"
        });
        var lessonId = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.DeleteAsync($"/lessons/{lessonId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /notebooks/{id}/index ───────────────────────────────────────

    [Fact]
    public async Task GetNotebookIndex_ReturnsCorrectStartPageNumbers()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        // Lesson A: 1 page (auto-created) + 2 added = 3 pages
        var respA = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "A" });
        var lessonAId = JsonDocument.Parse(await respA.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
        await client.PostAsync($"/lessons/{lessonAId}/pages", null);
        await client.PostAsync($"/lessons/{lessonAId}/pages", null);

        // Lesson B: 1 page (auto-created) + 1 added = 2 pages
        var respB = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "B" });
        var lessonBId = JsonDocument.Parse(await respB.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
        await client.PostAsync($"/lessons/{lessonBId}/pages", null);

        // Lesson C: 1 page (auto-created only)
        await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "C" });

        var resp = await client.GetAsync($"/notebooks/{notebookId}/index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var entries = doc.RootElement.GetProperty("entries");
        Assert.Equal(3, entries.GetArrayLength());
        Assert.Equal("A", entries[0].GetProperty("title").GetString());
        Assert.Equal(2, entries[0].GetProperty("startPageNumber").GetInt32()); // 2 + 0
        Assert.Equal(5, entries[1].GetProperty("startPageNumber").GetInt32()); // 2 + 3
        Assert.Equal(7, entries[2].GetProperty("startPageNumber").GetInt32()); // 2 + 3 + 2
    }

    [Fact]
    public async Task GetNotebookIndex_EmptyNotebook_ReturnsEmptyEntries()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        var resp = await client.GetAsync($"/notebooks/{notebookId}/index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public async Task GetNotebookIndex_UpdatesAfterPageDeletion()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var client = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(client);

        // Lesson A: 2 pages
        var respA = await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "A" });
        var lessonAId = JsonDocument.Parse(await respA.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString()!;
        var addPageResp = await client.PostAsync($"/lessons/{lessonAId}/pages", null);
        var page2Id = JsonDocument.Parse(await addPageResp.Content.ReadAsStringAsync())
            .RootElement.GetProperty("data").GetProperty("id").GetString()!;

        // Lesson B: 1 page
        await client.PostAsJsonAsync($"/notebooks/{notebookId}/lessons", new { title = "B" });

        // Before deletion: B starts at 2 + 2 = 4
        var indexBefore = await client.GetAsync($"/notebooks/{notebookId}/index");
        var docBefore = JsonDocument.Parse(await indexBefore.Content.ReadAsStringAsync());
        Assert.Equal(4, docBefore.RootElement.GetProperty("entries")[1].GetProperty("startPageNumber").GetInt32());

        // Delete page 2 from lesson A
        await client.DeleteAsync($"/lessons/{lessonAId}/pages/{page2Id}");

        // After deletion: B starts at 2 + 1 = 3
        var indexAfter = await client.GetAsync($"/notebooks/{notebookId}/index");
        var docAfter = JsonDocument.Parse(await indexAfter.Content.ReadAsStringAsync());
        Assert.Equal(3, docAfter.RootElement.GetProperty("entries")[1].GetProperty("startPageNumber").GetInt32());
    }

    [Fact]
    public async Task GetNotebookIndex_OtherUsersNotebook_Returns403()
    {
        using var factory = CreateFactory();
        await SeedInstrumentAsync(factory);
        await SeedColorfulPresetAsync(factory);
        var ownerClient = await RegisterAsync(factory);
        var notebookId = await CreateNotebookAsync(ownerClient);

        var otherClient = await RegisterAsync(factory);
        var resp = await otherClient.GetAsync($"/notebooks/{notebookId}/index");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Auth ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllEndpoints_WithoutAuth_Return401()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var endpoints = new[]
        {
            () => client.GetAsync($"/notebooks/{Guid.NewGuid()}/lessons"),
            () => client.PostAsJsonAsync($"/notebooks/{Guid.NewGuid()}/lessons", new { title = "T" }),
            () => client.GetAsync($"/lessons/{Guid.NewGuid()}"),
            () => client.PutAsJsonAsync($"/lessons/{Guid.NewGuid()}", new { title = "T" }),
            () => client.DeleteAsync($"/lessons/{Guid.NewGuid()}"),
            () => client.GetAsync($"/notebooks/{Guid.NewGuid()}/index")
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

file sealed class LessonsNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonsNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonsNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class LessonsTestPasswordHasher : IPasswordHasher
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