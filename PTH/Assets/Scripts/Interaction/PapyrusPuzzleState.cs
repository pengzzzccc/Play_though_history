using System;

namespace UnknownTechnology
{
    public sealed class PapyrusPuzzleState
    {
        public const int MaximumChances = 3;
        private readonly string[] required;
        private readonly string[] answers;
        private readonly bool[] solved;
        public bool IsUnlocked { get; private set; }
        public bool IsStarted { get; private set; }
        public int CorrectCount { get; private set; }
        public int PlacedCount { get; private set; }
        public int ChancesUsed { get; private set; }
        public bool NeedsNarrationReplay => ChancesUsed >= MaximumChances;
        public bool CanCheck => IsStarted && !IsComplete && PlacedCount == required.Length;
        public bool IsComplete => CorrectCount == required.Length;

        public PapyrusPuzzleState(string[] requiredIds)
        {
            if (requiredIds == null || requiredIds.Length == 0)
                throw new ArgumentException("A puzzle needs at least one blank.", nameof(requiredIds));
            required = (string[])requiredIds.Clone();
            answers = new string[required.Length];
            solved = new bool[required.Length];
        }

        public void Unlock()
        {
            if (!NeedsNarrationReplay) IsUnlocked = true;
        }

        public bool ResetAfterNarration()
        {
            if (!NeedsNarrationReplay) return false;
            Array.Clear(answers, 0, answers.Length);
            Array.Clear(solved, 0, solved.Length);
            CorrectCount = 0;
            PlacedCount = 0;
            ChancesUsed = 0;
            IsStarted = false;
            IsUnlocked = true;
            return true;
        }
        public bool TryStart()
        {
            if (!IsUnlocked || IsStarted) return false;
            IsStarted = true;
            return true;
        }
        public bool IsSolved(int index) => index >= 0 && index < solved.Length && solved[index];

        public bool CanPlace(int index, string answerId) => IsStarted && !IsComplete && index >= 0 &&
            index < required.Length && !string.IsNullOrEmpty(answerId);

        public bool TryPlace(int index, string answerId)
        {
            if (!CanPlace(index, answerId))
                return false;
            if (answers[index] == answerId) return true;
            if (answers[index] == null) PlacedCount++;
            if (solved[index]) CorrectCount--;
            solved[index] = false;
            answers[index] = answerId;
            return true;
        }

        public void RemoveAnswer(int index)
        {
            if (IsComplete || index < 0 || index >= answers.Length || answers[index] == null) return;
            answers[index] = null;
            PlacedCount--;
            if (solved[index]) CorrectCount--;
            solved[index] = false;
        }

        public bool CheckAnswers()
        {
            if (!CanCheck) return false;
            CorrectCount = 0;
            for (var i = 0; i < required.Length; i++)
            {
                solved[i] = string.Equals(required[i], answers[i], StringComparison.Ordinal);
                if (solved[i]) CorrectCount++;
            }
            if (!IsComplete)
            {
                ChancesUsed++;
                if (NeedsNarrationReplay)
                {
                    IsUnlocked = false;
                    IsStarted = false;
                }
            }
            return true;
        }
    }
}
