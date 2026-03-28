using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class SystemStylePresetRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<SystemStylePresetEntity, SystemStylePreset>(context, mapper),
        ISystemStylePresetRepository
{
    public async Task<IReadOnlyList<SystemStylePreset>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _context.SystemStylePresets
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<SystemStylePreset>>(entities);
    }

    public async Task<SystemStylePreset?> GetDefaultAsync(CancellationToken ct = default)
    {
        var entity = await _context.SystemStylePresets
            .FirstOrDefaultAsync(p => p.IsDefault, ct);
        return _mapper.Map<SystemStylePreset?>(entity);
    }
}