using System.Collections;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Tweens itself open to the target pose while an item whose id matches
    /// itemID sits in a slot, and tweens back to its original pose when that
    /// item is grabbed away or ejected by another item.
    /// </summary>
    public class TriggerGate : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private string itemID;

        [Header("Move Setup")]
        [SerializeField][Range(1, 10)] private float moveDuration = 2.5f;
        [SerializeField] private bool RotateToTarget = true;
        [SerializeField] private bool TransToTarget = true;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Coroutine moveRoutine;

        private void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Start()
        {
            GameEvents.CarryablePlaced += PlacedItemCheck;
            GameEvents.CarryableVacated += VacatedItemCheck;
        }

        private void PlacedItemCheck(CarryableItem item)
        {
            if (item == null || target == null || item.ItemId != itemID)
            {
                return;
            }

            TweenToPose(target.position, target.rotation);
        }

        private void VacatedItemCheck(CarryableItem item)
        {
            if (item == null || item.ItemId != itemID)
            {
                return;
            }

            TweenToPose(startPosition, startRotation);
        }

        private void TweenToPose(Vector3 endPosition, Quaternion endRotation)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveToPose(endPosition, endRotation));
        }

        private IEnumerator MoveToPose(Vector3 endPosition, Quaternion endRotation)
        {
            // Start from wherever the gate currently is so an interrupted
            // tween reverses smoothly instead of snapping.
            var currentPosition = transform.position;
            var currentRotation = transform.rotation;
            var time = 0f;
            while (time <= moveDuration)
            {
                time += Time.deltaTime;
                var k = moveCurve.Evaluate(Mathf.Clamp01(time / moveDuration));

                if (TransToTarget)
                {
                    transform.position = Vector3.Lerp(currentPosition, endPosition, k);
                }

                if (RotateToTarget)
                {
                    transform.rotation = Quaternion.Slerp(currentRotation, endRotation, k);
                }

                yield return null;
            }

            if (TransToTarget)
            {
                transform.position = endPosition;
            }

            if (RotateToTarget)
            {
                transform.rotation = endRotation;
            }

            moveRoutine = null;
        }

        private void OnDestroy()
        {
            GameEvents.CarryablePlaced -= PlacedItemCheck;
            GameEvents.CarryableVacated -= VacatedItemCheck;
        }
    }
}
