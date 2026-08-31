using System;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnknownTechnology.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private SceneFlowController sceneFlowController;

        private static GameBootstrap instance;
        private EventBus eventBus;
        private IDisposable pauseSubscription;
        private IDisposable resumeSubscription;
        private IDisposable deviceLostSubscription;

        public static GameBootstrap Instance => instance;
        public GameContext Context { get; private set; }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("A duplicate GameBootstrap was rejected.", this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            eventBus = new EventBus();
            var state = new GameStateStore(eventBus);
            var settings = new SettingsService(new PlayerPrefsSettingsStore(), eventBus);
            var accessPolicy = new DefaultSceneAccessPolicy();
            sceneFlowController.Initialize(eventBus, state, accessPolicy, new NoOpSceneProgressRestorer());
            inputRouter.Initialize(eventBus, state, settings);

            Context = new GameContext(eventBus, state, settings, inputRouter, sceneFlowController);
            GameContextProvider.Register(Context);

            pauseSubscription = eventBus.Subscribe<PauseRequested>(message => state.TryPause(out _));
            resumeSubscription = eventBus.Subscribe<ResumeRequested>(message => state.TryResume(out _));
            deviceLostSubscription = eventBus.Subscribe<InputDeviceLost>(message => state.TryPause(out _));
            eventBus.Publish(new BootstrapReady());

            InitializeEntryScene(SceneManager.GetActiveScene().name);
        }

        private void InitializeEntryScene(string activeSceneName)
        {
            if (string.Equals(activeSceneName, "Bootstrap", StringComparison.Ordinal))
            {
                sceneFlowController.RequestLoad(SceneFlowConfig.MainMenuRoute);
                return;
            }

            if (string.Equals(activeSceneName, "MainMenu", StringComparison.Ordinal))
            {
                Context.State.TryTransition(GamePhase.MainMenu, out _);
                return;
            }

            if (activeSceneName.StartsWith("Era_", StringComparison.Ordinal))
            {
                Context.State.TryTransition(GamePhase.Exploring, out _);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            pauseSubscription?.Dispose();
            resumeSubscription?.Dispose();
            deviceLostSubscription?.Dispose();
            eventBus?.Clear();
            GameContextProvider.Reset();
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

#if UNITY_EDITOR
        public void Configure(InputRouter configuredInputRouter, SceneFlowController configuredSceneFlowController)
        {
            inputRouter = configuredInputRouter;
            sceneFlowController = configuredSceneFlowController;
        }
#endif
    }
}
