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
using Moq;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Integration.Controllers;

/// <summary>
///     End-to-end integration tests for the <c>/api/instruments</c> endpoint.
/// </summary>
public class InstrumentsControllerTests
{
    private const string TestJwtSecret = "test-secret-key-must-be-at-least-32-chars!!";

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

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
                services.AddScoped<InstrumentSeeder, InstrumentsNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, InstrumentsNoOpChordSeeder>();
                services.AddScoped<SystemStylePresetSeeder, InstrumentsNoOpSystemStylePresetSeeder>();

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

    private static async Task SeedInstrumentsAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Instruments.AddRange(
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String, DisplayName = "6-String Guitar", StringCount = 6 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar7String, DisplayName = "7-String Guitar", StringCount = 7 }
        );
        await db.SaveChangesAsync();
    }

    // ── GET /api/instruments ──────────────────────────────────────────────

    [Fact]
    public async Task GetInstruments_Returns200WithAllSeededInstruments()
    {
        using var factory = CreateFactory();
        await SeedInstrumentsAsync(factory);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());

        var item = root[0];
        Assert.True(item.TryGetProperty("id", out _));
        Assert.True(item.TryGetProperty("key", out _));
        Assert.True(item.TryGetProperty("name", out _));
        Assert.True(item.TryGetProperty("stringCount", out _));
    }

    [Fact]
    public async Task GetInstruments_WithoutAuthorizationHeader_Returns200()
    {
        using var factory = CreateFactory();
        await SeedInstrumentsAsync(factory);
        var client = factory.CreateClient();
        // No Authorization header set — endpoint must be public

        var response = await client.GetAsync("/api/instruments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetInstruments_EmptyDatabase_Returns200WithEmptyArray()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }
}

file sealed class InstrumentsNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class InstrumentsNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class InstrumentsNoOpSystemStylePresetSeeder(AppDbContext ctx) : SystemStylePresetSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}