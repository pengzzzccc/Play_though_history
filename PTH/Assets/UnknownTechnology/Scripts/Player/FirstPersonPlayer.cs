using UnityEngine;

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

        private bool jumpRequested;

        private void Start()
        {
            if (Game.Runtime == null)
            {
                Debug.LogError("No GameBootstrap found. Start from Bootstrap or add DevSceneSetup to the scene.", this);
                enabled = false;
                return;
            }

            PlaceAtSpawn();
            Game.PhaseChanged += ApplyCursor;
            GameBootstrap.CancelPressed += HandleCancel;
            GameBootstrap.JumpPressed += HandleJump;
            ApplyCursor(Game.Phase);
        }

        private void Update()
        {
            var input = Game.Runtime;
            if (input == null)
            {
                return;
            }

            var canControl = Game.Phase == GamePhase.Exploring;
            motor.Tick(input.Move, jumpRequested, canControl, Time.deltaTime);
            jumpRequested = false;
            cameraController.Tick(input.Look, input.ControlScheme, Game.Settings, canControl, Time.deltaTime);
            animationController.Tick(motor.NormalizedSpeed, input.ToolHeld, Game.Settings, Time.deltaTime);
        }

        private void PlaceAtSpawn()
        {
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn == null)
            {
                return;
            }

            var controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private void HandleCancel()
        {
            if (Game.Phase == GamePhase.Paused)
            {
                Game.TryResume();
            }
        }

        private void HandleJump()
        {
            jumpRequested = Game.Phase == GamePhase.Exploring;
        }

        private void ApplyCursor(GamePhase phase)
        {
            var locked = phase == GamePhase.Exploring;
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
            Game.PhaseChanged -= ApplyCursor;
            GameBootstrap.CancelPressed -= HandleCancel;
            GameBootstrap.JumpPressed -= HandleJump;
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
