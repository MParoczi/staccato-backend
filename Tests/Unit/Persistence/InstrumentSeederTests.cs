using DomainModels.Enums;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

public class InstrumentSeederTests
{
    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_EmptyTable_InsertsExactlySevenInstruments()
    {
        await using var ctx = CreateContext();
        var seeder = new InstrumentSeeder(ctx);

        await seeder.SeedAsync();

        Assert.Equal(7, await ctx.Instruments.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_EmptyTable_InsertsAllSevenKeys()
    {
        await using var ctx = CreateContext();
        var seeder = new InstrumentSeeder(ctx);

        await seeder.SeedAsync();

        var keys = await ctx.Instruments.Select(i => i.Key).ToListAsync();
        Assert.Contains(InstrumentKey.Guitar6String, keys);
        Assert.Contains(InstrumentKey.Guitar7String, keys);
        Assert.Contains(InstrumentKey.Bass4String, keys);
        Assert.Contains(InstrumentKey.Bass5String, keys);
        Assert.Contains(InstrumentKey.Ukulele4String, keys);
        Assert.Contains(InstrumentKey.Banjo4String, keys);
        Assert.Contains(InstrumentKey.Banjo5String, keys);
    }

    [Theory]
    [InlineData(InstrumentKey.Guitar6String, "6-String Guitar", 6)]
    [InlineData(InstrumentKey.Guitar7String, "7-String Guitar", 7)]
    [InlineData(InstrumentKey.Bass4String, "4-String Bass", 4)]
    [InlineData(InstrumentKey.Bass5String, "5-String Bass", 5)]
    [InlineData(InstrumentKey.Ukulele4String, "Ukulele", 4)]
    [InlineData(InstrumentKey.Banjo4String, "4-String Banjo", 4)]
    [InlineData(InstrumentKey.Banjo5String, "5-String Banjo", 5)]
    public async Task SeedAsync_EmptyTable_EachInstrumentHasCorrectDisplayNameAndStringCount(
        InstrumentKey key, string expectedName, int expectedStringCount)
    {
        await using var ctx = CreateContext();
        var seeder = new InstrumentSeeder(ctx);

        await seeder.SeedAsync();

        var instrument = await ctx.Instruments.SingleAsync(i => i.Key == key);
        Assert.Equal(expectedName, instrument.DisplayName);
        Assert.Equal(expectedStringCount, instrument.StringCount);
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_NonEmptyTable_ExitsWithoutInsertingMoreRows()
    {
        await using var ctx = CreateContext();
        var seeder = new InstrumentSeeder(ctx);

        await seeder.SeedAsync(); // first run
        var countAfterFirst = await ctx.Instruments.CountAsync();

        await seeder.SeedAsync(); // second run — should be no-op
        var countAfterSecond = await ctx.Instruments.CountAsync();

        Assert.Equal(countAfterFirst, countAfterSecond);
        Assert.Equal(7, countAfterSecond);
    }
}