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
        var query = _context.Chords.Where(c => c.InstrumentId == instrumentId);

        if (root is not null)
            query = query.Where(c => c.Name == root);

        if (quality is not null)
            query = query.Where(c => c.Quality.ToLower() == quality.ToLower());

        var entities = await query.ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<Chord>>(entities);
    }
}