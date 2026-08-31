namespace UnknownTechnology.Core.State
{
    public enum GamePhase
    {
        Boot = 0,
        MainMenu = 1,
        Loading = 2,
        Exploring = 3,
        Dialogue = 4,
        Restoration = 5,
        Quiz = 6,
        Paused = 7,
        Completed = 8,
        FatalError = 9
    }

    public readonly struct GameStateSnapshot
    {
        public GameStateSnapshot(GamePhase phase, GamePhase resumePhase)
        {
            Phase = phase;
            ResumePhase = resumePhase;
        }

        public GamePhase Phase { get; }
        public GamePhase ResumePhase { get; }
        public bool IsPaused => Phase == GamePhase.Paused;
        public bool AllowsGameplayInput => Phase == GamePhase.Exploring;
    }
}
