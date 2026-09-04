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

        public static event Action CancelPressed;
        public static event Action JumpPressed;
        public static event Action<string> DeviceLost;
        public static event Action<string> DeviceRegained;

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
            Bind("Gameplay/Pause", Game.TryPause);
            Bind("Restoration/Pause", Game.TryPause);
            Bind("Gameplay/Jump", () => JumpPressed?.Invoke());
            Bind("Restoration/Cancel", () => CancelPressed?.Invoke());
            Bind("UI/Cancel", () => CancelPressed?.Invoke());
            playerInput.onDeviceLost += HandleDeviceLost;
            playerInput.onDeviceRegained += HandleDeviceRegained;

            Game.PhaseChanged += QueuePhase;
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
                GamePhase.Restoration => "Restoration",
                GamePhase.MainMenu or GamePhase.Dialogue or GamePhase.Quiz or GamePhase.Paused or GamePhase.Completed => "UI",
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
            DeviceLost?.Invoke(displayName);
            Game.TryPause();
        }

        private void HandleDeviceRegained(PlayerInput input)
        {
            var displayName = input.devices.Count > 0 ? input.devices[0].displayName : "Input device";
            DeviceRegained?.Invoke(displayName);
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            Game.PhaseChanged -= QueuePhase;
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
            CancelPressed = null;
            JumpPressed = null;
            DeviceLost = null;
            DeviceRegained = null;
        }
    }

    /// <summary>
    /// Placed in era scenes so they can be opened directly in the editor:
    /// spawns the persistent game root when it does not exist yet.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class DevSceneSetup : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrapPrefab;

        private void Awake()
        {
            if (GameBootstrap.Instance == null && bootstrapPrefab != null)
            {
                Instantiate(bootstrapPrefab);
            }
        }

#if UNITY_EDITOR
        public void Configure(GameBootstrap prefab)
        {
            bootstrapPrefab = prefab;
        }
#endif
    }
}
