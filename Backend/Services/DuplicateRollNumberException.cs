namespace SchoolPortal.API.Services
{
    // Thrown when a roll number is explicitly assigned (create or
    // SetRollNumberAsync) but another student already has it.
    public class DuplicateRollNumberException : Exception
    {
        public int RollNumber { get; }

        public DuplicateRollNumberException(int rollNumber)
            : base($"Roll number {rollNumber} is already assigned to another student.")
        {
            RollNumber = rollNumber;
        }
    }
}
