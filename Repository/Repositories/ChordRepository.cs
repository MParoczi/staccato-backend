using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class ChordRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<ChordEntity, Chord>(context, mapper), IChordRepository
{
    public async Task<IReadOnlyList<Chord>> SearchAsync(
        Guid instrumentId,
        string? root,
        string? quality,
        CancellationToken ct = default)
    {
        var query = _context.Chords
            .Include(c => c.Instrument)
            .Where(c => c.InstrumentId == instrumentId);

        if (root is not null)
            query = query.Where(c => c.Root.ToLower() == root.ToLower());

        if (quality is not null)
            query = query.Where(c => c.Quality.ToLower() == quality.ToLower());

        var entities = await query
            .OrderBy(c => c.Root)
            .ThenBy(c => c.Quality)
            .ToListAsync(ct);

        return _mapper.Map<IReadOnlyList<Chord>>(entities);
    }

    async Task<Chord?> IRepository<Chord>.GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _context.Chords
            .Include(c => c.Instrument)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return _mapper.Map<Chord?>(entity);
    }
}
