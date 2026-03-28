using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class UserRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<UserEntity, User>(context, mapper), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        return _mapper.Map<User?>(entity);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default)
    {
        var entity = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);
        return _mapper.Map<User?>(entity);
    }

    public async Task<(User User, IReadOnlyList<RefreshToken> Tokens)?> GetWithActiveTokensAsync(
        Guid userId, CancellationToken ct = default)
    {
        var entity = await _context.Users
            .AsNoTracking()
            .Include(u => u.RefreshTokens.Where(t => !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow))
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (entity is null) return null;

        var user = _mapper.Map<User>(entity);
        var tokens = _mapper.Map<IReadOnlyList<RefreshToken>>(entity.RefreshTokens);
        return (user, tokens);
    }
}