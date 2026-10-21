using System;

namespace UnknownTechnology
{
    public sealed class PapyrusPuzzleState
    {
        private readonly string[] required;
        private readonly bool[] solved;
        public bool IsUnlocked { get; private set; }
        public bool IsStarted { get; private set; }
        public int CorrectCount { get; private set; }
        public bool IsComplete => CorrectCount == required.Length;

        public PapyrusPuzzleState(string[] requiredIds)
        {
            if (requiredIds == null || requiredIds.Length == 0)
                throw new ArgumentException("A puzzle needs at least one blank.", nameof(requiredIds));
            required = (string[])requiredIds.Clone();
            solved = new bool[required.Length];
        }

        public void Unlock() => IsUnlocked = true;
        public bool TryStart()
        {
            if (!IsUnlocked || IsStarted) return false;
            IsStarted = true;
            return true;
        }
        public bool IsSolved(int index) => index >= 0 && index < solved.Length && solved[index];

        public bool CanPlace(int index, string answerId) => IsStarted && index >= 0 &&
            index < required.Length && !solved[index] && !string.IsNullOrEmpty(answerId) &&
            string.Equals(required[index], answerId, StringComparison.Ordinal);

        public bool TryPlace(int index, string answerId)
        {
            if (!CanPlace(index, answerId))
                return false;
            solved[index] = true;
            CorrectCount++;
            return true;
        }
    }
}
