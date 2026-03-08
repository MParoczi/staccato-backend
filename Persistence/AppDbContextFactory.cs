using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Persistence.Context;

namespace Persistence;

/// <summary>
///     Design-time factory used exclusively by EF Core tooling (dotnet ef migrations add/update).
///     Not invoked at runtime — the application uses AppDbContext registered via AddDatabase().
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=Staccato;Trusted_Connection=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}