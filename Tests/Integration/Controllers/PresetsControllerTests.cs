using System.Net;
using System.Text.Json;
using Domain.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Integration.Controllers;

/// <summary>
///     End-to-end integration tests for the <c>GET /presets</c> endpoint.
/// </summary>
public class PresetsControllerTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "test-secret-key-must-be-at-least-32-chars!!",
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
                services.AddSingleton<IPasswordHasher, PresetsTestPasswordHasher>();

                // Replace heavy seeders with no-ops; let SystemStylePresetSeeder run normally
                services.RemoveAll<InstrumentSeeder>();
                services.RemoveAll<ChordSeeder>();
                services.AddScoped<InstrumentSeeder, PresetsNoOpInstrumentSeeder>();
                services.AddScoped<ChordSeeder, PresetsNoOpChordSeeder>();

                services.RemoveAll<IAzureBlobService>();
                services.AddSingleton<IAzureBlobService>(new Mock<IAzureBlobService>().Object);
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

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPresets_Returns200WithFivePresets_WhenUnauthenticated()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.GetAsync("/presets");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(5, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetPresets_Returns200OrderedByDisplayOrder()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.GetAsync("/presets");

        resp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var presets = json.RootElement.EnumerateArray().ToList();
        for (var i = 0; i < presets.Count; i++)
            Assert.Equal(i + 1, presets[i].GetProperty("displayOrder").GetInt32());
    }

    [Fact]
    public async Task GetPresets_HasColorfulAsDefault()
    {
        using var factory = CreateFactory();
        var client = CreateClient(factory);

        var resp = await client.GetAsync("/presets");

        resp.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var presets = json.RootElement.EnumerateArray().ToList();

        var colorful = presets.Single(p => p.GetProperty("name").GetString() == "Colorful");
        var classic = presets.Single(p => p.GetProperty("name").GetString() == "Classic");

        Assert.True(colorful.GetProperty("isDefault").GetBoolean());
        Assert.False(classic.GetProperty("isDefault").GetBoolean());

        // Each preset must include 12 style entries
        Assert.Equal(12, colorful.GetProperty("styles").GetArrayLength());
    }
}

// ── No-op seeders ─────────────────────────────────────────────────────────────

file sealed class PresetsNoOpInstrumentSeeder(AppDbContext ctx) : InstrumentSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class PresetsNoOpChordSeeder(AppDbContext ctx) : ChordSeeder(ctx)
{
    public override Task SeedAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

file sealed class PresetsTestPasswordHasher : IPasswordHasher
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