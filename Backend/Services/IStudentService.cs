using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Repositories;

namespace SchoolPortal.API.Services
{
    public interface IStudentService
    {
        Task<List<StudentDto>> GetAllStudentsAsync();
        Task<StudentDto?> GetStudentByIdAsync(int id);
        Task<StudentDto> CreateStudentAsync(CreateStudentDto dto);
        Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto);
        Task<bool> DeleteStudentAsync(int id);
        // Interface
        Task<bool> SetDiscountAsync(int studentId, decimal amount, string? reason, bool applyToRemainingMonths);
    }
}