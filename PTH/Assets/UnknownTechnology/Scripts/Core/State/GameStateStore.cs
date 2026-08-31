using System;
using UnknownTechnology.Core.Events;

namespace UnknownTechnology.Core.State
{
    public interface IGameStateStore
    {
        GameStateSnapshot Current { get; }
        bool TryTransition(GamePhase target, out string rejectionReason);
        bool TryPause(out string rejectionReason);
        bool TryResume(out string rejectionReason);
    }

    public sealed class GameStateStore : IGameStateStore
    {
        private readonly IEventBus eventBus;
        private GamePhase phase = GamePhase.Boot;
        private GamePhase resumePhase = GamePhase.Boot;

        public GameStateStore(IEventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public GameStateSnapshot Current => new(phase, resumePhase);

        public bool TryTransition(GamePhase target, out string rejectionReason)
        {
            if (target == phase)
            {
                rejectionReason = string.Empty;
                return true;
            }

            if (target == GamePhase.Paused)
            {
                return TryPause(out rejectionReason);
            }

            if (phase == GamePhase.Paused)
            {
                rejectionReason = "Paused state must be left through TryResume.";
                return false;
            }

            if (!CanTransition(phase, target))
            {
                rejectionReason = $"Transition {phase} -> {target} is not allowed.";
                return false;
            }

            Commit(target);
            rejectionReason = string.Empty;
            return true;
        }

        public bool TryPause(out string rejectionReason)
        {
            if (phase == GamePhase.Paused)
            {
                rejectionReason = string.Empty;
                return true;
            }

            if (!IsPausable(phase))
            {
                rejectionReason = $"Phase {phase} cannot be paused.";
                return false;
            }

            resumePhase = phase;
            Commit(GamePhase.Paused);
            rejectionReason = string.Empty;
            return true;
        }

        public bool TryResume(out string rejectionReason)
        {
            if (phase != GamePhase.Paused)
            {
                rejectionReason = "The game is not paused.";
                return false;
            }

            var target = resumePhase;
            if (!IsPausable(target))
            {
                rejectionReason = $"Resume phase {target} is invalid.";
                return false;
            }

            Commit(target);
            resumePhase = GamePhase.Boot;
            rejectionReason = string.Empty;
            return true;
        }

        private void Commit(GamePhase target)
        {
            var previous = phase;
            phase = target;
            eventBus.Publish(new GamePhaseChanged(previous, phase, resumePhase));
        }

        private static bool IsPausable(GamePhase candidate)
        {
            return candidate == GamePhase.Exploring ||
                   candidate == GamePhase.Dialogue ||
                   candidate == GamePhase.Restoration ||
                   candidate == GamePhase.Quiz;
        }

        private static bool CanTransition(GamePhase source, GamePhase target)
        {
            if (target == GamePhase.FatalError)
            {
                return source != GamePhase.FatalError;
            }

            return source switch
            {
                GamePhase.Boot => target == GamePhase.Loading || target == GamePhase.MainMenu || target == GamePhase.Exploring,
                GamePhase.MainMenu => target == GamePhase.Loading,
                GamePhase.Loading => target == GamePhase.MainMenu || target == GamePhase.Exploring || target == GamePhase.Completed,
                GamePhase.Exploring => target == GamePhase.Dialogue || target == GamePhase.Restoration ||
                                       target == GamePhase.Quiz || target == GamePhase.Loading || target == GamePhase.Completed,
                GamePhase.Dialogue => target == GamePhase.Exploring,
                GamePhase.Restoration => target == GamePhase.Exploring,
                GamePhase.Quiz => target == GamePhase.Exploring || target == GamePhase.Loading || target == GamePhase.Completed,
                GamePhase.Completed => target == GamePhase.Loading || target == GamePhase.MainMenu,
                GamePhase.FatalError => target == GamePhase.Loading || target == GamePhase.MainMenu,
                _ => false
            };
        }
    }
}
