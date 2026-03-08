using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IChordRepository : IRepository<Chord>
{
    /// <summary>
    ///     Returns chords matching the given instrument and optional filters.
    ///     Null root or quality means "no filter on that dimension".
    /// </summary>
    Task<IReadOnlyList<Chord>> SearchAsync(
        Guid instrumentId,
        string? root,
        string? quality,
        CancellationToken ct = default);
}