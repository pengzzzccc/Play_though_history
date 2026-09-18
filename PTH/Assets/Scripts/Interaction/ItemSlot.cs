using System.Collections;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// The target position of a carryable item. While its item is carried the
    /// slot pulses an emissive highlight (brighter once the item is close to
    /// the snap point); releasing the item inside the sceptre's snap radius
    /// places it here. Placing is idempotent, validates the item id against
    /// requiredItemId (empty accepts anything) and ejects the previous
    /// occupant when a different item takes the slot.
    /// </summary>
    public class ItemSlot : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        [SerializeField] private Transform snapPoint;
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Color highlightColor = new Color(0.25f, 0.85f, 1f);
        [SerializeField] private float baseIntensity = 0.6f;
        [SerializeField] private float nearIntensity = 1.8f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmplitude = 0.3f;
        [SerializeField] private Vector3 placeOffset = new Vector3(0f, 0.15f, 0f);
        [SerializeField] private string requiredItemId;
        [SerializeField] private float ejectPopSpeed = 1.5f;
        [SerializeField] private float flashDuration = 0.6f;

        private Material highlightMaterial;
        private MaterialPropertyBlock flashBlock;
        private Coroutine flashRoutine;
        private float pulseTime;
        private bool highlighting;
        private bool near;

        public bool Filled { get; private set; }
        public CarryableItem OccupiedItem { get; private set; }

        public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;

        public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

        public Vector3 PlaceOffset => placeOffset;

        private void Awake()
        {
            if (snapPoint == null)
            {
                snapPoint = transform;
            }

            if (highlightRenderer != null)
            {
                // Instance the material so the highlight only affects this slot,
                // never every renderer sharing the slot pad material.
                highlightMaterial = highlightRenderer.material;
                highlightMaterial.EnableKeyword("_EMISSION");
                highlightMaterial.SetColor(EmissionColorId, Color.black);
            }
        }

        public bool IsInRange(Vector3 position, float radius)
        {
            return (position - SnapPosition).sqrMagnitude <= radius * radius;
        }

        public void SetNear(bool isNear)
        {
            near = isNear;
        }

        public void BeginHighlight()
        {
            if (highlightMaterial == null)
            {
                return;
            }

            highlighting = true;
            near = false;
            pulseTime = 0f;
        }

        public void EndHighlight()
        {
            highlighting = false;
            near = false;
            if (highlightMaterial != null)
            {
                highlightMaterial.SetColor(EmissionColorId, Color.black);
            }
        }

        /// <summary>
        /// Places an item into the slot — any item fits anywhere. Returns true
        /// when the item ends up in the slot. Idempotent for the item already
        /// sitting here; ejects the previous occupant when a different item
        /// takes the slot; flashes gold when requiredItemId is set and matches.
        /// </summary>
        public bool Place(CarryableItem item)
        {
            if (item == null)
            {
                return false;
            }

            // Idempotent: the same item is already in this slot.
            if (Filled && OccupiedItem == item)
            {
                return true;
            }

            // A different occupant gets kicked out before the new one seats.
            if (Filled && OccupiedItem != null)
            {
                Eject(OccupiedItem);
            }

            Filled = true;
            OccupiedItem = item;
            item.transform.SetPositionAndRotation(SnapPosition + placeOffset, SnapRotation);
            item.Place(this);
            EndHighlight();
            if (!string.IsNullOrEmpty(requiredItemId) && item.ItemId == requiredItemId)
            {
                StartFlash(item);
            }

            return true;
        }

        /// <summary>
        /// Frees the slot when its current occupant is grabbed again. Returns
        /// false when some other item occupies the slot.
        /// </summary>
        public bool Vacate(CarryableItem item)
        {
            if (!Filled || OccupiedItem != item)
            {
                return false;
            }

            Filled = false;
            OccupiedItem = null;
            item.ClearSlot();
            EndHighlight();
            return true;
        }

        private void Eject(CarryableItem item)
        {
            Filled = false;
            OccupiedItem = null;
            item.Eject();
            if (item.Body != null)
            {
                item.Body.AddForce(Vector3.up * ejectPopSpeed, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// Plays the gold validation flash on the placed item: _FlashAmount
        /// decays 1 → 0 through a property block, so the scepter's rim values
        /// on the same block are preserved.
        /// </summary>
        private void StartFlash(CarryableItem item)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(item));
        }

        private IEnumerator FlashRoutine(CarryableItem item)
        {
            var renderer = item.ItemRenderer;
            if (renderer == null)
            {
                flashRoutine = null;
                yield break;
            }

            flashBlock ??= new MaterialPropertyBlock();
            var time = 0f;
            while (time < flashDuration && renderer != null)
            {
                time += Time.deltaTime;
                var amount = Mathf.Clamp01(1f - time / flashDuration);
                renderer.GetPropertyBlock(flashBlock);
                flashBlock.SetFloat(FlashAmountId, amount);
                renderer.SetPropertyBlock(flashBlock);
                yield return null;
            }

            if (renderer != null)
            {
                renderer.GetPropertyBlock(flashBlock);
                flashBlock.SetFloat(FlashAmountId, 0f);
                renderer.SetPropertyBlock(flashBlock);
            }

            flashRoutine = null;
        }

        private void Update()
        {
            if (!highlighting || highlightMaterial == null)
            {
                return;
            }

            pulseTime += Time.deltaTime;
            var intensity = near ? nearIntensity : baseIntensity;
            if (!Game.Settings.reducedMotion)
            {
                intensity *= 1f + pulseAmplitude * Mathf.Sin(pulseTime * pulseSpeed);
            }

            highlightMaterial.SetColor(EmissionColorId, highlightColor * intensity);
        }

        private void OnDestroy()
        {
            if (highlightMaterial != null)
            {
                Destroy(highlightMaterial);
            }
        }
    }
}
