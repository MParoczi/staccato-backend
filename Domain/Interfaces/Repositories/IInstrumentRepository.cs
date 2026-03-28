using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IInstrumentRepository : IRepository<Instrument>
{
    /// <summary>
    ///     Returns all instruments ordered by Name ascending.
    /// </summary>
    Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns the instrument matching the given key, or null if not found.
    /// </summary>
    Task<Instrument?> GetByKeyAsync(InstrumentKey key, CancellationToken ct = default);
}