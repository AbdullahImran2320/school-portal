using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Repositories;

namespace SchoolPortal.API.Services
{

    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IFeeEngineService _feeEngineService;
        private readonly SchoolPortalDbContext _context;

        public StudentService(IStudentRepository repository, IFeeEngineService feeEngineService, SchoolPortalDbContext context)
        {
            _repository = repository;
            _feeEngineService = feeEngineService;
            _context = context;
        }

        private async Task<string> GetAcademicYearForClassAsync(int classId)
        {
            var schoolClass = await _context.Classes.FindAsync(classId);
            // Falls back to the current calendar year only if the class
            // record is somehow missing — the class's own AcademicYear is
            // the real source of truth, not a hardcoded literal.
            return schoolClass?.AcademicYear ?? DateTime.Now.Year.ToString();
        }
        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _repository.GetAllAsync();
            return students.Select(MapToDto).ToList();
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int id)
        {
            var student = await _repository.GetByIdAsync(id);
            return student == null ? null : MapToDto(student);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            var student = new Student
            {
                Name = dto.Name,
                BFormNumber = dto.BFormNumber,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                AdmissionDate = dto.AdmissionDate,
                AdmissionStatus = Enum.Parse<AdmissionStatus>(dto.AdmissionStatus),
                ClassId = dto.ClassId,
                ParentId = dto.ParentId
            };

            var created = await _repository.AddAsync(student);
            var academicYear = await GetAcademicYearForClassAsync(created.ClassId);
            await _feeEngineService.GenerateFeeRecordsForStudentAsync(created.StudentId, created.ClassId, academicYear);

            var full = await _repository.GetByIdAsync(created.StudentId);
            return MapToDto(full!);
           
        }

        public async Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null) return false;

            student.Name = dto.Name;
            student.BFormNumber = dto.BFormNumber;
            student.DateOfBirth = dto.DateOfBirth;
            student.Gender = dto.Gender;
            student.AdmissionStatus = Enum.Parse<AdmissionStatus>(dto.AdmissionStatus);
            student.ClassId = dto.ClassId;

            return await _repository.UpdateAsync(student);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static StudentDto MapToDto(Student s) => new()
        {
            StudentId = s.StudentId,
            Name = s.Name,
            BFormNumber = s.BFormNumber,
            DateOfBirth = s.DateOfBirth,
            Gender = s.Gender,
            AdmissionDate = s.AdmissionDate,
            AdmissionStatus = s.AdmissionStatus.ToString(),
            ClassId = s.ClassId,
            ClassName = s.Class?.ClassName ?? "",
            ParentId = s.ParentId,
            FatherName = s.Parent?.FatherName ?? "",
            FatherMobile = s.Parent?.FatherMobile ?? "",
            MotherName = s.Parent?.MotherName,
            MotherMobile = s.Parent?.MotherMobile
        };

        public async Task<bool> SetDiscountAsync(int studentId, decimal amount, string? reason, bool applyToRemainingMonths)
        {
            var student = await _repository.GetByIdAsync(studentId);
            if (student == null) return false;

            student.MonthlyDiscountAmount = amount;
            student.DiscountReason = reason;
            await _repository.UpdateAsync(student);

            if (applyToRemainingMonths)
            {
                // Only touch months that haven't been paid yet — never retroactively
                // change a month that's already Paid or Partial, since that would
                // silently alter money that's already been collected and recorded.
                var now = DateTime.Now;
                var candidateMonths = await _context.FeeLedgers.Where(l =>
                    l.StudentId == studentId &&
                    l.Year == now.Year &&
                    l.MonthNumber >= now.Month &&
                    l.Status == LedgerStatus.Unpaid)
                    .ToListAsync();

                var unpaidFutureMonths = candidateMonths
                    .Where(l => FeeCalculator.IsApplicableMonth(student.AdmissionDate, l.MonthNumber, l.Year))
                    .ToList();

                foreach (var l in unpaidFutureMonths)
                {
                    l.DiscountAmount = amount;
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}