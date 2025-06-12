using Application.Interfaces;

namespace Infrastructure.Data;

internal sealed class EfRepository<T>(AppDbContext db) : IRepository<T>
    where T : class
{
    public ValueTask<T?> FindAsync(int id, CancellationToken ct) 
        => db.FindAsync<T>([id], ct);

    public void Add(T entity) => db.Add(entity);
    
    public void Remove(T entity) => db.Remove(entity);
    
    public IQueryable<T> GetAll() => db.Set<T>();
}