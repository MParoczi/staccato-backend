using System.Text.Json;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seed;

public class ChordSeeder(AppDbContext context)
{
    protected virtual Stream? GetChordStream()
    {
        return typeof(ChordSeeder).Assembly
            .GetManifestResourceStream("Persistence.Data.guitar_chords.json");
    }

    public virtual async Task SeedAsync(CancellationToken ct = default)
    {
        var stream = GetChordStream()
                     ?? throw new InvalidOperationException(
                         "Embedded chord data resource 'Persistence.Data.guitar_chords.json' not found.");

        List<ChordRecord>? records;
        try
        {
            using var reader = new StreamReader(stream); // strips UTF-8 BOM automatically
            var json = await reader.ReadToEndAsync(ct);
            records = JsonSerializer.Deserialize<List<ChordRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Failed to deserialize embedded chord data resource.", ex);
        }

        if (records is null || records.Count == 0)
            throw new InvalidOperationException(
                "Embedded chord data resource is empty or invalid.");

        foreach (var record in records)
            if (string.IsNullOrWhiteSpace(record.Root) ||
                string.IsNullOrWhiteSpace(record.Quality) ||
                record.Positions is null || record.Positions.Count == 0)
                throw new InvalidOperationException(
                    $"Chord record '{record.Name}' is missing root, quality, or positions.");

        var guitar = await context.Instruments
                         .FirstOrDefaultAsync(i => i.Key == InstrumentKey.Guitar6String, ct)
                     ?? throw new InvalidOperationException(
                         "Guitar6String instrument not found — run InstrumentSeeder before ChordSeeder.");

        // Build HashSet of existing natural keys to enable differential seeding
        var existing = await context.Chords
            .Where(c => c.InstrumentId == guitar.Id)
            .Select(c => new { c.Root, c.Quality, c.Extension })
            .ToListAsync(ct);

        var existingKeys = existing
            .Select(c => (c.Root, c.Quality, c.Extension ?? string.Empty))
            .ToHashSet();

        var serialiserOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var toAdd = records
            .Where(r => !existingKeys.Contains((r.Root, r.Quality, r.Extension ?? string.Empty)))
            .Select(r => new ChordEntity
            {
                Id = Guid.NewGuid(),
                InstrumentId = guitar.Id,
                Name = r.Name,
                Root = r.Root,
                Quality = r.Quality,
                Extension = r.Extension,
                Alternation = r.Alternation,
                PositionsJson = JsonSerializer.Serialize(r.Positions, serialiserOptions)
            })
            .ToList();

        if (toAdd.Count == 0) return;

        context.Chords.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
    }

    // ── private DTO for deserialising guitar_chords.json ─────────────────────

    private sealed class ChordRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Root { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public string? Extension { get; set; }
        public string? Alternation { get; set; }
        public List<JsonElement>? Positions { get; set; }
    }
}