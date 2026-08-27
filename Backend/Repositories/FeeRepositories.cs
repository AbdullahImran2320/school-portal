// Repositories/IFeeComponentRepository.cs + FeeComponentRepository.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Repositories
{


    public class FeeComponentRepository : IFeeComponentRepository
    {
        private readonly SchoolPortalDbContext _context;
        public FeeComponentRepository(SchoolPortalDbContext context) => _context = context;

        public async Task<List<FeeComponent>> GetAllAsync() =>
            await _context.FeeComponents.Include(f => f.Class).ToListAsync();

        public async Task<List<FeeComponent>> GetByClassIdAsync(int classId) =>
            await _context.FeeComponents.Include(f => f.Class)
                .Where(f => f.ClassId == classId).ToListAsync();

        public async Task<FeeComponent?> GetByIdAsync(int id) =>
            await _context.FeeComponents.Include(f => f.Class)
                .FirstOrDefaultAsync(f => f.FeeComponentId == id);

        public async Task<FeeComponent> AddAsync(FeeComponent component)
        {
            _context.FeeComponents.Add(component);
            await _context.SaveChangesAsync();
            return component;
        }

        public async Task<bool> UpdateAsync(FeeComponent component)
        {
            _context.FeeComponents.Update(component);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var component = await _context.FeeComponents.FindAsync(id);
            if (component == null) return false;
            _context.FeeComponents.Remove(component);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}