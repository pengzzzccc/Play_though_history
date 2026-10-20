using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Entry point for level transitions. Owns the global busy check and creates
    /// the persistent ScreenFade overlay on demand.
    /// </summary>
    public static class SceneTransition
    {
        public static bool IsTransitioning => ScreenFade.Instance != null && ScreenFade.Instance.IsTransitioning;

        public static bool RequestTransition(string sceneName, float fadeOutDuration, float fadeInDuration)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneTransition: no target scene was provided.");
                return false;
            }

            if (IsTransitioning || Game.IsLoading)
            {
                return false;
            }

            if (Game.Phase != GamePhase.Exploring)
            {
                Debug.LogWarning("SceneTransition: can only change levels while exploring (current phase: " + Game.Phase + ").");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("SceneTransition: scene '" + sceneName + "' cannot be loaded. Add it to Build Settings first.");
                return false;
            }

            if (ScreenFade.Instance == null)
            {
                ScreenFade.Create();
            }

            if (ScreenFade.Instance == null)
            {
                Debug.LogError("SceneTransition: could not create the screen fade runner.");
                return false;
            }

            return ScreenFade.Instance.Begin(sceneName, fadeOutDuration, fadeInDuration);
        }
    }
}
