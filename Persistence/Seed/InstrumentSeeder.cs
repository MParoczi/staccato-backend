using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seed;

public class InstrumentSeeder(AppDbContext context)
{
    private static readonly InstrumentEntity[] Sources =
    [
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String, DisplayName = "6-String Guitar", StringCount = 6 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar7String, DisplayName = "7-String Guitar", StringCount = 7 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Bass4String, DisplayName = "4-String Bass", StringCount = 4 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Bass5String, DisplayName = "5-String Bass", StringCount = 5 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Ukulele4String, DisplayName = "Ukulele", StringCount = 4 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Banjo4String, DisplayName = "4-String Banjo", StringCount = 4 },
        new() { Id = Guid.NewGuid(), Key = InstrumentKey.Banjo5String, DisplayName = "5-String Banjo", StringCount = 5 }
    ];

    public virtual async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await context.Instruments
            .Select(i => i.Key)
            .ToHashSetAsync(ct);

        var toAdd = Sources.Where(s => !existing.Contains(s.Key)).ToList();
        if (toAdd.Count == 0) return;

        // Assign fresh IDs so parallel test runs never collide
        foreach (var entity in toAdd)
            entity.Id = Guid.NewGuid();

        context.Instruments.AddRange(toAdd);
        await context.SaveChangesAsync(ct);
    }
}