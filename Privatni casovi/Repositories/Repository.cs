using Microsoft.EntityFrameworkCore;
using Privatni_casovi.Data;

namespace Privatni_casovi.Repositories;

/// <summary>
/// The shared implementation of <see cref="IRepository{T}"/>. Entities that carry an
/// <c>Aktivan</c> flag override <see cref="GetAllAsync"/> to filter on it.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _db;

    public Repository(ApplicationDbContext db)
    {
        _db = db;
    }

    protected DbSet<T> Set => _db.Set<T>();

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
        => await Set.ToListAsync();

    public virtual async Task<T?> GetByIdAsync(object id)
        => await Set.FindAsync(id);

    public void Add(T entity) => Set.Add(entity);

    public void Update(T entity) => Set.Update(entity);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
