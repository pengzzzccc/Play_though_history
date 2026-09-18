using System.Collections;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Marks an object the sceptre can grab and carry. Keep a collider on the
    /// object so the grab raycast can hit it. The item owns its flight
    /// animation into a slot (FlyTo); CurrentSlot tracks where it is seated
    /// at runtime — placement itself is never restricted by a slot reference.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarryableItem : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private float flightDuration = 0.35f;
        [SerializeField] private float flightArc = 0.25f;

        private Rigidbody body;
        private Collider itemCollider;
        private Renderer itemRenderer;
        private Coroutine flightRoutine;

        public string ItemId => itemId;
        public Rigidbody Body => body;
        public Collider ItemCollider => itemCollider;
        public Renderer ItemRenderer => itemRenderer;
        public ItemSlot CurrentSlot { get; private set; }
        public bool IsCarried { get; private set; }
        public bool IsFlying { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            itemCollider = GetComponentInChildren<Collider>();
            itemRenderer = GetComponentInChildren<Renderer>();
        }

        public void Grab()
        {
            IsCarried = true;
            body.isKinematic = true;
        }

        public void Drop()
        {
            IsCarried = false;
            body.isKinematic = false;
        }

        public void Place(ItemSlot slot)
        {
            IsCarried = false;
            CurrentSlot = slot;
            body.isKinematic = true;
        }

        /// <summary>
        /// Frees the item from its slot when it is grabbed again; the slot's
        /// own bookkeeping happens in ItemSlot.Vacate.
        /// </summary>
        public void ClearSlot()
        {
            CurrentSlot = null;
        }

        /// <summary>
        /// Kicked out of its slot by another item: back to dynamic physics so
        /// it drops. The slot applies a small pop impulse.
        /// </summary>
        public void Eject()
        {
            IsCarried = false;
            CurrentSlot = null;
            body.isKinematic = false;
        }

        /// <summary>
        /// Flies the item into the slot along an arc, then places it. Finishes
        /// instantly when the game leaves the exploring phase.
        /// </summary>
        public void FlyTo(ItemSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            if (flightRoutine != null)
            {
                StopCoroutine(flightRoutine);
            }

            flightRoutine = StartCoroutine(FlightRoutine(slot));
        }

        private IEnumerator FlightRoutine(ItemSlot slot)
        {
            IsFlying = true;
            IsCarried = false;
            var startPosition = transform.position;
            var startRotation = transform.rotation;
            var time = 0f;
            while (time < flightDuration)
            {
                time += Time.deltaTime;
                if (Game.Phase != GamePhase.Exploring)
                {
                    time = flightDuration;
                }

                var t = Mathf.Clamp01(time / flightDuration);
                var eased = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, slot.SnapPosition + slot.PlaceOffset, eased)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * flightArc);
                transform.rotation = Quaternion.Slerp(startRotation, slot.SnapRotation, eased);
                yield return null;
            }

            IsFlying = false;
            flightRoutine = null;
            if (slot.Place(this))
            {
                GameEvents.RaiseCarryablePlaced(this);
            }
            else
            {
                Drop();
            }
        }
    }
}
