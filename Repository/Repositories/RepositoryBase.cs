using AutoMapper;
using Domain.Interfaces.Repositories;
using EntityModels;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public abstract class RepositoryBase<TEntity, TDomain>(AppDbContext context, IMapper mapper)
    : IRepository<TDomain>
    where TEntity : class, IEntity
{
    protected readonly AppDbContext _context = context;
    protected readonly IMapper _mapper = mapper;

    public async Task<TDomain?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, ct);
        return _mapper.Map<TDomain?>(entity);
    }

    public async Task AddAsync(TDomain entity, CancellationToken ct = default)
    {
        var entityModel = _mapper.Map<TEntity>(entity);
        await _context.Set<TEntity>().AddAsync(entityModel, ct);
    }

    public void Remove(TDomain entity)
    {
        var entityModel = _mapper.Map<TEntity>(entity);
        _context.Remove(entityModel);
    }

    public void Update(TDomain entity)
    {
        var entityModel = _mapper.Map<TEntity>(entity);
        _context.Update(entityModel);
    }
}