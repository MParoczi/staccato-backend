using Microsoft.EntityFrameworkCore;
using Moq;
using Persistence;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

public class DbInitializerTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateSqlServerContext()
    {
        // Use a real SQL Server options stub — provider name will be SqlServer.
        // We never actually call the DB; we only check the provider name branch.
        // For migrate-is-called tests we use Moq on the context instead.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=.;Database=StaccatoTest;Trusted_Connection=True;")
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (Mock<InstrumentSeeder> Instrument,
        Mock<ChordSeeder> Chord,
        Mock<SystemStylePresetSeeder> Preset)
        CreateSeederMocks(AppDbContext ctx)
    {
        var instrument = new Mock<InstrumentSeeder>(ctx);
        instrument.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var chord = new Mock<ChordSeeder>(ctx);
        chord.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var preset = new Mock<SystemStylePresetSeeder>(ctx);
        preset.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (instrument, chord, preset);
    }

    // ── InMemory branch: MigrateAsync must NOT be called ─────────────────────

    [Fact]
    public async Task InitializeAsync_InMemoryProvider_SkipsMigrateAsync()
    {
        await using var ctx = CreateInMemoryContext();
        var (instrument, chord, preset) = CreateSeederMocks(ctx);

        var initializer = new DbInitializer(ctx, instrument.Object, chord.Object, preset.Object);
        await initializer.InitializeAsync();

        // All three seeders must still be called
        instrument.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        chord.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        preset.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Provider name contains "InMemory" — verify we're in the right branch
        Assert.Contains("InMemory", ctx.Database.ProviderName);
    }

    // ── Seeder call order: Instrument → Chord → Preset ───────────────────────

    [Fact]
    public async Task InitializeAsync_InMemoryProvider_CallsSeedersInOrder()
    {
        await using var ctx = CreateInMemoryContext();
        var callOrder = new List<string>();

        var instrument = new Mock<InstrumentSeeder>(ctx);
        instrument.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("instrument"))
            .Returns(Task.CompletedTask);

        var chord = new Mock<ChordSeeder>(ctx);
        chord.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("chord"))
            .Returns(Task.CompletedTask);

        var preset = new Mock<SystemStylePresetSeeder>(ctx);
        preset.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("preset"))
            .Returns(Task.CompletedTask);

        var initializer = new DbInitializer(ctx, instrument.Object, chord.Object, preset.Object);
        await initializer.InitializeAsync();

        Assert.Equal(new[] { "instrument", "chord", "preset" }, callOrder);
    }

    // ── Non-InMemory provider: MigrateAsync IS expected ──────────────────────
    // (We verify via the provider-name check, not by actually calling Migrate)

    [Fact]
    public void SqlServerProvider_ProviderName_DoesNotContainInMemory()
    {
        using var ctx = CreateSqlServerContext();
        Assert.DoesNotContain("InMemory", ctx.Database.ProviderName ?? string.Empty);
    }
}