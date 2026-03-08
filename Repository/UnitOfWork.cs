using Domain.Interfaces;
using Persistence.Context;

namespace Repository;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }
}