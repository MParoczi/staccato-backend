namespace Domain.Interfaces;

public interface IUnitOfWork
{
    /// <summary>
    ///     Flushes all staged repository changes to the database as one atomic transaction.
    ///     Returns the number of state entries written.
    /// </summary>
    Task<int> CommitAsync(CancellationToken ct = default);
}