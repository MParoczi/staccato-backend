using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Seed;

namespace Persistence;

public class DbInitializer(
    AppDbContext context,
    InstrumentSeeder instrumentSeeder,
    ChordSeeder chordSeeder,
    SystemStylePresetSeeder presetSeeder)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (context.Database.ProviderName?.Contains("InMemory") != true)
            await context.Database.MigrateAsync(ct);

        await instrumentSeeder.SeedAsync(ct);
        await chordSeeder.SeedAsync(ct);
        await presetSeeder.SeedAsync(ct);
    }
}
