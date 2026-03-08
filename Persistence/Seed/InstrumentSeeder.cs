using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seed;

public class InstrumentSeeder(AppDbContext context)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Instruments.AnyAsync(ct)) return;

        context.Instruments.AddRange(
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar6String, DisplayName = "6-String Guitar", StringCount = 6 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Guitar7String, DisplayName = "7-String Guitar", StringCount = 7 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Bass4String,   DisplayName = "4-String Bass",   StringCount = 4 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Bass5String,   DisplayName = "5-String Bass",   StringCount = 5 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Ukulele4String, DisplayName = "Ukulele",        StringCount = 4 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Banjo4String,  DisplayName = "4-String Banjo",  StringCount = 4 },
            new InstrumentEntity { Id = Guid.NewGuid(), Key = InstrumentKey.Banjo5String,  DisplayName = "5-String Banjo",  StringCount = 5 }
        );

        await context.SaveChangesAsync(ct);
    }
}
