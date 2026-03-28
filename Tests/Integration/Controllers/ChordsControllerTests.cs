using System.Net;
using System.Text;
using System.Text.Json;
using Domain.Services;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Integration.Controllers;

/// <summary>
///     End-to-end integration tests for the <c>/api/chords</c> endpoints.
/// </summary>
public class ChordsControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // Minimal positions JSON matching the stored format (guitar_chords.json style)
    private const string OnePositionJson =
        """[{"label":"1","baseFret":1,"barre":null,"strings":[{"string":1,"state":"open","fret":null,"finger":null}]}]""";

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
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

                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.RemoveAll<SystemStylePresetSeeder>();
                services.AddScoped<InstrumentSeeder, ChordsNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, ChordsNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, ChordsNoOpSystemStylePresetSeeder>();

                services.RemoveAll<IAzureBlobService>();
                services.AddSingleton<IAzureBlobService>(new Moq.Mock<IAzureBlobService>().Object);

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

    private static async Task<(Guid InstrumentId, Guid ChordId)> SeedAsync(
        WebApplicationFactory<Program> factory,
        string root = "A",
        string quality = "Major")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var instrument = new InstrumentEntity
        {
            Id = Guid.NewGuid(),
            Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };
        db.Instruments.Add(instrument);

        var chord = new ChordEntity
        {
            Id = Guid.NewGuid(),
            InstrumentId = instrument.Id,
            Name = root,
            Root = root,
            Quality = quality,
            Extension = null,
            Alternation = null,
            PositionsJson = OnePositionJson
        };
        db.Chords.Add(chord);

        await db.SaveChangesAsync();
        return (instrument.Id, chord.Id);
    }

    // ── GET /api/chords ───────────────────────────────────────────────────

    [Fact]
    public async Task GetChords_WithValidInstrument_Returns200WithSummaries()
    {
        using var factory = CreateFactory();
        var (_, chordId) = await SeedAsync(factory);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/chords?instrument=Guitar6String");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var item = root[0];
        Assert.True(item.TryGetProperty("id", out var id));
        Assert.Equal(chordId.ToString(), id.GetString());
        Assert.Equal("Guitar6String", item.GetProperty("instrumentKey").GetString());
        Assert.Equal("A", item.GetProperty("root").GetString());
        Assert.Equal("Major", item.GetProperty("quality").GetString());
        Assert.True(item.TryGetProperty("previewPosition", out _));
        Assert.False(item.TryGetProperty("positions", out _));
    }

    [Fact]
    public async Task GetChords_WithRootAndQualityFilters_Returns200Filtered()
    {
        using var factory = CreateFactory();
        await SeedAsync(factory, root: "A", quality: "Major");

        // Seed a second chord that should NOT match
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var instrument = await db.Instruments.FirstAsync();
        db.Chords.Add(new ChordEntity
        {
            Id = Guid.NewGuid(),
            InstrumentId = instrument.Id,
            Name = "Am",
            Root = "A",
            Quality = "Minor",
            PositionsJson = OnePositionJson
        });
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/chords?instrument=Guitar6String&root=A&quality=Major");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetArrayLength());
        Assert.Equal("Major", json.RootElement[0].GetProperty("quality").GetString());
    }

    [Fact]
    public async Task GetChords_MissingInstrumentParam_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/chords");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetChords_InvalidInstrumentParam_Returns400()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/chords?instrument=Theremin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/chords/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetChordById_ExistingId_Returns200WithAllPositions()
    {
        using var factory = CreateFactory();
        var (_, chordId) = await SeedAsync(factory);
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/chords/{chordId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(chordId.ToString(), root.GetProperty("id").GetString());
        Assert.True(root.TryGetProperty("positions", out var positions));
        Assert.Equal(JsonValueKind.Array, positions.ValueKind);
        Assert.Equal(1, positions.GetArrayLength());
        Assert.False(root.TryGetProperty("previewPosition", out _));
    }

    [Fact]
    public async Task GetChordById_UnknownId_Returns404()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/chords/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

file sealed class ChordsNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class ChordsNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class ChordsNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
