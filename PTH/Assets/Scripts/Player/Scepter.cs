using System.Collections.Generic;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Grab-and-carry tool driven by FirstPersonPlayer. Any carryable within
    /// grab range can be grabbed while the tool input is held (the one under
    /// the crosshair takes priority, seated ones included); pointing the
    /// sceptre at a grabbable lights a fresnel rim and silhouette outline on
    /// it. While carrying, every slot glows; aiming at a slot within place
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

        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly RaycastHit[] AimHits = new RaycastHit[16];
        private static readonly Collider[] NearbyColliders = new Collider[16];

        private CarryableItem carried;
        private CarryableItem aimedItem;
        private ItemSlot aimedSlot;
        private ItemSlot nearSlot;
        private ItemSlot prevAimedSlot;
        private ItemSlot prevNearSlot;
        private readonly List<ItemSlot> carryingHighlights = new List<ItemSlot>();
        private MaterialPropertyBlock rimBlock;
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
            if (carried != null)
            {
                ClearAim();
                if (!canControl || !toolHeld)
                {
                    Release();
                }
                else
                {
                    MoveCarried(deltaTime);
                    UpdateCarriedAim();
                }

                return;
            }

            if (!canControl)
            {
                ClearAim();
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
                ClearAim();
                return;
            }

            var camTransform = cam.transform;
            if (Physics.Raycast(camTransform.position, camTransform.forward, out var hit, grabRange))
            {
                var item = hit.collider.GetComponentInParent<CarryableItem>();
                if (item != null && !item.IsCarried && !item.IsFlying)
                {
                    AimAt(item);
                    return;
                }
            }

            ClearAim();
        }

        private void AimAt(CarryableItem item)
        {
            if (aimedItem == item)
            {
                return;
            }

            SetRim(aimedItem, false);
            SetRim(item, true);
            aimedItem = item;
        }

        private void ClearAim()
        {
            SetRim(aimedItem, false);
            aimedItem = null;
        }

        private void SetRim(CarryableItem item, bool state)
        {
            if (item == null || item.ItemRenderer == null)
            {
                return;
            }

            rimBlock ??= new MaterialPropertyBlock();
            item.ItemRenderer.GetPropertyBlock(rimBlock);
            rimBlock.SetFloat(RimStrengthId, state ? 1f : 0f);
            item.ItemRenderer.SetPropertyBlock(rimBlock);
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
            if (item == null)
            {
                return;
            }

            carried = item;
            ClearAim();
            item.Grab();
            item.CurrentSlot?.Vacate(item);
            if (playerBody != null && item.ItemCollider != null)
            {
                Physics.IgnoreCollision(playerBody, item.ItemCollider, true);
            }

            // While the hand is full every slot glows as a placement hint.
            carryingHighlights.Clear();
            foreach (var slot in FindObjectsByType<ItemSlot>(FindObjectsSortMode.None))
            {
                slot.BeginHighlight();
                carryingHighlights.Add(slot);
            }

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
                if (item == null || item.IsCarried || item.IsFlying)
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
            var holdPoint = camTransform.position
                + camTransform.forward * holdDistance
                + camTransform.right * holdSideOffset;
            itemTransform.position = Vector3.Lerp(itemTransform.position, holdPoint, deltaTime * positionLerpSpeed);
            itemTransform.rotation = Quaternion.Slerp(
                itemTransform.rotation,
                Quaternion.LookRotation(camTransform.forward, camTransform.up),
                deltaTime * rotationLerpSpeed);
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

            if (prevAimedSlot != null && prevAimedSlot != newAimedSlot && prevAimedSlot != newNearSlot)
            {
                prevAimedSlot.SetNear(false);
            }

            if (prevNearSlot != null && prevNearSlot != newAimedSlot && prevNearSlot != newNearSlot)
            {
                prevNearSlot.SetNear(false);
            }

            newAimedSlot?.SetNear(true);
            newNearSlot?.SetNear(true);
            aimedSlot = newAimedSlot;
            nearSlot = newNearSlot;
            prevAimedSlot = newAimedSlot;
            prevNearSlot = newNearSlot;
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

        private void Release()
        {
            var item = carried;
            carried = null;
            if (item == null)
            {
                return;
            }

            // Capture the aimed slot before the feedback cleanup clears it.
            var remoteSlot = aimedSlot;
            var snapSlot = nearSlot;
            ClearCarryFeedback();

            if (remoteSlot != null)
            {
                item.FlyTo(remoteSlot);
            }
            else if (snapSlot != null)
            {
                if (snapSlot.Place(item))
                {
                    GameEvents.RaiseCarryablePlaced(item);
                }
                else
                {
                    item.Drop();
                }
            }
            else
            {
                item.Drop();
            }

            if (playerBody != null && item.ItemCollider != null)
            {
                Physics.IgnoreCollision(playerBody, item.ItemCollider, false);
            }
        }

        private void ClearCarryFeedback()
        {
            prevAimedSlot?.SetNear(false);
            prevNearSlot?.SetNear(false);
            prevAimedSlot = null;
            prevNearSlot = null;
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
