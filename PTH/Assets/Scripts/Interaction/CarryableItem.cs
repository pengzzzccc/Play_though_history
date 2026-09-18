using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Marks an object the sceptre can grab and carry. Keep a collider on the
    /// object so the grab raycast can hit it; targetSlot is optional — items
    /// without a slot carry fine but never highlight and never snap into place.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarryableItem : MonoBehaviour
    {
        [SerializeField] private ItemSlot targetSlot;

        private Rigidbody body;
        private Collider itemCollider;

        public ItemSlot TargetSlot => targetSlot;
        public Rigidbody Body => body;
        public Collider ItemCollider => itemCollider;
        public bool IsCarried { get; private set; }
        public bool IsPlaced { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            itemCollider = GetComponentInChildren<Collider>();
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

        public void Place()
        {
            IsCarried = false;
            IsPlaced = true;
            body.isKinematic = true;
        }
    }
}
