namespace Application.Interfaces;

public interface IRepository<T> where T : class
{
    ValueTask<T?> FindAsync(int id, CancellationToken ct = default);
    void Add(T entity);
}