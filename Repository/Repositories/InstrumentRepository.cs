using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
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

    public async Task<Instrument?> GetByKeyAsync(InstrumentKey key, CancellationToken ct = default)
    {
        var entity = await _context.Instruments
            .Where(i => i.Key == key)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
        return _mapper.Map<Instrument?>(entity);
    }
}