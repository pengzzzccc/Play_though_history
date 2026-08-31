using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownTechnology.Gameplay.Input
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class InputRouter : MonoBehaviour, IInputReader
    {
        public const string GameplayMapName = "Gameplay";
        public const string RestorationMapName = "Restoration";
        public const string UiMapName = "UI";

        private PlayerInput playerInput;
        private IEventBus eventBus;
        private ISettingsService settings;
        private IDisposable phaseSubscription;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction toolAction;
        private GamePhase pendingPhase;
        private bool hasPendingPhase;
        private bool initialized;

        public Vector2 Move => moveAction != null && moveAction.enabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => lookAction != null && lookAction.enabled ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool ToolHeld => toolAction != null && toolAction.enabled && toolAction.IsPressed();
        public ControlScheme CurrentControlScheme { get; private set; } = ControlScheme.KeyboardMouse;
        public string ActiveMapName { get; private set; } = string.Empty;
        public PlayerInput PlayerInput => playerInput;

        public event Action InteractPerformed;
        public event Action JumpPerformed;
        public event Action ToolPerformed;
        public event Action NotebookPerformed;
        public event Action PausePerformed;
        public event Action CancelPerformed;

        public void Initialize(IEventBus configuredEventBus, IGameStateStore state, ISettingsService configuredSettings)
        {
            if (initialized)
            {
                return;
            }

            eventBus = configuredEventBus ?? throw new ArgumentNullException(nameof(configuredEventBus));
            settings = configuredSettings ?? throw new ArgumentNullException(nameof(configuredSettings));
            playerInput = GetComponent<PlayerInput>();
            if (playerInput.actions == null)
            {
                throw new InvalidOperationException("PlayerInput must reference the Unknown Technology input asset.");
            }

            CacheActions();
            BindCallbacks();
            playerInput.onControlsChanged += HandleControlsChanged;
            playerInput.onDeviceLost += HandleDeviceLost;
            playerInput.onDeviceRegained += HandleDeviceRegained;
            phaseSubscription = eventBus.Subscribe<GamePhaseChanged>(message => QueuePhase(message.Current));
            ApplyPhase(state.Current.Phase);
            UpdateControlScheme();
            initialized = true;
        }

        public void ApplyPhase(GamePhase phase)
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            hasPendingPhase = false;
            foreach (var map in playerInput.actions.actionMaps)
            {
                map.Disable();
            }

            ActiveMapName = phase switch
            {
                GamePhase.Exploring => GameplayMapName,
                GamePhase.Restoration => RestorationMapName,
                GamePhase.MainMenu or GamePhase.Dialogue or GamePhase.Quiz or GamePhase.Paused or GamePhase.Completed => UiMapName,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(ActiveMapName))
            {
                playerInput.actions.FindActionMap(ActiveMapName, true).Enable();
            }
        }

        private void QueuePhase(GamePhase phase)
        {
            // State changes can originate inside a UI action callback. Disabling that
            // action map immediately invalidates the CallbackContext still used by the
            // InputSystemUIInputModule, so apply only the latest phase at frame end.
            pendingPhase = phase;
            hasPendingPhase = true;
        }

        private void LateUpdate()
        {
            if (!hasPendingPhase)
            {
                return;
            }

            var phase = pendingPhase;
            hasPendingPhase = false;
            ApplyPhase(phase);
        }

        private void CacheActions()
        {
            moveAction = playerInput.actions.FindAction($"{GameplayMapName}/Move", true);
            lookAction = playerInput.actions.FindAction($"{GameplayMapName}/Look", true);
            toolAction = playerInput.actions.FindAction($"{GameplayMapName}/Tool", true);
        }

        private void BindCallbacks()
        {
            BindPerformed($"{GameplayMapName}/Interact", () => InteractPerformed?.Invoke());
            BindPerformed($"{GameplayMapName}/Jump", () => JumpPerformed?.Invoke());
            BindPerformed($"{GameplayMapName}/Tool", () => ToolPerformed?.Invoke());
            BindPerformed($"{GameplayMapName}/Notebook", () => NotebookPerformed?.Invoke());
            BindPerformed($"{GameplayMapName}/Pause", () => PausePerformed?.Invoke());
            BindPerformed($"{RestorationMapName}/Pause", () => PausePerformed?.Invoke());
            BindPerformed($"{RestorationMapName}/Cancel", () => CancelPerformed?.Invoke());
            BindPerformed($"{UiMapName}/Cancel", () => CancelPerformed?.Invoke());
        }

        private void BindPerformed(string actionPath, Action callback)
        {
            playerInput.actions.FindAction(actionPath, true).performed += _ => callback();
        }

        private void HandleControlsChanged(PlayerInput changedInput)
        {
            UpdateControlScheme();
        }

        private void UpdateControlScheme()
        {
            var next = string.Equals(playerInput.currentControlScheme, "Gamepad", StringComparison.Ordinal)
                ? ControlScheme.Gamepad
                : ControlScheme.KeyboardMouse;
            if (next == CurrentControlScheme)
            {
                return;
            }

            CurrentControlScheme = next;
            eventBus?.Publish(new ControlSchemeChanged(next));
        }

        private void HandleDeviceLost(PlayerInput input)
        {
            var displayName = input.devices.Count > 0 ? input.devices[0].displayName : "Input device";
            eventBus?.Publish(new InputDeviceLost(displayName));
        }

        private void HandleDeviceRegained(PlayerInput input)
        {
            var displayName = input.devices.Count > 0 ? input.devices[0].displayName : "Input device";
            eventBus?.Publish(new InputDeviceRegained(displayName));
        }

        private void OnDestroy()
        {
            hasPendingPhase = false;
            phaseSubscription?.Dispose();
            phaseSubscription = null;
            if (playerInput != null)
            {
                playerInput.onControlsChanged -= HandleControlsChanged;
                playerInput.onDeviceLost -= HandleDeviceLost;
                playerInput.onDeviceRegained -= HandleDeviceRegained;
            }
        }
    }
}
