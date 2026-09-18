namespace SchoolPortal.API.Services
{
    // Thrown when a formatted roll number can't be built yet — most
    // commonly because the student's class doesn't have a ClassCode set.
    // Distinct from DuplicateRollNumberException (a conflict) since this is
    // a setup gap the Admin needs to fix in Manage Classes, not a clash
    // between two students.
    public class RollNumberGenerationException : Exception
    {
        public RollNumberGenerationException(string message) : base(message) { }
    }
}
