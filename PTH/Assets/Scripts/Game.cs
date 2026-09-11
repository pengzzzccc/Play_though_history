using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology
{
    public enum GamePhase
    {
        Boot = 0,
        MainMenu = 1,
        Loading = 2,
        Exploring = 3,
        Paused = 4
    }

    public enum ControlScheme
    {
        KeyboardMouse = 0,
        Gamepad = 1
    }

    /// <summary>
    /// Global game state: the phase machine, the settings entry point and scene loads.
    /// Everything reads this statically; phases only change through the guarded methods.
    /// </summary>
    public static class Game
    {
        private static GameSettings settings;

        public static GameSettings Settings => settings ??= GameSettings.Load();
        public static GameBootstrap Runtime => GameBootstrap.Instance;
        public static GamePhase Phase { get; private set; } = GamePhase.Boot;
        public static GamePhase ResumePhase { get; private set; }
        public static bool IsLoading { get; private set; }

        public static bool SetPhase(GamePhase target)
        {
            if (target == Phase)
            {
                return true;
            }

            if (target == GamePhase.Paused)
            {
                return TryPause();
            }

            if (Phase == GamePhase.Paused && target != GamePhase.Loading)
            {
                Debug.LogWarning($"Paused must be left through {nameof(TryResume)}.");
                return false;
            }

            if (!CanTransition(Phase, target))
            {
                Debug.LogWarning($"Transition {Phase} -> {target} is not allowed.");
                return false;
            }

            if (Phase == GamePhase.Paused)
            {
                // Leaving pause directly into a scene load (save & quit):
                // there is nothing left to resume into afterwards.
                ResumePhase = GamePhase.Boot;
            }

            Commit(target);
            return true;
        }

        public static bool TryPause()
        {
            if (Phase == GamePhase.Paused)
            {
                return true;
            }

            if (!IsPausable(Phase))
            {
                Debug.LogWarning($"{Phase} cannot be paused.");
                return false;
            }

            ResumePhase = Phase;
            Commit(GamePhase.Paused);
            return true;
        }

        public static bool TryResume()
        {
            if (Phase != GamePhase.Paused)
            {
                return false;
            }

            var target = ResumePhase;
            if (!IsPausable(target))
            {
                Debug.LogWarning($"Resume phase {target} is invalid.");
                return false;
            }

            ResumePhase = GamePhase.Boot;
            Commit(target);
            return true;
        }

        public static void LoadScene(string sceneName, GamePhase phaseAfterLoad)
        {
            if (IsLoading)
            {
                Debug.LogWarning("Another scene load is already in progress.");
                return;
            }

            if (!SetPhase(GamePhase.Loading))
            {
                return;
            }

            IsLoading = true;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                IsLoading = false;
                Debug.LogError($"Could not start loading scene '{sceneName}'. Is it in Build Settings?");
                SetPhase(GamePhase.MainMenu);
                return;
            }

            operation.completed += _ =>
            {
                IsLoading = false;
                SetPhase(phaseAfterLoad);
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Phase = GamePhase.Boot;
            ResumePhase = GamePhase.Boot;
            IsLoading = false;
            settings = null;
        }

        private static void Commit(GamePhase target)
        {
            Phase = target;
            GameEvents.RaisePhaseChanged(target);
        }

        private static bool IsPausable(GamePhase phase)
        {
            return phase == GamePhase.Exploring;
        }

        private static bool CanTransition(GamePhase source, GamePhase target)
        {
            return source switch
            {
                GamePhase.Boot => target == GamePhase.Loading || target == GamePhase.MainMenu || target == GamePhase.Exploring,
                GamePhase.MainMenu => target == GamePhase.Loading,
                GamePhase.Loading => target == GamePhase.MainMenu || target == GamePhase.Exploring,
                GamePhase.Exploring => target == GamePhase.Loading,
                GamePhase.Paused => target == GamePhase.Loading,
                _ => false
            };
        }
    }
}
