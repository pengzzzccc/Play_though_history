using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.State;
using UnityEngine;

namespace UnknownTechnology.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private FirstPersonCameraController cameraController;
        [SerializeField] private PlayerAnimationController animationController;

        private GameContext context;
        private IDisposable phaseSubscription;
        private bool jumpRequested;

        public bool IsInitialized => context != null;

        private void Start()
        {
            if (!GameContextProvider.IsReady)
            {
                Debug.LogError("The player could not find a GameContext. Start from Bootstrap or use a development era scene.", this);
                enabled = false;
                return;
            }

            context = GameContextProvider.Current;
            phaseSubscription = context.EventBus.Subscribe<GamePhaseChanged>(message => ApplyCursor(message.Current));
            context.Input.PausePerformed += HandlePause;
            context.Input.CancelPerformed += HandleCancel;
            context.Input.JumpPerformed += HandleJump;
            ApplyCursor(context.State.Current.Phase);
        }

        private void Update()
        {
            if (context == null)
            {
                return;
            }

            var phase = context.State.Current.Phase;
            var canControl = phase == GamePhase.Exploring;
            motor.Tick(context.Input.Move, jumpRequested, canControl, Time.deltaTime);
            jumpRequested = false;
            cameraController.Tick(
                context.Input.Look,
                context.Input.CurrentControlScheme,
                context.Settings.Current,
                canControl,
                Time.deltaTime);
            animationController.Tick(motor.NormalizedSpeed, context.Input.ToolHeld, context.Settings.Current, Time.deltaTime);
        }

        private void HandlePause()
        {
            context?.State.TryPause(out _);
        }

        private void HandleCancel()
        {
            if (context != null && context.State.Current.Phase == GamePhase.Paused)
            {
                context.State.TryResume(out _);
            }
        }

        private void HandleJump()
        {
            jumpRequested = context != null && context.State.Current.Phase == GamePhase.Exploring;
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
            phaseSubscription?.Dispose();
            phaseSubscription = null;
            if (context != null)
            {
                context.Input.PausePerformed -= HandlePause;
                context.Input.CancelPerformed -= HandleCancel;
                context.Input.JumpPerformed -= HandleJump;
            }
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
