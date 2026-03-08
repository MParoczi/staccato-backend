using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IInstrumentRepository : IRepository<Instrument>
{
    /// <summary>
    ///     Returns all instruments ordered by Name ascending.
    /// </summary>
    Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default);
}