using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private string[] wrongAnswerHints;
        [SerializeField] private Color correctColour = new Color(0.59f, 0.78f, 0.48f);
        [SerializeField] private Color incorrectColour = new Color(0.93f, 0.48f, 0.40f);

        private PapyrusPuzzleState state;
        private Vector3[] homes;
        private Quaternion[] rotations;
        private bool[] pendingReturn;
        private bool[] returning;
        private Material[] fragmentMaterials;
        private Color[] originalColours;
        private GUIStyle objectiveStyle;
        private float completedAt;
        private string paragraphMarkup;
        private readonly List<Renderer> screenRenderers = new List<Renderer>();
        private readonly List<Canvas> screenCanvases = new List<Canvas>();

        public bool IsUnlocked => state != null && state.IsUnlocked;
        public bool IsStarted => state != null && state.IsStarted;
        public bool IsComplete => state != null && state.IsComplete;
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
            }
            state = new PapyrusPuzzleState(required);
            paragraphMarkup = paragraphText != null ? paragraphText.text : null;
            homes = new Vector3[fragments.Length];
            rotations = new Quaternion[fragments.Length];
            pendingReturn = new bool[fragments.Length];
            returning = new bool[fragments.Length];
            fragmentMaterials = new Material[fragments.Length];
            originalColours = new Color[fragments.Length];
            for (var i = 0; i < fragments.Length; i++)
            {
                homes[i] = transform.InverseTransformPoint(fragments[i].transform.position);
                rotations[i] = Quaternion.Inverse(transform.rotation) * fragments[i].transform.rotation;
                fragmentMaterials[i] = fragments[i].GetComponentInChildren<Renderer>().material;
                originalColours[i] = fragmentMaterials[i].GetColor("_BaseColor");
            }

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

        private void SetScreenPower(bool powered)
        {
            foreach (var renderer in screenRenderers) renderer.enabled = powered;
            foreach (var canvas in screenCanvases) canvas.enabled = powered;
            if (lockedOverlay != null) lockedOverlay.SetActive(!powered);
            if (startPrompt != null) startPrompt.SetActive(IsUnlocked && !powered);
            if (powerIndicator != null)
                powerIndicator.color = powered ? new Color(0.25f, 0.9f, 0.5f) :
                    IsUnlocked ? new Color(1f, 0.65f, 0.15f) : new Color(0.65f, 0.08f, 0.04f);
        }

        private void Start()
        {
            RefreshParagraph();
        }

        private void RefreshParagraph()
        {
            if (paragraphText == null || paragraphMarkup == null)
                return;

            var text = paragraphMarkup;
            for (var i = 0; i < blanks.Length; i++)
                if (state.IsSolved(i))
                    text = text.Replace($"<link=\"gap-{i}\"><nobr><color=#00000000>",
                        $"<link=\"gap-{i}\"><nobr><color=#24572A>");

            paragraphText.text = text;
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
            }
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
        }

        private void OnDisable()
        {
            GameEvents.FlashbackFinishedOrSkipped -= Unlock;
            GameEvents.CarryableGrabbed -= Grabbed;
            GameEvents.CarryablePlaced -= Placed;
        }

        private void Unlock(ArtefactSlideshow source)
        {
            if (state == null || source == null || source != flashback || IsUnlocked)
                return;
            state.Unlock();
            SetScreenPower(false);
        }

        public void BeginMinigame()
        {
            if (!CanBegin || !state.TryStart()) return;
            SetScreenPower(true);
            RefreshParagraph();
            SetInteraction(true);
            Feedback("Hold F / Right Mouse (or Right Trigger) to grab a fragment. Aim at a blank and release to place it.", false);
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
            var index = Array.IndexOf(fragments, item);
            if (index < 0) { Feedback("Use the answer fragments from this screen.", true); return false; }
            if (state.CanPlace(gap, item.ItemId)) return true;

            pendingReturn[index] = true;
            fragmentMaterials[index].SetColor("_BaseColor", incorrectColour);
            var hint = state.IsSolved(gap) ? "That blank is already complete. Try another blank." :
                wrongAnswerHints != null && gap < wrongAnswerHints.Length ? wrongAnswerHints[gap] : "Try another fragment.";
            Feedback(hint, true);
            return false;
        }

        private void Grabbed(CarryableItem item)
        {
            var index = Array.IndexOf(fragments, item);
            if (index >= 0) pendingReturn[index] = true;
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

            pendingReturn[index] = false;
            item.ItemCollider.enabled = false;
            fragmentMaterials[index].SetColor("_BaseColor", correctColour);
            HideVisualChildren(item.transform);
            HideVisualChildren(blanks[gap].transform);
            RefreshParagraph();
            progressText.text = state.CorrectCount + " / " + blanks.Length + " fragments restored";
            Feedback("Correct! That fragment belongs here.", false);
            if (IsComplete)
            {
                completedAt = Time.unscaledTime;
                progressText.text = "Papyrus restored!";
                Feedback("Minigame complete! You restored the story of papyrus.", false);
            }
        }

        private void Update()
        {
            if (!IsStarted) return;
            for (var i = 0; i < fragments.Length; i++)
                if (pendingReturn[i] && !returning[i] && !fragments[i].IsCarried &&
                    !fragments[i].IsFlying && fragments[i].CurrentSlot == null)
                    StartCoroutine(ReturnHome(i));
        }

        private IEnumerator ReturnHome(int index)
        {
            returning[index] = true;
            pendingReturn[index] = false;
            var item = fragments[index];
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
                (IsComplete && Time.unscaledTime - completedAt > 5f)) return;
            if (objectiveStyle == null)
                objectiveStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
            var width = Mathf.Min(600, Screen.width - 32);
            GUI.Box(new Rect((Screen.width - width) / 2, Screen.height * 0.12f, width, 48),
                IsComplete ? "Minigame complete!" : IsStarted ? "Complete the minigame" : "Fix the papyrus", objectiveStyle);
        }

        private void OnDestroy()
        {
            if (fragmentMaterials == null) return;
            foreach (var material in fragmentMaterials) if (material != null) Destroy(material);
        }
    }
}
