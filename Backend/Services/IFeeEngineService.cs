using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IFeeEngineService
    {
        Task GenerateFeeRecordsForStudentAsync(int studentId, int classId, string academicYear);
    }
}