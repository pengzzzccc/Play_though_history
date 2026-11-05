using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Wires the hand-authored wall screen to the core sceptre mechanic.
    /// Visuals, card positions and text are editable prefab children.
    /// </summary>
    public class PapyrusWallPuzzle : MonoBehaviour
    {
        [SerializeField] private ArtefactSlideshow flashback;
        [SerializeField] private CarryableItem[] fragments;
        [SerializeField] private ItemSlot[] blanks;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject screenContent;
        [SerializeField] private GameObject startPrompt;
        [SerializeField] private Graphic powerIndicator;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text paragraphText;
        [SerializeField] private AudioClip correctAnswerSound;
        [SerializeField] private AudioClip incorrectAnswerSound;
        [SerializeField] private Color correctColour = new Color(0.59f, 0.78f, 0.48f);
        [SerializeField] private Color incorrectColour = new Color(0.93f, 0.48f, 0.40f);

        private PapyrusPuzzleState state;
        private Vector3[] homes;
        private Quaternion[] rotations;
        private bool[] pendingReturn;
        private bool[] returning;
        private Material[] fragmentMaterials;
        private Color[] originalColours;
        private readonly GuidancePrompt guidance = new GuidancePrompt();
        private string paragraphMarkup;
        private string[] answerLabels;
        private RectTransform[] answerOutlines;
        private Image[][] outlineEdges;
        private Button checkButton;
        private BoxCollider checkButtonCollider;
        private TMP_Text chancesText;
        private TMP_Text standbyText;
        private string standbyPrompt;
        private bool checkingAnswers;
        private bool resettingPuzzle;
        private Scepter scepter;
        private AudioManager audioManager;
        private readonly Dictionary<CarryableItem, Action> restoreAnswerSizes = new Dictionary<CarryableItem, Action>();
        private readonly List<Renderer> screenRenderers = new List<Renderer>();
        private readonly List<Canvas> screenCanvases = new List<Canvas>();

        public bool IsUnlocked => state != null && state.IsUnlocked;
        public bool IsStarted => state != null && state.IsStarted;
        public bool IsComplete => state != null && state.IsComplete;
        public bool IsCheckingAnswers => checkingAnswers;
        public bool CanCheck
        {
            get
            {
                if (state == null || !state.CanCheck || checkingAnswers ||
                    Game.Phase != GamePhase.Exploring || CanvasQuizUI.IsQuizOpen) return false;
                foreach (var item in fragments)
                    if (item.IsCarried || item.IsFlying) return false;
                return true;
            }
        }
        public bool CanBegin => IsUnlocked && !IsStarted &&
            Game.Phase == GamePhase.Exploring && !CanvasQuizUI.IsQuizOpen;

        private void Awake()
        {
            if (fragments == null || fragments.Length == 0 || blanks == null || blanks.Length == 0)
            {
                Debug.LogError("PapyrusWallPuzzle: assign the fragments and blanks in the Inspector.", this);
                enabled = false;
                return;
            }

            var required = new string[blanks.Length];
            for (var i = 0; i < blanks.Length; i++)
            {
                var index = i;
                required[i] = blanks[i].RequiredItemId;
                blanks[i].PlacementValidator = item => Validate(index, item);
                blanks[i].AutoCheckPlacement = false;
            }
            state = new PapyrusPuzzleState(required);
            CreateCheckButton();
            CreateChancesCounter();
            standbyText = startPrompt != null ? startPrompt.GetComponentInChildren<TMP_Text>(true) : null;
            standbyPrompt = standbyText != null ? standbyText.text : null;
            paragraphMarkup = paragraphText != null ? paragraphText.text : null;
            CreateAnswerOutlines();
            answerLabels = new string[fragments.Length];
            homes = new Vector3[fragments.Length];
            rotations = new Quaternion[fragments.Length];
            pendingReturn = new bool[fragments.Length];
            returning = new bool[fragments.Length];
            fragmentMaterials = new Material[fragments.Length];
            originalColours = new Color[fragments.Length];
            for (var i = 0; i < fragments.Length; i++)
            {
                var label = fragments[i].GetComponentInChildren<TMP_Text>();
                answerLabels[i] = label != null ? Regex.Replace(label.text, "<[^>]*>", string.Empty) : fragments[i].ItemId;
                homes[i] = transform.InverseTransformPoint(fragments[i].transform.position);
                rotations[i] = Quaternion.Inverse(transform.rotation) * fragments[i].transform.rotation;
                fragmentMaterials[i] = fragments[i].GetComponentInChildren<Renderer>().material;
                originalColours[i] = fragmentMaterials[i].GetColor("_BaseColor");
            }

            if (paragraphText != null)
                foreach (var blank in blanks) HideVisualChildren(blank.transform);

            if (flashback == null)
                foreach (var candidate in FindObjectsByType<ArtefactSlideshow>())
                    if (candidate.gameObject.scene == gameObject.scene) { flashback = candidate; break; }

            // Hide only the display visuals. Keep the core toy components awake
            // so their physics references are ready when the TV switches on.
            RegisterScreenVisuals(screenContent);
            foreach (var item in fragments) RegisterScreenVisuals(item.gameObject);
            foreach (var slot in blanks) RegisterScreenVisuals(slot.gameObject);
            SetInteraction(false);
            SetScreenPower(false);
        }

        private void RegisterScreenVisuals(GameObject content)
        {
            if (content == null) return;
            foreach (var renderer in content.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) screenRenderers.Add(renderer);
            foreach (var canvas in content.GetComponentsInChildren<Canvas>(true))
                if (canvas.enabled) screenCanvases.Add(canvas);
        }

        private void CreateCheckButton()
        {
            if (screenContent == null) return;
            var image = RuntimeUI.Create<Image>("Check answers", screenContent.transform);
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(170, 46);
            rect.anchoredPosition3D = new Vector3(470, -310, -2);
            image.color = new Color(0.25f, 0.38f, 0.25f);
            image.raycastTarget = false;
            checkButton = image.gameObject.AddComponent<Button>();
            checkButton.targetGraphic = image;
            checkButton.interactable = false;
            checkButton.onClick.AddListener(CheckAnswers);
            checkButtonCollider = image.gameObject.AddComponent<BoxCollider>();
            checkButtonCollider.size = new Vector3(170, 46, 4);
            checkButtonCollider.enabled = false;

            var font = feedbackText != null ? feedbackText.font : paragraphText != null ? paragraphText.font : TMP_Settings.defaultFontAsset;
            var label = RuntimeUI.CreateText("Label", rect, font, 24, TextAlignmentOptions.Center);
            RuntimeUI.Stretch(label.rectTransform);
            label.text = "Check";
        }

        private void SetScreenPower(bool powered)
        {
            foreach (var renderer in screenRenderers) renderer.enabled = powered;
            foreach (var canvas in screenCanvases) canvas.enabled = powered;
            if (lockedOverlay != null) lockedOverlay.SetActive(!powered);
            if (startPrompt != null) startPrompt.SetActive(!powered && (IsUnlocked || state.NeedsNarrationReplay));
            if (standbyText != null)
                standbyText.text = state.NeedsNarrationReplay
                    ? "3/3 chances used.\nWatch the artefact narration again to retry."
                    : standbyPrompt;
            if (powerIndicator != null)
                powerIndicator.color = powered ? new Color(0.25f, 0.9f, 0.5f) :
                    IsUnlocked ? new Color(1f, 0.65f, 0.15f) : new Color(0.65f, 0.08f, 0.04f);
        }

        private void CreateChancesCounter()
        {
            if (screenContent == null) return;
            var font = feedbackText != null ? feedbackText.font : TMP_Settings.defaultFontAsset;
            chancesText = RuntimeUI.CreateText("Chances counter", screenContent.transform, font, 36, TextAlignmentOptions.Center);
            chancesText.rectTransform.sizeDelta = new Vector2(300, 52);
            chancesText.rectTransform.anchoredPosition3D = new Vector3(200, -310, -2);
            chancesText.fontStyle = FontStyles.Bold;
            if (progressText != null)
            {
                // Keep the progress under the paragraph, clear of the larger counter and Check button.
                progressText.rectTransform.anchoredPosition = new Vector2(-275, -315);
                progressText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 505);
            }
            UpdateChances();
        }

        private void UpdateChances()
        {
            if (chancesText == null) return;
            chancesText.text = state.ChancesUsed + "/" + PapyrusPuzzleState.MaximumChances + " Chances";
            chancesText.color = state.ChancesUsed > 0 ? new Color(0.50f, 0.08f, 0.04f) : new Color(0.09f, 0.065f, 0.04f);
        }

        private void CreateAnswerOutlines()
        {
            if (paragraphText == null) return;
            answerOutlines = new RectTransform[blanks.Length];
            outlineEdges = new Image[blanks.Length][];
            for (var gap = 0; gap < blanks.Length; gap++)
            {
                var rect = RuntimeUI.CreateRect($"Answer {gap + 1} outline", paragraphText.transform);
                rect.anchorMin = rect.anchorMax = paragraphText.rectTransform.pivot;
                answerOutlines[gap] = rect;
                outlineEdges[gap] = new Image[4];
                for (var edge = 0; edge < 4; edge++)
                {
                    var image = RuntimeUI.Create<Image>("Border", rect);
                    image.color = Color.black;
                    image.raycastTarget = false;
                    var horizontal = edge < 2;
                    var side = edge % 2;
                    image.rectTransform.anchorMin = horizontal ? new Vector2(0, side) : new Vector2(side, 0);
                    image.rectTransform.anchorMax = horizontal ? new Vector2(1, side) : new Vector2(side, 1);
                    image.rectTransform.sizeDelta = horizontal ? new Vector2(0, 1.2f) : new Vector2(1.2f, 0);
                    image.rectTransform.anchoredPosition = Vector2.zero;
                    outlineEdges[gap][edge] = image;
                }
            }
        }

        private void Start()
        {
            scepter = FindAnyObjectByType<Scepter>();
            audioManager = FindAnyObjectByType<AudioManager>();
            RefreshParagraph();
        }

        private void RefreshParagraph()
        {
            if (paragraphText == null || paragraphMarkup == null)
                return;

            // Display the player's actual choice, including incorrect answers, in the
            // paragraph's own font and baseline. Reserve space for the surrounding box.
            paragraphText.text = Regex.Replace(paragraphMarkup,
                "<link=\"gap-(?<gap>\\d+)\">(?<nobr><nobr>)?<color=#00000000>(?<answer>.*?)</color>(?:</nobr>)?</link>",
                FormatParagraphGap, RegexOptions.Singleline);
            Canvas.ForceUpdateCanvases();
            paragraphText.ForceMeshUpdate();

            // Measure the actual wrapped text so changing the font or paragraph
            // in the Inspector also moves the physical sceptre targets with it.
            var info = paragraphText.textInfo;
            for (var linkIndex = 0; linkIndex < info.linkCount; linkIndex++)
            {
                var link = info.linkInfo[linkIndex];
                if (!link.GetLinkID().StartsWith("gap-", StringComparison.Ordinal) ||
                    !int.TryParse(link.GetLinkID().Substring(4), out var gap) ||
                    gap < 0 || gap >= blanks.Length || link.linkTextLength == 0)
                    continue;

                PositionBlank(gap, link);
            }
            Physics.SyncTransforms();
        }

        private void PositionBlank(int gap, TMP_LinkInfo link)
        {
            var info = paragraphText.textInfo;
            var first = info.characterInfo[link.linkTextfirstCharacterIndex];
            var last = info.characterInfo[link.linkTextfirstCharacterIndex + link.linkTextLength - 1];
            var top = first.ascender;
            var bottom = first.descender;
            for (var c = 1; c < link.linkTextLength; c++)
            {
                var character = info.characterInfo[link.linkTextfirstCharacterIndex + c];
                top = Mathf.Max(top, character.ascender);
                bottom = Mathf.Min(bottom, character.descender);
            }

            var target = blanks[gap].transform;
            var center = new Vector3((first.origin + last.xAdvance) * 0.5f, (top + bottom) * 0.5f, 0);
            target.position = paragraphText.transform.TransformPoint(center) +
                transform.TransformVector(new Vector3(0, 0, -0.04f));
            var width = paragraphText.transform.TransformVector(Vector3.right * (last.xAdvance - first.origin)).magnitude / target.lossyScale.x;
            var hitPadding = paragraphText.transform.TransformVector(Vector3.right * 12).magnitude / target.lossyScale.x;
            var height = paragraphText.transform.TransformVector(Vector3.up * (top - bottom + 4)).magnitude / target.lossyScale.y;
            var surface = target.GetComponentInChildren<MeshRenderer>(true);
            if (surface != null)
                surface.transform.localScale = new Vector3(width, height, surface.transform.localScale.z);
            var collider = target.GetComponent<BoxCollider>();
            if (collider != null)
                collider.size = new Vector3(width + hitPadding, height, collider.size.z);

            var outline = answerOutlines[gap];
            outline.anchoredPosition3D = new Vector3(center.x, center.y, -0.01f);
            outline.sizeDelta = new Vector2(last.xAdvance - first.origin + 4, top - bottom + 4);
            foreach (var edge in outlineEdges[gap]) edge.color = Color.black;

            var item = blanks[gap].OccupiedItem;
            if (item != null)
            {
                item.transform.SetPositionAndRotation(blanks[gap].SnapPosition + blanks[gap].PlaceOffset, blanks[gap].SnapRotation);
                FitAnswerToBlank(item, blanks[gap]);
            }
        }

        private string FormatParagraphGap(Match match)
        {
            var gap = int.Parse(match.Groups["gap"].Value);
            if (gap < 0 || gap >= blanks.Length) return match.Value;
            var index = Array.IndexOf(fragments, blanks[gap].OccupiedItem);
            var label = index >= 0 ? answerLabels[index] : match.Groups["answer"].Value;
            var colour = index >= 0 ? ColorUtility.ToHtmlStringRGBA(paragraphText.color) : "00000000";
            var answer = $"<space=4><link=\"gap-{gap}\"><color=#{colour}>{label}</color></link><space=4>";
            // A link without its own nobr belongs to a larger phrase kept together in the template.
            return match.Groups["nobr"].Success ? $"<nobr>{answer}</nobr>" : answer;
        }

        private static void HideVisualChildren(Transform parent)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.GetComponent<Renderer>() != null || child.GetComponent<Canvas>() != null)
                    child.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.FlashbackFinishedOrSkipped += Unlock;
            GameEvents.CarryableGrabbed += Grabbed;
            GameEvents.CarryablePlaced += Placed;
            GameEvents.CarryableVacated += Vacated;
        }

        private void OnDisable()
        {
            if (outlineEdges != null)
                foreach (var edges in outlineEdges)
                    foreach (var edge in edges) edge.color = Color.black;
            GameEvents.FlashbackFinishedOrSkipped -= Unlock;
            GameEvents.CarryableGrabbed -= Grabbed;
            GameEvents.CarryablePlaced -= Placed;
            GameEvents.CarryableVacated -= Vacated;
        }

        private void Unlock(ArtefactSlideshow source)
        {
            if (state == null || source == null || source != flashback || IsUnlocked)
                return;
            if (state.NeedsNarrationReplay)
            {
                // Finishing or skipping the replay both grant a fresh attempt.
                if (!state.ResetAfterNarration()) return;
                ResetAnswersForRetry();
            }
            else
                state.Unlock();
            guidance.Show("Fix the papyrus");
            UpdateChances();
            SetScreenPower(false);
        }

        private void ResetAnswersForRetry()
        {
            StopAllCoroutines();
            checkingAnswers = false;
            resettingPuzzle = true;
            for (var i = 0; i < fragments.Length; i++)
            {
                var item = fragments[i];
                item.CurrentSlot?.Vacate(item);
                RestoreAnswerSize(item);
                item.transform.SetPositionAndRotation(transform.TransformPoint(homes[i]), transform.rotation * rotations[i]);
                item.Drop();
                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
                item.Body.isKinematic = true;
                item.SetHighlight(0f);
                fragmentMaterials[i].SetColor("_BaseColor", originalColours[i]);
                pendingReturn[i] = false;
                returning[i] = false;
            }
            resettingPuzzle = false;
            SetInteraction(false);
            if (checkButton != null) checkButton.interactable = false;
            if (checkButtonCollider != null) checkButtonCollider.enabled = false;
            RefreshParagraph();
            UpdateProgress();
        }

        public void BeginMinigame()
        {
            if (!CanBegin || !state.TryStart()) return;
            guidance.Show("Fill every blank, then click Check");
            SetScreenPower(true);
            RefreshParagraph();
            SetInteraction(true);
            UpdateProgress();
            UpdateChances();
            Feedback("Place an answer in every blank, then aim at Check and click Left Mouse (A / Cross on gamepad).", false);
        }

        private void SetInteraction(bool enabled)
        {
            foreach (var item in fragments)
                foreach (var collider in item.GetComponentsInChildren<Collider>()) collider.enabled = enabled;
            foreach (var slot in blanks)
                foreach (var collider in slot.GetComponentsInChildren<Collider>()) collider.enabled = enabled;
        }

        private bool Validate(int gap, CarryableItem item)
        {
            if (checkingAnswers) return false;
            var index = Array.IndexOf(fragments, item);
            if (index < 0) { Feedback("Use the answer fragments from this screen.", true); return false; }
            if (state.CanPlace(gap, item.ItemId)) return true;

            pendingReturn[index] = true;
            return false;
        }

        private void Vacated(CarryableItem item)
        {
            if (resettingPuzzle) return;
            var index = Array.IndexOf(fragments, item);
            if (index < 0) return;
            RestoreAnswerSize(item);
            pendingReturn[index] = true;
            for (var gap = 0; gap < blanks.Length; gap++)
                if (!blanks[gap].Filled) state.RemoveAnswer(gap);
            RefreshParagraph();
            UpdateProgress();
        }

        private void Grabbed(CarryableItem item)
        {
            var index = Array.IndexOf(fragments, item);
            if (index >= 0)
            {
                RestoreAnswerSize(item);
                pendingReturn[index] = true;
            }
        }

        private void Placed(CarryableItem item)
        {
            var index = Array.IndexOf(fragments, item);
            if (index < 0) return;
            var gap = Array.IndexOf(blanks, item.CurrentSlot);
            if (gap < 0)
            {
                item.CurrentSlot?.Vacate(item);
                Feedback("Put this fragment in a blank on the papyrus screen.", true);
                return;
            }
            if (!state.TryPlace(gap, item.ItemId)) return;

            if (paragraphText != null) RefreshParagraph();
            else FitAnswerToBlank(item, blanks[gap]);
            pendingReturn[index] = false;
            UpdateProgress();
            Feedback(state.CanCheck ? "All answers placed. Aim at Check and click Left Mouse (A / Cross on gamepad)." :
                "Answer placed. Fill the remaining blanks before checking.", false);
        }

        private void FitAnswerToBlank(CarryableItem item, ItemSlot blank)
        {
            RestoreAnswerSize(item);
            var surface = item.GetComponentInChildren<MeshRenderer>(true);
            var blankSurface = blank.GetComponentInChildren<MeshRenderer>(true);
            if (surface == null || blankSurface == null) return;

            // Measure in the card's coordinate system so rotated/scaled wall screens also fit.
            // Use the visible blank, rather than its larger raycast hit area.
            var width = item.transform.InverseTransformVector(blankSurface.transform.TransformVector(
                Vector3.right * blankSurface.localBounds.size.x)).magnitude * 0.96f;
            var height = item.transform.InverseTransformVector(blankSurface.transform.TransformVector(
                Vector3.up * blankSurface.localBounds.size.y)).magnitude * 0.96f;
            var cardWidth = item.transform.InverseTransformVector(surface.transform.TransformVector(
                Vector3.right * surface.localBounds.size.x)).magnitude;
            var cardHeight = item.transform.InverseTransformVector(surface.transform.TransformVector(
                Vector3.up * surface.localBounds.size.y)).magnitude;
            if (width <= 0f || height <= 0f || cardWidth <= 0f || cardHeight <= 0f) return;

            var originalScale = surface.transform.localScale;
            var originalSurfaceEnabled = surface.enabled;
            var canvas = item.GetComponentInChildren<Canvas>();
            var originalCanvasEnabled = canvas != null && canvas.enabled;
            var collider = item.GetComponent<BoxCollider>();
            var originalColliderSize = collider != null ? collider.size : Vector3.zero;
            var text = item.GetComponentInChildren<TMP_Text>();
            var originalTextSize = text != null ? text.rectTransform.sizeDelta : Vector2.zero;
            var originalFontSize = text != null ? text.fontSize : 0f;
            var originalMinimum = text != null ? text.fontSizeMin : 0f;
            var originalAutoSizing = text != null && text.enableAutoSizing;
            var originalWrapping = text != null ? text.textWrappingMode : TextWrappingModes.Normal;
            var originalOverflow = text != null ? text.overflowMode : TextOverflowModes.Overflow;
            restoreAnswerSizes[item] = () =>
            {
                if (surface != null)
                {
                    surface.transform.localScale = originalScale;
                    surface.enabled = originalSurfaceEnabled;
                }
                if (canvas != null) canvas.enabled = originalCanvasEnabled;
                if (collider != null) collider.size = originalColliderSize;
                if (text == null) return;
                text.rectTransform.sizeDelta = originalTextSize;
                text.enableAutoSizing = originalAutoSizing;
                text.fontSizeMin = originalMinimum;
                text.fontSize = originalFontSize;
                text.textWrappingMode = originalWrapping;
                text.overflowMode = originalOverflow;
                text.ForceMeshUpdate();
            };

            surface.transform.localScale = new Vector3(originalScale.x * width / cardWidth,
                originalScale.y * height / cardHeight, originalScale.z);
            if (collider != null) collider.size = new Vector3(width, height, originalColliderSize.z);
            if (paragraphText != null)
            {
                // Keep the invisible card collider available for picking the answer up again.
                surface.enabled = false;
                if (canvas != null) canvas.enabled = false;
                return;
            }
            if (text == null) return;

            var textUnitWidth = item.transform.InverseTransformVector(text.transform.TransformVector(Vector3.right)).magnitude;
            var textUnitHeight = item.transform.InverseTransformVector(text.transform.TransformVector(Vector3.up)).magnitude;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * 0.94f / textUnitWidth);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * 0.94f / textUnitHeight);
            text.enableAutoSizing = true;
            text.fontSizeMin = 1f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.ForceMeshUpdate();
        }

        private void RestoreAnswerSize(CarryableItem item)
        {
            if (!restoreAnswerSizes.TryGetValue(item, out var restore)) return;
            restore();
            restoreAnswerSizes.Remove(item);
        }

        private void UpdateProgress()
        {
            if (progressText != null)
                progressText.text = state.PlacedCount + " / " + blanks.Length + " answers placed";
        }

        public void CheckAnswers()
        {
            if (!CanCheck || !state.CheckAnswers()) return;
            audioManager?.PlayUI(state.IsComplete ? correctAnswerSound : incorrectAnswerSound);
            UpdateChances();
            checkingAnswers = true;
            SetInteraction(false);
            if (checkButton != null) checkButton.interactable = false;
            StartCoroutine(ShowCheckResults());
        }

        private IEnumerator ShowCheckResults()
        {
            Feedback("Checking answers... Green = correct, red = incorrect.", false);
            const float duration = 1.5f;
            var time = 0f;
            while (time < duration)
            {
                var strength = Game.Settings.reducedMotion ? 1f : 0.65f + 0.35f * Mathf.Cos(time * Mathf.PI * 4f);
                for (var gap = 0; gap < blanks.Length; gap++)
                {
                    var index = Array.IndexOf(fragments, blanks[gap].OccupiedItem);
                    if (paragraphText != null)
                        TintInlineAnswer(gap, Color.Lerp(paragraphText.color,
                            state.IsSolved(gap) ? correctColour : incorrectColour, strength));
                    else if (index >= 0)
                        fragmentMaterials[index].SetColor("_BaseColor", Color.Lerp(originalColours[index],
                            state.IsSolved(gap) ? correctColour : incorrectColour, strength));
                }
                if (paragraphText != null) paragraphText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            for (var i = 0; i < fragments.Length; i++)
                fragmentMaterials[i].SetColor("_BaseColor", originalColours[i]);
            checkingAnswers = false;
            RefreshParagraph();

            if (state.NeedsNarrationReplay)
            {
                if (checkButtonCollider != null) checkButtonCollider.enabled = false;
                SetInteraction(false);
                SetScreenPower(false);
                yield break;
            }
            if (IsComplete)
            {
                if (paragraphText == null)
                    for (var gap = 0; gap < blanks.Length; gap++)
                    {
                        HideVisualChildren(blanks[gap].OccupiedItem.transform);
                        HideVisualChildren(blanks[gap].transform);
                    }
                progressText.text = "Papyrus restored!";
                Feedback("Minigame complete! You restored the story of papyrus.", false);
            }
            else
            {
                SetInteraction(true);
                progressText.text = state.CorrectCount + " / " + blanks.Length + " correct";
                Feedback("Change the incorrect answers, then click Check again.", true);
            }
        }

        private void TintInlineAnswer(int gap, Color colour)
        {
            foreach (var edge in outlineEdges[gap]) edge.color = colour;
            var info = paragraphText.textInfo;
            for (var i = 0; i < info.linkCount; i++)
            {
                var link = info.linkInfo[i];
                if (link.GetLinkID() != $"gap-{gap}") continue;
                for (var c = 0; c < link.linkTextLength; c++)
                {
                    var character = info.characterInfo[link.linkTextfirstCharacterIndex + c];
                    if (!character.isVisible) continue;
                    var colours = info.meshInfo[character.materialReferenceIndex].colors32;
                    for (var vertex = 0; vertex < 4; vertex++) colours[character.vertexIndex + vertex] = colour;
                }
                break;
            }
        }

        private void Update()
        {
            if (Game.Phase == GamePhase.Exploring && !CanvasQuizUI.IsQuizOpen && !checkingAnswers)
                guidance.Tick(Time.unscaledDeltaTime);

            if (!IsStarted) return;
            UpdateCheckButton();
            for (var i = 0; i < fragments.Length; i++)
                if (pendingReturn[i] && !returning[i] && !fragments[i].IsCarried &&
                    !fragments[i].IsFlying && fragments[i].CurrentSlot == null)
                    StartCoroutine(ReturnHome(i));
        }

        private void LateUpdate()
        {
            if (outlineEdges == null || checkingAnswers) return;
            // Use the same target as Release, after the player's sceptre has updated.
            // Hover colour indicates a drop target; answer correctness waits for Check.
            var draggingAnswer = IsStarted && !IsComplete && Game.Phase == GamePhase.Exploring &&
                !CanvasQuizUI.IsQuizOpen && scepter != null && Array.IndexOf(fragments, scepter.Carried) >= 0;
            var target = draggingAnswer ? scepter.PlacementTarget : null;
            for (var gap = 0; gap < blanks.Length; gap++)
            {
                var colour = blanks[gap] == target ? new Color(0.1f, 0.8f, 0.2f) : Color.black;
                foreach (var edge in outlineEdges[gap]) edge.color = colour;
            }
        }

        private void UpdateCheckButton()
        {
            if (checkButton == null) return;
            checkButton.interactable = CanCheck;
            checkButtonCollider.enabled = CanCheck;
            var clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            clicked |= Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            var camera = Camera.main;
            if (!CanCheck || !clicked || camera == null) return;
            var point = Cursor.lockState == CursorLockMode.Locked || Mouse.current == null
                ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) : Mouse.current.position.ReadValue();
            if (Physics.Raycast(camera.ScreenPointToRay(point), out var hit, 10f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider == checkButtonCollider)
                checkButton.onClick.Invoke();
        }

        private IEnumerator ReturnHome(int index)
        {
            returning[index] = true;
            pendingReturn[index] = false;
            var item = fragments[index];
            RestoreAnswerSize(item);
            item.Grab();
            var position = item.transform.position;
            var rotation = item.transform.rotation;
            var home = transform.TransformPoint(homes[index]);
            var homeRotation = transform.rotation * rotations[index];
            var duration = Game.Settings.reducedMotion ? 0 : 0.4f;
            var time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
                item.transform.SetPositionAndRotation(Vector3.Lerp(position, home, t), Quaternion.Slerp(rotation, homeRotation, t));
                yield return null;
            }
            item.transform.SetPositionAndRotation(home, homeRotation);
            fragmentMaterials[index].SetColor("_BaseColor", originalColours[index]);
            item.Drop();
            item.Body.isKinematic = true;
            returning[index] = false;
        }

        private void Feedback(string text, bool error)
        {
            feedbackText.text = text;
            feedbackText.color = error ? new Color(0.50f, 0.08f, 0.04f) : new Color(0.09f, 0.065f, 0.04f);
        }

        private void OnGUI()
        {
            if (!IsUnlocked || Game.Phase != GamePhase.Exploring || CanvasQuizUI.IsQuizOpen ||
                (IsComplete && !checkingAnswers)) return;
            guidance.Draw(checkingAnswers ? "Checking answers..." : null);
        }

        private void OnDestroy()
        {
            if (fragmentMaterials == null) return;
            foreach (var material in fragmentMaterials) if (material != null) Destroy(material);
        }
    }
}
