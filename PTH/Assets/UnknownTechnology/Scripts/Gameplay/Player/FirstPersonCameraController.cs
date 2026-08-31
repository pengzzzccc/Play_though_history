using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.Settings;
using UnityEngine;

namespace UnknownTechnology.Gameplay.Player
{
    public sealed class FirstPersonCameraController : MonoBehaviour
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
            GameSettingsSnapshot settings,
            bool canLook,
            float deltaTime)
        {
            if (!canLook || playerRoot == null || pitchPivot == null)
            {
                return;
            }

            var scale = controlScheme == ControlScheme.Gamepad
                ? settings.GamepadSensitivity * deltaTime
                : settings.MouseSensitivity;
            var yawDelta = lookInput.x * scale;
            var pitchDelta = lookInput.y * scale * (settings.InvertY ? 1f : -1f);

            playerRoot.Rotate(Vector3.up, yawDelta, Space.World);
            pitch = Mathf.Clamp(pitch + pitchDelta, minimumPitch, maximumPitch);
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void ResetView(float yaw = 0f, float configuredPitch = 0f)
        {
            pitch = Mathf.Clamp(configuredPitch, minimumPitch, maximumPitch);
            if (playerRoot != null)
            {
                playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (pitchPivot != null)
            {
                pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
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
