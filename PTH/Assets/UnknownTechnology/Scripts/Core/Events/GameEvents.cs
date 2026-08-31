using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;

namespace UnknownTechnology.Core.Events
{
    public readonly struct BootstrapReady
    {
    }

    public readonly struct FatalErrorRaised
    {
        public FatalErrorRaised(string message)
        {
            Message = message ?? string.Empty;
        }

        public string Message { get; }
    }

    public readonly struct GamePhaseChanged
    {
        public GamePhaseChanged(GamePhase previous, GamePhase current, GamePhase resumePhase)
        {
            Previous = previous;
            Current = current;
            ResumePhase = resumePhase;
        }

        public GamePhase Previous { get; }
        public GamePhase Current { get; }
        public GamePhase ResumePhase { get; }
    }

    public readonly struct SceneLoadRequested
    {
        public SceneLoadRequested(string routeId)
        {
            RouteId = routeId ?? string.Empty;
        }

        public string RouteId { get; }
    }

    public readonly struct SceneLoadStarted
    {
        public SceneLoadStarted(string routeId)
        {
            RouteId = routeId;
        }

        public string RouteId { get; }
    }

    public readonly struct SceneLoadProgressed
    {
        public SceneLoadProgressed(string routeId, float progress)
        {
            RouteId = routeId;
            Progress = progress;
        }

        public string RouteId { get; }
        public float Progress { get; }
    }

    public readonly struct SceneReady
    {
        public SceneReady(string routeId)
        {
            RouteId = routeId;
        }

        public string RouteId { get; }
    }

    public readonly struct SceneLoadRejected
    {
        public SceneLoadRejected(string routeId, string reason)
        {
            RouteId = routeId;
            Reason = reason;
        }

        public string RouteId { get; }
        public string Reason { get; }
    }

    public readonly struct SceneLoadFailed
    {
        public SceneLoadFailed(string routeId, string reason)
        {
            RouteId = routeId;
            Reason = reason;
        }

        public string RouteId { get; }
        public string Reason { get; }
    }

    public readonly struct PauseRequested
    {
    }

    public readonly struct ResumeRequested
    {
    }

    public readonly struct ControlSchemeChanged
    {
        public ControlSchemeChanged(ControlScheme scheme)
        {
            Scheme = scheme;
        }

        public ControlScheme Scheme { get; }
    }

    public readonly struct InputDeviceLost
    {
        public InputDeviceLost(string displayName)
        {
            DisplayName = displayName ?? string.Empty;
        }

        public string DisplayName { get; }
    }

    public readonly struct InputDeviceRegained
    {
        public InputDeviceRegained(string displayName)
        {
            DisplayName = displayName ?? string.Empty;
        }

        public string DisplayName { get; }
    }

    public readonly struct SettingsChanged
    {
        public SettingsChanged(GameSettingsSnapshot settings)
        {
            Settings = settings;
        }

        public GameSettingsSnapshot Settings { get; }
    }
}
