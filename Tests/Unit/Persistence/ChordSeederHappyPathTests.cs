using System.Text.Json;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

public class ChordSeederHappyPathTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _tempFile;

    public ChordSeederHappyPathTests()
    {
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "guitar_chords.json");
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, true);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private async Task<InstrumentEntity> SeedGuitarInstrumentAsync(AppDbContext ctx)
    {
        var guitar = new InstrumentEntity
        {
            Id = Guid.NewGuid(),
            Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };
        ctx.Instruments.Add(guitar);
        await ctx.SaveChangesAsync();
        return guitar;
    }

    private void WriteChordFile(object[] chords)
    {
        File.WriteAllText(_tempFile,
            JsonSerializer.Serialize(chords,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private ChordSeeder CreateSeeder(AppDbContext ctx)
    {
        // Override BaseDirectory via a subclass that accepts a custom file path.
        return new TestableChordSeeder(ctx, _tempFile);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_ValidFile_InsertsOneRowPerJsonEntry()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        var chords = new[]
        {
            new { name = "A", suffix = "major", positions = new[] { MakePosition() } },
            new { name = "A", suffix = "minor", positions = new[] { MakePosition() } },
            new { name = "B", suffix = "major", positions = new[] { MakePosition() } }
        };
        WriteChordFile(chords);

        var seeder = CreateSeeder(ctx);
        await seeder.SeedAsync();

        Assert.Equal(3, await ctx.Chords.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ValidFile_EachChordHasCorrectNameSuffixAndInstrumentId()
    {
        await using var ctx = CreateContext();
        var guitar = await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            new { name = "C", suffix = "maj7", positions = new[] { MakePosition() } }
        });

        var seeder = CreateSeeder(ctx);
        await seeder.SeedAsync();

        var chord = await ctx.Chords.SingleAsync();
        Assert.Equal("C", chord.Name);
        Assert.Equal("maj7", chord.Suffix);
        Assert.Equal(guitar.Id, chord.InstrumentId);
        Assert.False(string.IsNullOrWhiteSpace(chord.PositionsJson));
    }

    [Fact]
    public async Task SeedAsync_ValidFile_PositionsJsonIsValidJson()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            new { name = "G", suffix = "7", positions = new[] { MakePosition() } }
        });

        var seeder = CreateSeeder(ctx);
        await seeder.SeedAsync();

        var chord = await ctx.Chords.SingleAsync();
        // Should not throw
        using var doc = JsonDocument.Parse(chord.PositionsJson);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_NonEmptyTable_ExitsWithoutInsertingMoreRows()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            new { name = "D", suffix = "minor", positions = new[] { MakePosition() } }
        });

        var seeder = CreateSeeder(ctx);
        await seeder.SeedAsync(); // first run
        var countAfterFirst = await ctx.Chords.CountAsync();

        await seeder.SeedAsync(); // second run — no-op
        Assert.Equal(countAfterFirst, await ctx.Chords.CountAsync());
    }

    // ── position fixture ──────────────────────────────────────────────────────

    private static object MakePosition()
    {
        return new
        {
            label = "1",
            baseFret = 1,
            barre = (object?)null,
            strings = new[]
            {
                new { @string = 6, state = "muted", fret = (int?)null, finger = (int?)null },
                new { @string = 5, state = "open", fret = (int?)null, finger = (int?)null },
                new { @string = 4, state = "fretted", fret = (int?)2, finger = (int?)1 },
                new { @string = 3, state = "fretted", fret = (int?)2, finger = (int?)2 },
                new { @string = 2, state = "fretted", fret = (int?)2, finger = (int?)3 },
                new { @string = 1, state = "open", fret = (int?)null, finger = (int?)null }
            }
        };
    }

    // ── testable subclass — overrides file path ───────────────────────────────

    private sealed class TestableChordSeeder(AppDbContext ctx, string filePath)
        : ChordSeeder(ctx)
    {
        protected override string ChordFilePath => filePath;
    }
}