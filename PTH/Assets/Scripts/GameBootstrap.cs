using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UnknownTechnology
{
    /// <summary>
    /// Single persistent root: owns PlayerInput, exposes polling input (Move/Look/ToolHeld),
    /// switches action maps with the game phase and auto-pauses when a device is lost.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction toolAction;
        private GamePhase pendingPhase;
        private bool hasPendingPhase;

        public Vector2 Move => moveAction != null && moveAction.enabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => lookAction != null && lookAction.enabled ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool ToolHeld => toolAction != null && toolAction.enabled && toolAction.IsPressed();
        public ControlScheme ControlScheme =>
            playerInput != null && string.Equals(playerInput.currentControlScheme, "Gamepad", StringComparison.Ordinal)
                ? ControlScheme.Gamepad
                : ControlScheme.KeyboardMouse;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            playerInput = GetComponent<PlayerInput>();
            moveAction = playerInput.actions.FindAction("Gameplay/Move", true);
            lookAction = playerInput.actions.FindAction("Gameplay/Look", true);
            toolAction = playerInput.actions.FindAction("Gameplay/Tool", true);
            Bind("Gameplay/Pause", () => Game.TryPause());
            Bind("Restoration/Pause", () => Game.TryPause());
            Bind("Gameplay/Jump", GameEvents.RaiseJumpPressed);
            Bind("Restoration/Cancel", GameEvents.RaiseCancelPressed);
            Bind("UI/Cancel", GameEvents.RaiseCancelPressed);
            playerInput.onDeviceLost += HandleDeviceLost;
            playerInput.onDeviceRegained += HandleDeviceRegained;

            GameEvents.PhaseChanged += QueuePhase;
            EnterActiveScene(SceneManager.GetActiveScene().name);
        }

        private void EnterActiveScene(string sceneName)
        {
            if (sceneName == "Bootstrap")
            {
                Game.LoadScene("MainMenu", GamePhase.MainMenu);
                return;
            }

            if (sceneName == "MainMenu")
            {
                Game.SetPhase(GamePhase.MainMenu);
                return;
            }

            if (sceneName.StartsWith("Era_", StringComparison.Ordinal))
            {
                Game.SetPhase(GamePhase.Exploring);
            }
        }

        private void QueuePhase(GamePhase phase)
        {
            // A phase change can originate inside a UI action callback; switching that
            // map immediately would invalidate the CallbackContext still in use, so
            // apply the latest phase at frame end.
            pendingPhase = phase;
            hasPendingPhase = true;
        }

        private void LateUpdate()
        {
            if (!hasPendingPhase)
            {
                return;
            }

            hasPendingPhase = false;
            ApplyPhase(pendingPhase);
        }

        private void ApplyPhase(GamePhase phase)
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            foreach (var map in playerInput.actions.actionMaps)
            {
                map.Disable();
            }

            var mapName = phase switch
            {
                GamePhase.Exploring => "Gameplay",
                GamePhase.MainMenu or GamePhase.Paused => "UI",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(mapName))
            {
                playerInput.actions.FindActionMap(mapName, true).Enable();
            }
        }

        private void Bind(string actionPath, Action callback)
        {
            playerInput.actions.FindAction(actionPath, true).performed += _ => callback();
        }

        private void HandleDeviceLost(PlayerInput input)
        {
            var displayName = input.devices.Count > 0 ? input.devices[0].displayName : "Input device";
            GameEvents.RaiseDeviceLost(displayName);
            Game.TryPause();
        }

        private void HandleDeviceRegained(PlayerInput input)
        {
            var displayName = input.devices.Count > 0 ? input.devices[0].displayName : "Input device";
            GameEvents.RaiseDeviceRegained(displayName);
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            GameEvents.PhaseChanged -= QueuePhase;
            if (playerInput != null)
            {
                playerInput.onDeviceLost -= HandleDeviceLost;
                playerInput.onDeviceRegained -= HandleDeviceRegained;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}
