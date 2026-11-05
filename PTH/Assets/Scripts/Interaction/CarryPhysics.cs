using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Shared obstruction rule for carried and flying items: triggers, the item
    /// itself and the slot it is flying into never count as blocking.
    /// </summary>
    public static class CarryPhysics
    {
        public static bool IsBlocking(RaycastHit hit, CarryableItem item, ItemSlot ignoreSlot)
        {
            var hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger)
            {
                return false;
            }

            if (item != null && hitCollider.transform.IsChildOf(item.transform))
            {
                return false;
            }

            return ignoreSlot == null || hitCollider.GetComponentInParent<ItemSlot>() != ignoreSlot;
        }
    }
}
