using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Core;
using Payment.Api.Core.Interface;
using Payment.Api.Infraestructura.Data;

public class BaseService<T> : IService<T>
    where T : Entity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseService(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    protected IQueryable<T> Query =>
    _dbSet.Where(e => !e.IsDeleted);

    protected IQueryable<T> ReadOnlyQuery =>
    Query.AsNoTracking();
    public async Task<List<T>> GetAllAsync()
    {

        return await ReadOnlyQuery.ToListAsync();
    }
        
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await ReadOnlyQuery.FirstOrDefaultAsync(e=> e.Id == id);
    }

    protected async Task<T?> GetTrackedByIdAsync(Guid id)
    {
        return await Query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> DeleteById(Guid id)
    {
        var entity = await GetTrackedByIdAsync(id);

        if (entity is null)
            return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }
}