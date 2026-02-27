using Microsoft.EntityFrameworkCore;
using EasyParking.Models;

namespace EasyParking.Services;

public class CrudService<T> where T : class
{
    private readonly Data.AppDbContext _context;
    public CrudService(Data.AppDbContext context) => _context = context;

    public async Task<List<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();
    public async Task<T?> GetByIdAsync(int id) => await _context.Set<T>().FindAsync(id);
    
    public async Task AddAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
