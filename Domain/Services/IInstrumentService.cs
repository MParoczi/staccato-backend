using DomainModels.Models;

namespace Domain.Services;

public interface IInstrumentService
{
    /// <summary>
    ///     Returns all seeded instruments ordered by name ascending.
    /// </summary>
    Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default);
}