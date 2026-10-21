using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UnknownTechnology
{
    [RequireComponent(typeof(UIDocument), typeof(AudioSource))]
    public class ArtefactSlideshow : MonoBehaviour
    {
        [System.Serializable]
        public class Slide
        {
            public string title;
            public AudioClip narration;
            public Texture2D image;
        }

        [Tooltip("Assign Flashback_Slide_1.wav through Flashback_Slide_5.wav in order. Images are optional.")]
        [SerializeField] private Slide[] slides =
        {
            new Slide { title = "Flashback - Part 1" },
            new Slide { title = "Flashback - Part 2" },
            new Slide { title = "Flashback - Part 3" },
            new Slide { title = "Flashback - Part 4" },
            new Slide { title = "Flashback - Part 5" }
        };

        public bool IsOpen { get; private set; }

        private AudioSource narrationSource;
        private VisualElement overlay;
        private VisualElement hud;
        private VisualElement slideImage;
        private VisualElement audioPlaceholder;
        private Label slideTitle;
        private Label slideCounter;
        private Label narrationHint;
        private Button nextButton;
        private Button skipButton;
        private StyleEnum<DisplayStyle> previousHudDisplay;
        private int currentSlide;
        private bool bound;

        private void Awake()
        {
            narrationSource = GetComponent<AudioSource>();
            narrationSource.playOnAwake = false;
            narrationSource.loop = false;
            narrationSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            Bind();
            GameEvents.CancelPressed += FinishOrSkip;
            GameEvents.PhaseChanged += HandlePhaseChanged;
        }

        private void Start()
        {
            if (!Bind())
                Debug.LogError("ArtefactSlideshow: flashback controls are missing from the UIDocument.", this);
        }

        private void Update()
        {
            // The custom UI Cancel action uses Backspace; also support the quiz's Escape shortcut.
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                FinishOrSkip();
        }

        private bool Bind()
        {
            if (bound)
                return true;

            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
                return false;

            overlay = root.Q("flashback-overlay");
            hud = root.Q("hud");
            slideImage = root.Q("flashback-image");
            audioPlaceholder = root.Q("flashback-audio-placeholder");
            slideTitle = root.Q<Label>("flashback-title");
            slideCounter = root.Q<Label>("flashback-counter");
            narrationHint = root.Q<Label>("flashback-narration-hint");
            nextButton = root.Q<Button>("flashback-next-button");
            skipButton = root.Q<Button>("flashback-skip-button");

            if (overlay == null || hud == null || slideImage == null || audioPlaceholder == null ||
                slideTitle == null || slideCounter == null || narrationHint == null ||
                nextButton == null || skipButton == null)
                return false;

            overlay.AddToClassList("hidden");
            nextButton.clicked += Next;
            skipButton.clicked += FinishOrSkip;
            bound = true;
            return true;
        }

        public void Begin()
        {
            if (IsOpen || Game.Phase != GamePhase.Exploring || CanvasQuizUI.IsQuizOpen || !Bind())
                return;

            if (slides == null || slides.Length == 0)
            {
                Debug.LogWarning("ArtefactSlideshow: no slides are configured.", this);
                return;
            }

            if (!Game.SetPhase(GamePhase.Flashback))
                return;

            IsOpen = true;
            currentSlide = 0;
            previousHudDisplay = hud.style.display;
            hud.style.display = DisplayStyle.None;
            overlay.RemoveFromClassList("hidden");
            ShowSlide();
            nextButton.schedule.Execute(() => nextButton.Focus());
        }

        private void Next()
        {
            if (!IsOpen)
                return;

            if (currentSlide >= slides.Length - 1)
            {
                FinishOrSkip();
                return;
            }

            currentSlide++;
            ShowSlide();
        }

        private void ShowSlide()
        {
            var slide = slides[currentSlide];
            narrationSource.Stop();
            narrationSource.clip = slide?.narration;
            if (narrationSource.clip != null)
                narrationSource.Play();

            slideTitle.text = string.IsNullOrWhiteSpace(slide?.title)
                ? $"Flashback - Part {currentSlide + 1}" : slide.title;
            slideCounter.text = $"PART {currentSlide + 1} OF {slides.Length}";
            nextButton.text = currentSlide == slides.Length - 1 ? "Finish" : "Next";
            narrationHint.text = narrationSource.clip != null
                ? $"Listen to the narration, then choose {nextButton.text} when ready."
                : "No narration available for this part.";

            var texture = slide?.image;
            slideImage.style.backgroundImage = new StyleBackground(texture);
            slideImage.style.display = texture != null ? DisplayStyle.Flex : DisplayStyle.None;
            audioPlaceholder.style.display = texture == null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void FinishOrSkip()
        {
            if (!IsOpen)
                return;
            Close();
            GameEvents.RaiseFlashbackFinishedOrSkipped(this);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            narrationSource.Stop();
            narrationSource.clip = null;
            overlay.AddToClassList("hidden");
            hud.style.display = previousHudDisplay;

            if (Game.Phase == GamePhase.Flashback)
                Game.SetPhase(GamePhase.Exploring);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (IsOpen && phase != GamePhase.Flashback)
                Close();
        }

        private void OnDisable()
        {
            Close();
            GameEvents.CancelPressed -= FinishOrSkip;
            GameEvents.PhaseChanged -= HandlePhaseChanged;
            if (bound)
            {
                nextButton.clicked -= Next;
                skipButton.clicked -= FinishOrSkip;
                bound = false;
            }
        }
    }
}
