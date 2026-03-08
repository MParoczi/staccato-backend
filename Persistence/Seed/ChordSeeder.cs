using System.Text.Json;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seed;

public class ChordSeeder(AppDbContext context)
{
    protected virtual string ChordFilePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "guitar_chords.json");

    public virtual async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Chords.AnyAsync(ct)) return;

        var filePath = ChordFilePath;

        if (!File.Exists(filePath))
            throw new InvalidOperationException(
                $"Chord data file not found at expected path: {filePath}");

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to read chord data file at path: {filePath}", ex);
        }

        List<ChordRecord>? records;
        try
        {
            records = JsonSerializer.Deserialize<List<ChordRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize chord data file at path: {filePath}", ex);
        }

        if (records is null || records.Count == 0)
            throw new InvalidOperationException(
                $"Chord data file is empty or invalid at path: {filePath}");

        var seen = new HashSet<string>();
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Name) ||
                string.IsNullOrWhiteSpace(record.Suffix) ||
                record.Positions is null || record.Positions.Count == 0)
                throw new InvalidOperationException(
                    $"Chord data file contains an entry with missing name, suffix, or positions at path: {filePath}");

            var key = $"{record.Name}|{record.Suffix}";
            if (!seen.Add(key))
                throw new InvalidOperationException(
                    $"Chord data file contains duplicate name+suffix '{key}' at path: {filePath}");
        }

        var guitar = await context.Instruments
            .FirstOrDefaultAsync(i => i.Key == InstrumentKey.Guitar6String, ct)
            ?? throw new InvalidOperationException(
                "Guitar6String instrument not found — run InstrumentSeeder before ChordSeeder.");

        var serialiserOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var entities = records.Select(r => new ChordEntity
        {
            Id           = Guid.NewGuid(),
            InstrumentId = guitar.Id,
            Name         = r.Name,
            Suffix       = r.Suffix,
            PositionsJson = JsonSerializer.Serialize(r.Positions, serialiserOptions)
        }).ToList();

        context.Chords.AddRange(entities);
        await context.SaveChangesAsync(ct);
    }

    // ── private DTO for deserialising guitar_chords.json ─────────────────────

    private sealed class ChordRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public List<JsonElement>? Positions { get; set; }
    }
}
