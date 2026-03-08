namespace Domain.Interfaces.Repositories;

public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    void Update(T entity);
}
