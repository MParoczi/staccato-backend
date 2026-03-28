using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class RefreshTokenRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<RefreshTokenEntity, RefreshToken>(context, mapper), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var entity = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token, ct);
        return _mapper.Map<RefreshToken?>(entity);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var entities = await _context.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<RefreshToken>>(entities);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);
        foreach (var token in tokens)
            token.IsRevoked = true;
        await _context.SaveChangesAsync(ct);
    }
}