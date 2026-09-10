using UnityEngine;

namespace UnknownTechnology
{
    public class FirstPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform pitchPivot;
        [SerializeField] private float minimumPitch = -80f;
        [SerializeField] private float maximumPitch = 80f;

        private float pitch;

        public float Pitch => pitch;

        public void Tick(
            Vector2 lookInput,
            ControlScheme controlScheme,
            GameSettings settings,
            bool canLook,
            float deltaTime)
        {
            if (!canLook || playerRoot == null || pitchPivot == null)
            {
                return;
            }

            var scale = controlScheme == ControlScheme.Gamepad
                ? settings.gamepadSensitivity * deltaTime
                : settings.mouseSensitivity;
            var yawDelta = lookInput.x * scale;
            var pitchDelta = lookInput.y * scale * (settings.invertY ? 1f : -1f);

            playerRoot.Rotate(Vector3.up, yawDelta, Space.World);
            pitch = Mathf.Clamp(pitch + pitchDelta, minimumPitch, maximumPitch);
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

#if UNITY_EDITOR
        public void Configure(Transform configuredPlayerRoot, Transform configuredPitchPivot)
        {
            playerRoot = configuredPlayerRoot;
            pitchPivot = configuredPitchPivot;
        }
#endif
    }
}
