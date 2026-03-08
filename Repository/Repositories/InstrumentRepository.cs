using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class InstrumentRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<InstrumentEntity, Instrument>(context, mapper), IInstrumentRepository
{
    public async Task<IReadOnlyList<Instrument>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _context.Instruments
            .OrderBy(i => i.DisplayName)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<Instrument>>(entities);
    }
}