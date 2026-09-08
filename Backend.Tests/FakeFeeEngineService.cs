// FakeFeeEngineService.cs
using SchoolPortal.API.Services;

namespace Backend.Tests
{
    // PromotionService's job is deciding WHICH class a student moves to —
    // not generating fee records, which is FeeEngineService's own job and
    // has its own tests to write separately. This fake just records what it
    // was asked to do, so promotion tests can assert "fee generation was
    // triggered for the right student/class/year" without needing a full
    // FeeComponent setup for every test.
    public class FakeFeeEngineService : IFeeEngineService
    {
        public List<(int StudentId, int ClassId, string AcademicYear)> Calls { get; } = new();

        public Task GenerateFeeRecordsForStudentAsync(int studentId, int classId, string academicYear)
        {
            Calls.Add((studentId, classId, academicYear));
            return Task.CompletedTask;
        }
    }
}
