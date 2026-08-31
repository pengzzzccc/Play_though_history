using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.Settings;
using UnityEngine;

namespace UnknownTechnology.Core.State
{
    public sealed class GameContext
    {
        public GameContext(
            IEventBus eventBus,
            IGameStateStore state,
            ISettingsService settings,
            IInputReader input,
            ISceneFlowService sceneFlow)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            SceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public IEventBus EventBus { get; }
        public IGameStateStore State { get; }
        public ISettingsService Settings { get; }
        public IInputReader Input { get; }
        public ISceneFlowService SceneFlow { get; }
    }

    public static class GameContextProvider
    {
        public static GameContext Current { get; private set; }
        public static bool IsReady => Current != null;

        public static void Register(GameContext context)
        {
            if (Current != null && !ReferenceEquals(Current, context))
            {
                throw new InvalidOperationException("A different GameContext is already registered.");
            }

            Current = context ?? throw new ArgumentNullException(nameof(context));
        }

        public static void Reset()
        {
            Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            Reset();
        }
    }
}
