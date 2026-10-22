using UnityEngine;
using UnknownTechnology.Audio;

namespace UnknownTechnology
{
    /// <summary>
    /// Drives motor, camera and presentation from the global input each frame,
    /// manages the cursor with the game phase and places itself at PlayerSpawn on start.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class FirstPersonPlayer : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private FirstPersonCameraController cameraController;
        [SerializeField] private PlayerAnimationController animationController;
        [SerializeField] private Scepter scepter;

        private bool jumpRequested;
        [SerializeField] private float footstepInterval = 0.45f;

        private float footstepTimer;
        private AudioManager audioManager;
        private CharacterController characterController;

        private void Start()
        {
            if (Game.Runtime == null)
            {
                Debug.LogError("No GameBootstrap found. Start from Bootstrap or add DevSceneSetup to the scene.", this);
                enabled = false;
                return;
            }
            audioManager = FindAnyObjectByType<AudioManager>();
            characterController = GetComponent<CharacterController>();

            PlaceAtSpawn();
            GameEvents.PhaseChanged += ApplyCursor;
            GameEvents.JumpPressed += HandleJump;
            ApplyCursor(Game.Phase);
        }

        private void Update()
        {

            var input = Game.Runtime;
            if (input == null)
            {
                return;
            }

            var canControl = Game.Phase == GamePhase.Exploring && !CanvasQuizUI.IsQuizOpen && !SceneTransition.IsTransitioning;

            motor.Tick(input.Move, jumpRequested, canControl, Time.deltaTime);
            UpdateFootsteps(canControl);
            jumpRequested = false;

            cameraController.Tick(
                input.Look,
                input.ControlScheme,
                Game.Settings,
                canControl,
                Time.deltaTime
            );

            animationController.Tick(
                motor.NormalizedSpeed,
                input.ToolHeld,
                Game.Settings,
                Time.deltaTime
            );

            if (scepter != null)
            {
                scepter.Tick(input.ToolHeld, canControl, Time.deltaTime);
            }
        }
        private void UpdateFootsteps(bool canControl)
        {
            bool isMoving = motor.NormalizedSpeed > 0.1f;
            bool isGrounded = characterController != null && characterController.isGrounded;

            if (!canControl || !isMoving || !isGrounded)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                audioManager?.PlayFootstepSound();
                footstepTimer = footstepInterval;
            }
        }

        public float CameraPitch => cameraController != null ? cameraController.Pitch : 0f;

        public void ApplySavedPose(Vector3 position, float yaw, float pitch)
        {
            Teleport(position, Quaternion.Euler(0f, yaw, 0f));
            if (cameraController != null)
            {
                cameraController.SetPitch(pitch);
            }

            motor.StopImmediately();
        }

        private void PlaceAtSpawn()
        {
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn == null)
            {
                return;
            }

            Teleport(spawn.transform.position, spawn.transform.rotation);
        }

        private void Teleport(Vector3 position, Quaternion rotation)
        {
            // CharacterController overrides transform writes while enabled.
            var controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            transform.SetPositionAndRotation(position, rotation);
            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private void HandleJump()
        {
            jumpRequested = Game.Phase == GamePhase.Exploring;
        }

        private void ApplyCursor(GamePhase phase)
        {
            // Keep the cursor locked while a level transition is fading in/out;
            // a normal menu-driven load still unlocks it as before.
            var locked = phase == GamePhase.Exploring ||
                (phase == GamePhase.Loading && SceneTransition.IsTransitioning);
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            if (!locked)
            {
                jumpRequested = false;
                motor.StopImmediately();
            }
        }

        private void OnDestroy()
        {
            GameEvents.PhaseChanged -= ApplyCursor;
            GameEvents.JumpPressed -= HandleJump;
        }

#if UNITY_EDITOR
        public void Configure(
            PlayerMotor configuredMotor,
            FirstPersonCameraController configuredCameraController,
            PlayerAnimationController configuredAnimationController)
        {
            motor = configuredMotor;
            cameraController = configuredCameraController;
            animationController = configuredAnimationController;
        }
#endif
    }
}
