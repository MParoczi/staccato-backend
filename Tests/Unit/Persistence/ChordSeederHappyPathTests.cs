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
        return new TestableChordSeeder(ctx, _tempFile);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_ValidFile_InsertsOneRowPerJsonEntry()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            MakeChord("A", "A", "Major"),
            MakeChord("Am", "A", "Minor"),
            MakeChord("B", "B", "Major")
        });

        await CreateSeeder(ctx).SeedAsync();

        Assert.Equal(3, await ctx.Chords.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_ValidFile_EachChordHasCorrectFieldsAndInstrumentId()
    {
        await using var ctx = CreateContext();
        var guitar = await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            MakeChord("C", "C", "Major")
        });

        await CreateSeeder(ctx).SeedAsync();

        var chord = await ctx.Chords.SingleAsync();
        Assert.Equal("C", chord.Name);
        Assert.Equal("C", chord.Root);
        Assert.Equal("Major", chord.Quality);
        Assert.Null(chord.Extension);
        Assert.Null(chord.Alternation);
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
            MakeChord("G7", "G", "Seventh")
        });

        await CreateSeeder(ctx).SeedAsync();

        var chord = await ctx.Chords.SingleAsync();
        using var doc = JsonDocument.Parse(chord.PositionsJson);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_SameChordTwice_NoExtraRowsInserted()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        WriteChordFile(new[]
        {
            MakeChord("D", "D", "Major")
        });

        var seeder = CreateSeeder(ctx);
        await seeder.SeedAsync(); // first run
        var countAfterFirst = await ctx.Chords.CountAsync();

        await seeder.SeedAsync(); // second run — differential seeder skips existing
        Assert.Equal(countAfterFirst, await ctx.Chords.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_PartiallySeeded_InsertsOnlyNewChords()
    {
        await using var ctx = CreateContext();
        await SeedGuitarInstrumentAsync(ctx);

        // First run: 2 chords
        WriteChordFile(new[]
        {
            MakeChord("E", "E", "Major"),
            MakeChord("Em", "E", "Minor")
        });
        await CreateSeeder(ctx).SeedAsync();
        Assert.Equal(2, await ctx.Chords.CountAsync());

        // Second run: same 2 chords + 1 new
        WriteChordFile(new[]
        {
            MakeChord("E", "E", "Major"),
            MakeChord("Em", "E", "Minor"),
            MakeChord("E7", "E", "Seventh")
        });
        await CreateSeeder(ctx).SeedAsync();
        Assert.Equal(3, await ctx.Chords.CountAsync());
    }

    // ── fixtures ──────────────────────────────────────────────────────────────

    private static object MakeChord(string name, string root, string quality,
        string? extension = null, string? alternation = null)
    {
        return new
        {
            name,
            root,
            quality,
            extension,
            alternation,
            positions = new[] { MakePosition() }
        };
    }

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

    // ── testable subclass — overrides stream ──────────────────────────────────

    private sealed class TestableChordSeeder(AppDbContext ctx, string filePath)
        : ChordSeeder(ctx)
    {
        protected override Stream? GetChordStream() =>
            File.Exists(filePath) ? File.OpenRead(filePath) : null;
    }
}
