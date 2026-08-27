// Services/IResultService.cs + ResultService.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
  
    public class ResultService : IResultService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly double _passingPercentage;

        public ResultService(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _passingPercentage = config.GetValue<double>("AcademicSettings:PassingPercentage");
        }

        private string PassFail(int marks, int total) =>
            total > 0 && (marks * 100.0 / total) >= _passingPercentage ? "Pass" : "Fail";

        public async Task<ResultDto> RecordResultAsync(RecordResultDto dto)
        {
            var result = new Result
            {
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                ExamId = dto.ExamId,
                MarksObtained = dto.MarksObtained,
                TotalMarks = dto.TotalMarks
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            var percentage = dto.TotalMarks > 0 ? dto.MarksObtained * 100.0 / dto.TotalMarks : 0;

            return new ResultDto
            {
                ResultId = result.ResultId,
                SubjectName = subject?.SubjectName ?? "",
                MarksObtained = dto.MarksObtained,
                TotalMarks = dto.TotalMarks,
                Percentage = Math.Round(percentage, 1),
                PassFail = PassFail(dto.MarksObtained, dto.TotalMarks)
            };
        }

        public async Task<ReportCardDto?> GetReportCardAsync(int studentId, int examId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var exam = await _context.Exams.FindAsync(examId);
            if (student == null || exam == null) return null;

            var results = await _context.Results
                .Include(r => r.Subject)
                .Where(r => r.StudentId == studentId && r.ExamId == examId)
                .ToListAsync();

            var subjectResults = results.Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                SubjectName = r.Subject.SubjectName,
                MarksObtained = r.MarksObtained,
                TotalMarks = r.TotalMarks,
                Percentage = Math.Round(r.TotalMarks > 0 ? r.MarksObtained * 100.0 / r.TotalMarks : 0, 1),
                PassFail = PassFail(r.MarksObtained, r.TotalMarks)
            }).ToList();

            var totalObtained = results.Sum(r => r.MarksObtained);
            var totalMax = results.Sum(r => r.TotalMarks);
            var overallPercentage = totalMax > 0 ? Math.Round(totalObtained * 100.0 / totalMax, 1) : 0;

            // Every subject must individually pass — a student who scores
            // well overall but fails one subject is still an overall Fail.
            // This is the common convention in Pakistani school reporting
            // and matters a lot more to parents than the raw average does.
            var overallResult = subjectResults.All(s => s.PassFail == "Pass") ? "Pass" : "Fail";

            return new ReportCardDto
            {
                StudentId = studentId,
                StudentName = student.Name,
                ExamName = exam.ExamName,
                Term = exam.Term,
                Subjects = subjectResults,
                OverallPercentage = overallPercentage,
                OverallResult = overallResult
            };
        }
    }
}