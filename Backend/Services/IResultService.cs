using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IResultService
    {
        Task<ResultDto> RecordResultAsync(RecordResultDto dto);
        Task<ReportCardDto?> GetReportCardAsync(int studentId, int examId);
        Task<List<ExistingResultDto>> GetExistingResultsAsync(int examId, int subjectId);
    }
}