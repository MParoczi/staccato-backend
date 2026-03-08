using System.Text.Json;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

/// <summary>
/// Six fail cases from FR-038: missing file, invalid JSON, null/empty array,
/// missing fields, empty positions, duplicate name+suffix.
/// </summary>
public class ChordSeederFailTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _tempFile;

    public ChordSeederFailTests()
    {
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "guitar_chords.json");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── helpers ──────────────────────────────────────────────────────────────

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private async Task SeedGuitarAsync(AppDbContext ctx)
    {
        ctx.Instruments.Add(new InstrumentEntity
        {
            Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar", StringCount = 6
        });
        await ctx.SaveChangesAsync();
    }

    private ChordSeeder Seeder(AppDbContext ctx) => new TestableChordSeeder(ctx, _tempFile);

    // FR-038 (a): file missing → InvalidOperationException containing the file path
    [Fact]
    public async Task SeedAsync_FileMissing_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);
        // Do NOT create the file

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // FR-038 (b): invalid JSON → exception with file path
    [Fact]
    public async Task SeedAsync_InvalidJson_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);
        File.WriteAllText(_tempFile, "this is not json {{ [");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // FR-038 (c): valid JSON but empty array → InvalidOperationException with file path
    [Fact]
    public async Task SeedAsync_EmptyArray_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);
        File.WriteAllText(_tempFile, "[]");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // FR-038 (d): entry missing name/suffix/positions field → exception with file path
    [Fact]
    public async Task SeedAsync_EntryMissingName_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        // name is empty string
        var chords = new[]
        {
            new { name = "", suffix = "major", positions = new[] { MakePosition() } }
        };
        File.WriteAllText(_tempFile,
            JsonSerializer.Serialize(chords,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // FR-038 (e): entry with empty positions array → exception with file path
    [Fact]
    public async Task SeedAsync_EmptyPositionsArray_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        var chords = new[]
        {
            new { name = "A", suffix = "major", positions = Array.Empty<object>() }
        };
        File.WriteAllText(_tempFile,
            JsonSerializer.Serialize(chords,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // FR-038 (f): duplicate name+suffix → exception with file path
    [Fact]
    public async Task SeedAsync_DuplicateNamePlusSuffix_ThrowsWithFilePath()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        var chords = new[]
        {
            new { name = "A", suffix = "major", positions = new[] { MakePosition() } },
            new { name = "A", suffix = "major", positions = new[] { MakePosition() } },  // duplicate
        };
        File.WriteAllText(_tempFile,
            JsonSerializer.Serialize(chords,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Seeder(ctx).SeedAsync());

        Assert.Contains(_tempFile, ex.Message);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static object MakePosition() => new
    {
        label    = "1",
        baseFret = 1,
        barre    = (object?)null,
        strings  = new[]
        {
            new { @string = 6, state = "muted",   fret = (int?)null, finger = (int?)null },
            new { @string = 5, state = "open",    fret = (int?)null, finger = (int?)null },
            new { @string = 4, state = "fretted", fret = (int?)2,    finger = (int?)1   },
            new { @string = 3, state = "fretted", fret = (int?)2,    finger = (int?)2   },
            new { @string = 2, state = "fretted", fret = (int?)2,    finger = (int?)3   },
            new { @string = 1, state = "open",    fret = (int?)null, finger = (int?)null },
        }
    };

    private sealed class TestableChordSeeder(AppDbContext ctx, string filePath)
        : ChordSeeder(ctx)
    {
        protected override string ChordFilePath => filePath;
    }
}
