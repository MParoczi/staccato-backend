using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class UserSavedPresetRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<UserSavedPresetEntity, UserSavedPreset>(context, mapper), IUserSavedPresetRepository
{
    public async Task<IReadOnlyList<UserSavedPreset>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var entities = await _context.UserSavedPresets
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<UserSavedPreset>>(entities);
    }

    public Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludePresetId = null, CancellationToken ct = default)
    {
        return _context.UserSavedPresets
            .Where(p => p.UserId == userId && p.Name == name &&
                        (excludePresetId == null || p.Id != excludePresetId))
            .AnyAsync(ct);
    }
}