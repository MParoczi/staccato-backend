using Domain.Interfaces.Repositories;
using DomainModels.Models;

namespace Domain.Services;

public class InstrumentService(IInstrumentRepository instrumentRepository) : IInstrumentService
{
    public Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default)
    {
        return instrumentRepository.GetAllAsync(ct);
    }
}