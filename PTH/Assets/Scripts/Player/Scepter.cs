using System.Collections.Generic;
using UnityEngine;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Grab-and-carry tool driven by FirstPersonPlayer. Any carryable within
    /// grab range can be grabbed while the tool input is held (the one under
    /// the crosshair takes priority, seated ones included); pointing the
    /// sceptre at a grabbable lights a fresnel rim and silhouette outline on
    /// it, fading in and out and dimming towards the edge of grab range.
    /// While carrying, every slot glows; aiming at a slot within place
    /// range makes it brighter, and releasing then makes the item fly into
    /// that slot (the item owns its flight). Releasing near a slot without
    /// aiming snaps into the closest one; otherwise the item drops.
    /// </summary>
    public class Scepter : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float grabRange = 3f;
        [SerializeField] private float placeRange = 10f;
        [SerializeField] private float holdDistance = 2.8f;
        [SerializeField] private float holdSideOffset = 0.35f;
        [SerializeField] private float positionLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 6f;
        [SerializeField] private float snapRadius = 1.1f;
        [SerializeField] private float highlightFullDistance = 4f;
        [SerializeField] private float maxCarrySpeed = 20f;
        [SerializeField] private float connectionBreakDelay = 0.1f;

        private static readonly RaycastHit[] AimHits = new RaycastHit[16];
        private static readonly Collider[] NearbyColliders = new Collider[16];

        private CarryableItem carried;
        private CarryableItem aimedItem;
        private ItemSlot aimedSlot;
        private ItemSlot nearSlot;
        private readonly List<ItemSlot> carryingHighlights = new List<ItemSlot>();
        private Collider[] playerColliders;
        private AudioManager audioManager;
        private bool waitForToolRelease;
        private float blockedTime;

        public bool IsCarrying => carried != null;
        public CarryableItem Carried => carried;

        private void Awake()
        {
            playerColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Start()
        {
            audioManager = FindAnyObjectByType<AudioManager>();
        }

        private void OnEnable()
        {
            GameEvents.PhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.PhaseChanged -= HandlePhaseChanged;
            audioManager?.StopScepterCarrySound();
            ClearAim();
            if (carried != null)
            {
                // Tear-down mid-carry: drop silently, gameplay feedback would
                // reach listeners that are being destroyed with the scene.
                carried.Drop();
                carried = null;
            }

            ClearCarryFeedback();
        }

        public void Tick(bool toolHeld, bool canControl, float deltaTime)
        {
            // An item can also disappear while held (for example on scene unload).
            if (carried == null)
            {
                audioManager?.StopScepterCarrySound();
            }

            if (carried != null)
            {
                ClearAim();
                if (!canControl || !toolHeld)
                {
                    Release();
                }
                else
                {
                    // A brief obstruction is tolerated; a lasting one breaks
                    // the link and the item falls.
                    blockedTime = IsConnectionBlocked(carried) ? blockedTime + deltaTime : 0f;
                    if (blockedTime > connectionBreakDelay)
                    {
                        Release(true);
                    }
                    else
                    {
                        MoveCarried(deltaTime);
                        UpdateCarriedAim();
                    }
                }

                return;
            }

            // After any release the tool must be let go before it grabs again,
            // so a dropped item is not re-grabbed every frame.
            if (!toolHeld)
            {
                waitForToolRelease = false;
            }

            if (!canControl)
            {
                ClearAim();
                return;
            }

            UpdateAim();
            if (toolHeld && !waitForToolRelease)
            {
                TryGrab();
            }
        }

        private void UpdateAim()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                ClearAim();
                return;
            }

            var camTransform = cam.transform;
            if (Physics.Raycast(camTransform.position, camTransform.forward, out var hit, grabRange))
            {
                var item = hit.collider.GetComponentInParent<CarryableItem>();
                if (item != null && !item.IsCarried && !item.IsFlying)
                {
                    AimAt(item, HighlightAtDistance(hit.distance));
                    return;
                }
            }

            ClearAim();
        }

        private void AimAt(CarryableItem item, float strength)
        {
            if (aimedItem != null && aimedItem != item)
            {
                aimedItem.SetHighlight(0f);
            }

            aimedItem = item;
            item.SetHighlight(strength);
        }

        private void ClearAim()
        {
            if (aimedItem != null)
            {
                aimedItem.SetHighlight(0f);
            }

            aimedItem = null;
        }

        /// <summary>
        /// Full highlight up to highlightFullDistance, easing down to nothing
        /// at the edge of grab range so far targets do not glow at full power.
        /// </summary>
        private float HighlightAtDistance(float distance)
        {
            if (grabRange <= highlightFullDistance)
            {
                return 1f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(highlightFullDistance, grabRange, distance));
        }

        private void TryGrab()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            // Anything in range works, seated items included; the target under
            // the crosshair wins.
            var item = aimedItem != null ? aimedItem : FindNearestInRange(cam.transform.position);
            if (item == null || IsConnectionBlocked(item))
            {
                return;
            }

            carried = item;
            blockedTime = 0f;
            ClearAim();
            item.Grab();
            item.CurrentSlot?.Vacate(item);
            IgnorePlayerCollision(item, true);

            // While the hand is full every slot glows as a placement hint.
            carryingHighlights.Clear();
            foreach (var slot in FindObjectsByType<ItemSlot>())
            {
                slot.BeginHighlight();
                carryingHighlights.Add(slot);
            }

            GameEvents.RaiseCarryableGrabbed(item);
            audioManager?.PlayScepterPickupSound();
            audioManager?.StartScepterCarrySound();
        }

        private CarryableItem FindNearestInRange(Vector3 center)
        {
            var hits = Physics.OverlapSphere(center, grabRange);
            CarryableItem nearest = null;
            var nearestSqrDistance = float.MaxValue;
            foreach (var hit in hits)
            {
                var item = hit.GetComponentInParent<CarryableItem>();
                if (item == null || item.IsCarried || item.IsFlying)
                {
                    continue;
                }

                var sqrDistance = (item.transform.position - center).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance || IsConnectionBlocked(item))
                {
                    // Items behind walls cannot be grabbed.
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                nearest = item;
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

            // Driven through the rigidbody so walls and other solids stop it.
            var camTransform = cam.transform;
            var body = carried.Body;
            var holdPoint = camTransform.position
                + camTransform.forward * holdDistance
                + camTransform.right * holdSideOffset;
            body.linearVelocity = Vector3.ClampMagnitude((holdPoint - body.position) * positionLerpSpeed, maxCarrySpeed);
            body.angularVelocity = Vector3.zero;
            body.MoveRotation(Quaternion.Slerp(
                body.rotation,
                Quaternion.LookRotation(camTransform.forward, camTransform.up),
                deltaTime * rotationLerpSpeed));
        }

        /// <summary>
        /// True when something solid sits between the camera and the item.
        /// </summary>
        private bool IsConnectionBlocked(CarryableItem item)
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return false;
            }

            var origin = cam.transform.position;
            var target = item.ItemRenderer != null ? item.ItemRenderer.bounds.center : item.transform.position;
            var toTarget = target - origin;
            var distance = toTarget.magnitude;
            if (distance < 0.0001f)
            {
                return false;
            }

            var count = Physics.RaycastNonAlloc(
                origin, toTarget / distance, AimHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var index = 0; index < count; index++)
            {
                if (CarryPhysics.IsBlocking(AimHits[index], item, null))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// While carrying: tracks the slot under the crosshair (any slot, the
        /// carried item itself is skipped so it cannot block the ray) and the
        /// closest slot in snap range. Both glow brighter as "ready to place".
        /// </summary>
        private void UpdateCarriedAim()
        {
            var newAimedSlot = GetAimedSlot();
            var newNearSlot = FindNearestSlot(carried.transform.position, snapRadius);

            if (aimedSlot != null && aimedSlot != newAimedSlot && aimedSlot != newNearSlot)
            {
                aimedSlot.SetNear(false);
            }

            if (nearSlot != null && nearSlot != newAimedSlot && nearSlot != newNearSlot)
            {
                nearSlot.SetNear(false);
            }

            newAimedSlot?.SetNear(true);
            newNearSlot?.SetNear(true);
            aimedSlot = newAimedSlot;
            nearSlot = newNearSlot;
        }

        private ItemSlot GetAimedSlot()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return null;
            }

            var camTransform = cam.transform;
            var count = Physics.RaycastNonAlloc(
                camTransform.position, camTransform.forward, AimHits, placeRange);
            var bestDistance = float.MaxValue;
            Collider bestCollider = null;
            for (var index = 0; index < count; index++)
            {
                var hit = AimHits[index];
                if (carried != null && hit.collider.transform.IsChildOf(carried.transform))
                {
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestCollider = hit.collider;
                }
            }

            // Occlusion: only the closest hit counts — a wall in front of a
            // slot blocks the throw.
            return bestCollider != null ? bestCollider.GetComponentInParent<ItemSlot>() : null;
        }

        private ItemSlot FindNearestSlot(Vector3 position, float radius)
        {
            var count = Physics.OverlapSphereNonAlloc(position, radius, NearbyColliders);
            ItemSlot nearest = null;
            var nearestSqrDistance = float.MaxValue;
            for (var index = 0; index < count; index++)
            {
                var slot = NearbyColliders[index].GetComponentInParent<ItemSlot>();
                if (slot == null || !slot.IsInRange(position, radius))
                {
                    continue;
                }

                var sqrDistance = (slot.SnapPosition - position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = slot;
                }
            }

            return nearest;
        }

        private void Release(bool dropOnly = false)
        {
            audioManager?.StopScepterCarrySound();
            var item = carried;
            carried = null;
            if (item == null)
            {
                return;
            }

            waitForToolRelease = true;

            // Capture the aimed slot before the feedback cleanup clears it.
            var remoteSlot = dropOnly ? null : aimedSlot;
            var snapSlot = dropOnly ? null : nearSlot;
            ClearCarryFeedback();

            if (remoteSlot != null)
            {
                item.FlyTo(remoteSlot);
            }
            else if (snapSlot != null)
            {
                if (!snapSlot.Place(item))
                {
                    DropItem(item);
                }
            }
            else
            {
                DropItem(item);
            }

            IgnorePlayerCollision(item, false);
        }

        private void IgnorePlayerCollision(CarryableItem item, bool ignore)
        {
            if (item.ItemCollider == null)
            {
                return;
            }

            foreach (var playerCollider in playerColliders)
            {
                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(playerCollider, item.ItemCollider, ignore);
                }
            }
        }

        private void DropItem(CarryableItem item)
        {
            item.Drop();
            // Phase changes also release items; keep pause and scene transitions silent.
            if (Game.Phase == GamePhase.Exploring && !SceneTransition.IsTransitioning)
            {
                audioManager?.PlayScepterDropSound();
            }
        }

        private void ClearCarryFeedback()
        {
            aimedSlot?.SetNear(false);
            nearSlot?.SetNear(false);
            aimedSlot = null;
            nearSlot = null;
            foreach (var slot in carryingHighlights)
            {
                slot.EndHighlight();
            }

            carryingHighlights.Clear();
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
