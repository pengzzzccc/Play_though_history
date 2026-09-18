using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// The target position of a carryable item. While its item is carried the
    /// slot pulses an emissive highlight (brighter once the item is close to
    /// the snap point); releasing the item inside the sceptre's snap radius
    /// places it here for good.
    /// </summary>
    public class ItemSlot : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform snapPoint;
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Color highlightColor = new Color(0.25f, 0.85f, 1f);
        [SerializeField] private float baseIntensity = 0.6f;
        [SerializeField] private float nearIntensity = 1.8f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseAmplitude = 0.3f;
        [SerializeField] private Vector3 placeOffset = new Vector3(0f, 0.15f, 0f);

        private Material highlightMaterial;
        private float pulseTime;
        private bool highlighting;
        private bool near;

        public bool Filled { get; private set; }

        public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;

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
            return !Filled && (position - SnapPosition).sqrMagnitude <= radius * radius;
        }

        public void SetNear(bool isNear)
        {
            near = isNear;
        }

        public void BeginHighlight()
        {
            if (Filled || highlightMaterial == null)
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

        public void Place(CarryableItem item)
        {
            if (Filled || item == null)
            {
                return;
            }

            Filled = true;
            var rotation = snapPoint != null ? snapPoint.rotation : transform.rotation;
            item.transform.SetPositionAndRotation(SnapPosition + placeOffset, rotation);
            item.Place();
            EndHighlight();
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
