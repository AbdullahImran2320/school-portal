using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Repositories
{
    public interface IFeeComponentRepository
    {
        Task<List<FeeComponent>> GetAllAsync();
        Task<List<FeeComponent>> GetByClassIdAsync(int classId);
        Task<FeeComponent?> GetByIdAsync(int id);
        Task<FeeComponent> AddAsync(FeeComponent component);
        Task<bool> UpdateAsync(FeeComponent component);
        Task<bool> DeleteAsync(int id);
    }
}