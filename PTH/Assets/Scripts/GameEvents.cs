using System;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Every global game event lives here. Subscribe in OnEnable/Start and unsubscribe
    /// in OnDestroy; all events are cleared automatically when a play session starts.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GamePhase> PhaseChanged;
        public static event Action CancelPressed;
        public static event Action JumpPressed;
        public static event Action<string> DeviceLost;
        public static event Action<string> DeviceRegained;

        public static void RaisePhaseChanged(GamePhase phase) => PhaseChanged?.Invoke(phase);
        public static void RaiseCancelPressed() => CancelPressed?.Invoke();
        public static void RaiseJumpPressed() => JumpPressed?.Invoke();
        public static void RaiseDeviceLost(string displayName) => DeviceLost?.Invoke(displayName);
        public static void RaiseDeviceRegained(string displayName) => DeviceRegained?.Invoke(displayName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            PhaseChanged = null;
            CancelPressed = null;
            JumpPressed = null;
            DeviceLost = null;
            DeviceRegained = null;
        }
    }
}
