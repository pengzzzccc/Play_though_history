using UnityEngine;

namespace UnknownTechnology
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedVelocity = -2f;
        [SerializeField] private float maximumFallSpeed = -35f;

        private CharacterController characterController;
        private float verticalVelocity;

        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void Tick(Vector2 input, bool jumpRequested, bool canMove, float deltaTime)
        {
            if (characterController == null || !characterController.enabled || deltaTime <= 0f)
            {
                return;
            }

            var planarInput = canMove ? Vector2.ClampMagnitude(input, 1f) : Vector2.zero;
            var planarVelocity = transform.right * planarInput.x + transform.forward * planarInput.y;
            NormalizedSpeed = planarInput.magnitude;

            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = groundedVelocity;
                }

                if (canMove && jumpRequested && jumpHeight > 0f && gravity < 0f)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {
                verticalVelocity = Mathf.Max(maximumFallSpeed, verticalVelocity + gravity * deltaTime);
            }

            var velocity = planarVelocity * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * deltaTime);
        }

        public void StopImmediately()
        {
            NormalizedSpeed = 0f;
            verticalVelocity = characterController != null && characterController.isGrounded ? groundedVelocity : 0f;
        }
    }
}
