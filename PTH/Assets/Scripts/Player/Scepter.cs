using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Grab-and-carry tool driven by FirstPersonPlayer. Any carryable within
    /// grab range can be grabbed while the tool input is held (the one under
    /// the crosshair takes priority); pointing the sceptre at a grabbable draws
    /// an animated aim ring around it (ScepterAimRing shader on a billboard
    /// quad). Releasing drops the item, or snaps it into its slot when released
    /// within the snap radius.
    /// </summary>
    public class Scepter : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float grabRange = 3f;
        [SerializeField] private float holdDistance = 1.6f;
        [SerializeField] private float positionLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 6f;
        [SerializeField] private float snapRadius = 1.1f;
        [SerializeField] private Color aimRingColor = new Color(0.25f, 0.85f, 1f);
        [SerializeField] private Shader aimRingShader;

        private const string AimRingShaderName = "Custom/ScepterAimRing";
        // The shader draws the ring at 0.75 of the quad half-extent, so a world
        // ring radius R needs a quad scale of R / (0.75 * 0.5).
        private const float RingScalePerWorldRadius = 1f / 0.375f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int MotionAmountId = Shader.PropertyToID("_MotionAmount");

        private CarryableItem carried;
        private CarryableItem aimedItem;
        private Transform aimRing;
        private Material aimRingMaterial;
        private bool aimRingShaderMissing;
        private CharacterController playerBody;

        public bool IsCarrying => carried != null;
        public CarryableItem Carried => carried;

        private void Awake()
        {
            playerBody = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            GameEvents.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.PhaseChanged -= HandlePhaseChanged;
            HideAimRing();
            if (carried != null)
            {
                // Tear-down mid-carry: drop silently, gameplay feedback would
                // reach listeners that are being destroyed with the scene.
                carried.Drop();
                carried.TargetSlot?.EndHighlight();
                carried = null;
            }
        }

        private void OnDestroy()
        {
            if (aimRingMaterial != null)
            {
                Destroy(aimRingMaterial);
            }

            if (aimRing != null)
            {
                Destroy(aimRing.gameObject);
            }
        }

        public void Tick(bool toolHeld, bool canControl, float deltaTime)
        {
            if (carried != null)
            {
                HideAimRing();
                if (!canControl || !toolHeld)
                {
                    Release();
                }
                else
                {
                    MoveCarried(deltaTime);
                }

                return;
            }

            if (!canControl)
            {
                HideAimRing();
                return;
            }

            UpdateAim();
            if (toolHeld)
            {
                TryGrab();
            }
        }

        private void UpdateAim()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                HideAimRing();
                return;
            }

            var camTransform = cam.transform;
            if (Physics.Raycast(camTransform.position, camTransform.forward, out var hit, grabRange))
            {
                var item = hit.collider.GetComponentInParent<CarryableItem>();
                if (item != null && !item.IsPlaced && !item.IsCarried)
                {
                    ShowAimRing(item, camTransform);
                    return;
                }
            }

            HideAimRing();
        }

        private void TryGrab()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            // Anything in range works; the target under the crosshair wins.
            var item = aimedItem != null ? aimedItem : FindNearestInRange(cam.transform.position);
            if (item == null)
            {
                return;
            }

            carried = item;
            aimedItem = null;
            item.Grab();
            if (playerBody != null && item.ItemCollider != null)
            {
                Physics.IgnoreCollision(playerBody, item.ItemCollider, true);
            }

            item.TargetSlot?.BeginHighlight();
            GameEvents.RaiseCarryableGrabbed(item);
        }

        private CarryableItem FindNearestInRange(Vector3 center)
        {
            var hits = Physics.OverlapSphere(center, grabRange);
            CarryableItem nearest = null;
            var nearestSqrDistance = float.MaxValue;
            foreach (var hit in hits)
            {
                var item = hit.GetComponentInParent<CarryableItem>();
                if (item == null || item.IsPlaced || item.IsCarried)
                {
                    continue;
                }

                var sqrDistance = (item.transform.position - center).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = item;
                }
            }

            return nearest;
        }

        private void MoveCarried(float deltaTime)
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            var camTransform = cam.transform;
            var itemTransform = carried.transform;
            var holdPoint = camTransform.position + camTransform.forward * holdDistance;
            itemTransform.position = Vector3.Lerp(itemTransform.position, holdPoint, deltaTime * positionLerpSpeed);
            itemTransform.rotation = Quaternion.Slerp(
                itemTransform.rotation,
                Quaternion.LookRotation(camTransform.forward, camTransform.up),
                deltaTime * rotationLerpSpeed);

            var slot = carried.TargetSlot;
            if (slot != null && !slot.Filled)
            {
                slot.SetNear(slot.IsInRange(itemTransform.position, snapRadius));
            }
        }

        private void Release()
        {
            var item = carried;
            carried = null;
            if (item == null)
            {
                return;
            }

            var slot = item.TargetSlot;
            if (slot != null && slot.IsInRange(item.transform.position, snapRadius))
            {
                slot.Place(item);
                GameEvents.RaiseCarryablePlaced(item);
            }
            else
            {
                item.Drop();
                slot?.EndHighlight();
            }

            if (playerBody != null && item.ItemCollider != null)
            {
                Physics.IgnoreCollision(playerBody, item.ItemCollider, false);
            }
        }

        private void ShowAimRing(CarryableItem item, Transform camTransform)
        {
            EnsureAimRing();
            if (aimRing == null)
            {
                return;
            }

            aimedItem = item;
            var bounds = item.ItemCollider != null
                ? item.ItemCollider.bounds
                : new Bounds(item.transform.position, Vector3.one);
            aimRing.position = bounds.center;
            aimRing.rotation = camTransform.rotation;
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.35f + 0.1f;
            aimRing.localScale = Vector3.one * (radius * RingScalePerWorldRadius);
            aimRingMaterial.SetFloat(MotionAmountId, Game.Settings.reducedMotion ? 0f : 1f);
            aimRing.gameObject.SetActive(true);
        }

        private void HideAimRing()
        {
            aimedItem = null;
            if (aimRing != null && aimRing.gameObject.activeSelf)
            {
                aimRing.gameObject.SetActive(false);
            }
        }

        private void EnsureAimRing()
        {
            if (aimRing != null)
            {
                return;
            }

            var shader = aimRingShader != null ? aimRingShader : Shader.Find(AimRingShaderName);
            if (shader == null)
            {
                if (!aimRingShaderMissing)
                {
                    aimRingShaderMissing = true;
                    Debug.LogWarning($"Scepter aim ring shader '{AimRingShaderName}' not found; assign it on the Scepter component.", this);
                }

                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Scepter Aim Ring";
            Destroy(quad.GetComponent<Collider>());
            aimRingMaterial = new Material(shader);
            aimRingMaterial.SetColor(BaseColorId, aimRingColor * 2f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = aimRingMaterial;
            quad.SetActive(false);
            aimRing = quad.transform;
            aimRing.SetParent(transform, false);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Exploring)
            {
                Release();
            }
        }
    }
}
