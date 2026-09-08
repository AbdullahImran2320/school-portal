// PromotionServiceTests.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Services;
using Xunit;

namespace Backend.Tests
{
    // A fresh in-memory SQLite database per test (xUnit creates a new
    // instance of this class for every [Fact]) — no shared state, no test
    // ordering dependencies, and real transaction support that the actual
    // service relies on.
    public class PromotionServiceTests : IDisposable
    {
        private readonly SqliteInMemoryFixture _fixture;
        private readonly FakeFeeEngineService _feeEngine = new();
        private readonly PromotionService _sut;

        public PromotionServiceTests()
        {
            _fixture = new SqliteInMemoryFixture();
            _sut = new PromotionService(_fixture.Context, _feeEngine);
        }

        public void Dispose() => _fixture.Dispose();

        private Parent AddParent()
        {
            var parent = new Parent { FatherName = "Test Father", FatherMobile = "0300-0000000", Address = "Test Address" };
            _fixture.Context.Parents.Add(parent);
            _fixture.Context.SaveChanges();
            return parent;
        }

        private SchoolClass AddClass(string className, string section, int promotionOrder, string year = "2026")
        {
            var cls = new SchoolClass { ClassName = className, Section = section, AcademicYear = year, PromotionOrder = promotionOrder };
            _fixture.Context.Classes.Add(cls);
            _fixture.Context.SaveChanges();
            return cls;
        }

        private Student AddStudent(SchoolClass cls, Parent parent, string name = "Test Student")
        {
            var student = new Student
            {
                Name = name,
                BFormNumber = "00000-0000000-0",
                DateOfBirth = new DateTime(2015, 1, 1),
                Gender = "Male",
                AdmissionDate = new DateTime(2024, 1, 1),
                AdmissionStatus = AdmissionStatus.Admitted,
                ClassId = cls.ClassId,
                ParentId = parent.ParentId
            };
            _fixture.Context.Students.Add(student);
            _fixture.Context.SaveChanges();
            return student;
        }

        [Fact]
        public async Task Promotes_ToTheOnlySection_WhenNextGradeHasNoSections()
        {
            var parent = AddParent();
            var class1 = AddClass("Class 1", "", promotionOrder: 1);
            var class2 = AddClass("Class 2", "", promotionOrder: 2);
            var student = AddStudent(class1, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(1, result.PromotedCount);
            Assert.Empty(result.UnresolvedSections);
            var updated = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class2.ClassId, updated!.ClassId);
            Assert.Contains(_feeEngine.Calls, c => c.StudentId == student.StudentId && c.ClassId == class2.ClassId && c.AcademicYear == "2027");
        }

        [Fact]
        public async Task Promotes_ToSameNamedSection_WhenNextGradeHasMultipleSections()
        {
            var parent = AddParent();
            var class1A = AddClass("Class 1", "A", promotionOrder: 1);
            AddClass("Class 1", "B", promotionOrder: 1);
            var class2A = AddClass("Class 2", "A", promotionOrder: 2);
            AddClass("Class 2", "B", promotionOrder: 2);
            var student = AddStudent(class1A, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(1, result.PromotedCount);
            var updated = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class2A.ClassId, updated!.ClassId); // matched by section name, not just first available
        }

        [Fact]
        public async Task SectionMatch_IsCaseInsensitive()
        {
            var parent = AddParent();
            var class1 = AddClass("Class 1", "a", promotionOrder: 1); // lowercase
            AddClass("Class 1", "B", promotionOrder: 1);
            var class2A = AddClass("Class 2", "A", promotionOrder: 2); // uppercase
            AddClass("Class 2", "B", promotionOrder: 2);
            var student = AddStudent(class1, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(1, result.PromotedCount);
            var updated = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class2A.ClassId, updated!.ClassId);
        }

        [Fact]
        public async Task FlagsForManualReview_InsteadOfGuessing_WhenNoSectionNameMatches()
        {
            var parent = AddParent();
            var class1A = AddClass("Class 1", "A", promotionOrder: 1);
            AddClass("Class 1", "B", promotionOrder: 1);
            AddClass("Class 2", "Red", promotionOrder: 2);   // no section named "A" up here
            AddClass("Class 2", "Blue", promotionOrder: 2);
            var student = AddStudent(class1A, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(0, result.PromotedCount);
            var unresolved = Assert.Single(result.UnresolvedSections);
            Assert.Equal(student.StudentId, unresolved.StudentId);
            Assert.Equal(new[] { "Blue", "Red" }, unresolved.AvailableSectionsInNextGrade.OrderBy(s => s));

            // Nothing should have moved and no fee record generated for an
            // unresolved student — that's the whole point of not guessing.
            var unchanged = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class1A.ClassId, unchanged!.ClassId);
            Assert.DoesNotContain(_feeEngine.Calls, c => c.StudentId == student.StudentId);
        }

        [Fact]
        public async Task Graduates_StudentInHighestClass_WhenNoNextGradeExists()
        {
            var parent = AddParent();
            var class10 = AddClass("Class 10", "", promotionOrder: 13); // top of the sequence, nothing above it
            var student = AddStudent(class10, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(1, result.GraduatedCount);
            Assert.Equal(0, result.PromotedCount);
            var updated = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(AdmissionStatus.Graduated, updated!.AdmissionStatus);
        }

        [Fact]
        public async Task HeldBackStudent_StaysInSameClass_ButStillGetsNewYearFeeRecords()
        {
            var parent = AddParent();
            var class1 = AddClass("Class 1", "", promotionOrder: 1);
            AddClass("Class 2", "", promotionOrder: 2);
            var student = AddStudent(class1, parent);

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto
            {
                FromAcademicYear = "2026",
                ToAcademicYear = "2027",
                HoldBackStudentIds = new List<int> { student.StudentId }
            });

            Assert.Equal(1, result.HeldBackCount);
            Assert.Equal(0, result.PromotedCount);
            var updated = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class1.ClassId, updated!.ClassId); // repeats the same class
            Assert.Contains(_feeEngine.Calls, c => c.StudentId == student.StudentId && c.ClassId == class1.ClassId && c.AcademicYear == "2027");
        }

        [Fact]
        public async Task SkipsStudent_WhoAlreadyHasAFeeLedgerForTheTargetYear()
        {
            // Simulates running promotion twice by accident — the second run
            // should be a safe no-op for anyone already processed, not a
            // second promotion on top of the first.
            var parent = AddParent();
            var class1 = AddClass("Class 1", "", promotionOrder: 1);
            var class2 = AddClass("Class 2", "", promotionOrder: 2);
            var student = AddStudent(class1, parent);

            _fixture.Context.FeeLedgers.Add(new FeeLedger
            {
                StudentId = student.StudentId,
                MonthNumber = 1,
                Year = 2027,
                DueAmount = 5000
            });
            _fixture.Context.SaveChanges();

            var result = await _sut.PromoteAllAsync(new PromoteClassesDto { FromAcademicYear = "2026", ToAcademicYear = "2027" });

            Assert.Equal(1, result.AlreadyProcessedCount);
            Assert.Equal(0, result.PromotedCount);
            var unchanged = await _fixture.Context.Students.FindAsync(student.StudentId);
            Assert.Equal(class1.ClassId, unchanged!.ClassId); // untouched, not moved to class2
        }
    }
}
