namespace SchoolPortal.API.Services
{
    // Thrown when a roll number position is explicitly assigned (create or
    // SetRollNumberAsync) but another student in the same class already
    // has that position.
    public class DuplicateRollNumberException : Exception
    {
        public int RollNumberSequence { get; }

        public DuplicateRollNumberException(int rollNumberSequence)
            : base($"Position {rollNumberSequence} is already taken by another student in this class.")
        {
            RollNumberSequence = rollNumberSequence;
        }
    }
}
