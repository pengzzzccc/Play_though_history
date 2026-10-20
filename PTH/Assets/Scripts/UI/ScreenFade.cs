using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    /// <summary>
    /// Persistent full-screen black overlay used between levels. Creation is lazy:
    /// the first transition creates this object with DontDestroyOnLoad, so the
    /// overlay survives the scene that started the transition.
    /// </summary>
    public sealed class ScreenFade : MonoBehaviour
    {
        private const float OverlaySortingOrder = 1000f;

        public static ScreenFade Instance { get; private set; }

        private UIDocument document;
        private VisualElement overlay;
        private float opacity;
        private bool transitioning;

        public bool IsTransitioning => transitioning;

        internal static void Create()
        {
            if (Instance != null)
            {
                return;
            }

            var fadeObject = new GameObject("[ScreenFade]");
            DontDestroyOnLoad(fadeObject);
            fadeObject.AddComponent<ScreenFade>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Reuse the PanelSettings of the scene UI so the overlay renders in the
            // same UI Toolkit panel as the HUD. The high sorting order keeps it on
            // top of every scene UIDocument, including UIDocuments created later.
            var reference = FindAnyObjectByType<UIDocument>();
            if (reference == null || reference.panelSettings == null)
            {
                Debug.LogWarning("[ScreenFade] No UIDocument with PanelSettings found; level transitions will run without a fade.");
                return;
            }

            var uiObject = new GameObject("Fade UI");
            uiObject.SetActive(false);
            uiObject.transform.SetParent(transform, false);

            var uiDocument = uiObject.AddComponent<UIDocument>();
            uiDocument.panelSettings = reference.panelSettings;
            uiDocument.sortingOrder = OverlaySortingOrder;

            uiObject.SetActive(true);
            document = uiDocument;
        }

        private void Start()
        {
            if (document == null)
            {
                return;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[ScreenFade] The UIDocument has no root visual element; level transitions will run without a fade.");
                return;
            }

            root.pickingMode = PickingMode.Ignore;

            overlay = new VisualElement
            {
                name = "fade-overlay",
                pickingMode = PickingMode.Ignore
            };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            overlay.style.backgroundColor = Color.black;
            overlay.style.opacity = 0f;
            root.Add(overlay);
        }

        public bool Begin(string sceneName, float fadeOutDuration, float fadeInDuration)
        {
            if (transitioning || Game.IsLoading || Game.Phase != GamePhase.Exploring)
            {
                return false;
            }

            transitioning = true;
            StartCoroutine(Run(sceneName, Mathf.Max(0f, fadeOutDuration), Mathf.Max(0f, fadeInDuration)));
            return true;
        }

        private IEnumerator Run(string sceneName, float fadeOutDuration, float fadeInDuration)
        {
            // Let this component's Start run and build the overlay before fading.
            yield return null;

            yield return FadeTo(1f, fadeOutDuration);

            Game.LoadScene(sceneName, GamePhase.Exploring);

            if (!Game.IsLoading && Game.Phase == GamePhase.Exploring)
            {
                // Game.LoadScene silently refuses when another load is in progress;
                // the current scene is still showing, so fade back in instead of
                // leaving the player on a black screen.
                Debug.LogError("[ScreenFade] The scene load for '" + sceneName + "' did not start.");
                yield return FadeTo(0f, fadeInDuration);
                transitioning = false;
                yield break;
            }

            while (Game.IsLoading)
            {
                yield return null;
            }

            if (Game.Phase != GamePhase.Exploring)
            {
                Debug.LogError("[ScreenFade] Loading '" + sceneName + "' failed; the screen will fade back in.");
                yield return FadeTo(0f, fadeInDuration);
                transitioning = false;
                yield break;
            }

            // Give the loaded scene one frame so Player.Start can place the player
            // at PlayerSpawn before the screen fades back in.
            yield return null;

            yield return FadeTo(0f, fadeInDuration);
            transitioning = false;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (overlay == null || duration <= 0f)
            {
                opacity = target;
                if (overlay != null)
                {
                    overlay.style.opacity = target;
                }

                yield break;
            }

            var start = opacity;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                opacity = Mathf.SmoothStep(start, target, Mathf.Clamp01(elapsed / duration));
                overlay.style.opacity = opacity;
                yield return null;
            }

            opacity = target;
            overlay.style.opacity = target;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}
