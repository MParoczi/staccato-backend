using System.Text;
using System.Text.Json;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Tests.Unit.Persistence;

/// <summary>
///     Fail cases for ChordSeeder: null stream, invalid JSON, empty array,
///     missing required fields (root/quality), and empty positions.
/// </summary>
public class ChordSeederFailTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static async Task SeedGuitarAsync(AppDbContext ctx)
    {
        ctx.Instruments.Add(new InstrumentEntity
        {
            Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar", StringCount = 6
        });
        await ctx.SaveChangesAsync();
    }

    private static ChordSeeder Seeder(AppDbContext ctx, string? json)
    {
        return new TestableChordSeeder(ctx, json);
    }

    private static string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ── fail cases ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SeedAsync_StreamNull_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, null).SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_InvalidJson_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, "this is not json {{ [").SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_EmptyArray_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, "[]").SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_EntryMissingRoot_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        var json = Serialize(new[]
        {
            new
            {
                name = "A", root = "", quality = "Major", extension = (string?)null,
                alternation = (string?)null, positions = new[] { MakePosition() }
            }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, json).SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_EntryMissingQuality_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        var json = Serialize(new[]
        {
            new
            {
                name = "A", root = "A", quality = "", extension = (string?)null,
                alternation = (string?)null, positions = new[] { MakePosition() }
            }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, json).SeedAsync());
    }

    [Fact]
    public async Task SeedAsync_EmptyPositionsArray_ThrowsInvalidOperation()
    {
        await using var ctx = CreateContext();
        await SeedGuitarAsync(ctx);

        var json = Serialize(new[]
        {
            new
            {
                name = "A", root = "A", quality = "Major", extension = (string?)null,
                alternation = (string?)null, positions = Array.Empty<object>()
            }
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => Seeder(ctx, json).SeedAsync());
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

    // ── testable subclass — overrides stream ──────────────────────────────────

    private sealed class TestableChordSeeder(AppDbContext ctx, string? json)
        : ChordSeeder(ctx)
    {
        protected override Stream? GetChordStream()
        {
            return json is null ? null : new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
}