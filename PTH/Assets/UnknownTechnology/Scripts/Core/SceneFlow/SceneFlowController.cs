using System;
using System.Collections;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology.Core.SceneFlow
{
    public sealed class SceneFlowController : MonoBehaviour, ISceneFlowService
    {
        [SerializeField] private SceneFlowConfig config;

        private IEventBus eventBus;
        private IGameStateStore state;
        private ISceneAccessPolicy accessPolicy;
        private ISceneProgressRestorer progressRestorer;
        private IDisposable requestSubscription;
        private bool initialized;

        public bool IsLoading { get; private set; }
        public SceneFlowConfig Config => config;

        public void Initialize(
            IEventBus configuredEventBus,
            IGameStateStore configuredState,
            ISceneAccessPolicy configuredAccessPolicy,
            ISceneProgressRestorer configuredProgressRestorer)
        {
            if (initialized)
            {
                return;
            }

            eventBus = configuredEventBus ?? throw new ArgumentNullException(nameof(configuredEventBus));
            state = configuredState ?? throw new ArgumentNullException(nameof(configuredState));
            accessPolicy = configuredAccessPolicy ?? throw new ArgumentNullException(nameof(configuredAccessPolicy));
            progressRestorer = configuredProgressRestorer ?? throw new ArgumentNullException(nameof(configuredProgressRestorer));
            requestSubscription = eventBus.Subscribe<SceneLoadRequested>(request => RequestLoad(request.RouteId));
            initialized = true;
        }

        public bool RequestLoad(string routeId)
        {
            if (!initialized)
            {
                Debug.LogError("SceneFlowController has not been initialized.");
                return false;
            }

            if (IsLoading)
            {
                eventBus.Publish(new SceneLoadRejected(routeId, "Another scene load is already in progress."));
                return false;
            }

            if (config == null || !config.TryGetRoute(routeId, out var route))
            {
                eventBus.Publish(new SceneLoadRejected(routeId, "The requested scene route is not configured."));
                return false;
            }

            if (route.RequiresAccess && !accessPolicy.CanEnter(routeId, out var accessReason))
            {
                eventBus.Publish(new SceneLoadRejected(routeId, accessReason));
                return false;
            }

            StartCoroutine(LoadRoutine(route));
            return true;
        }

        private IEnumerator LoadRoutine(SceneRoute route)
        {
            IsLoading = true;
            if (!state.TryTransition(GamePhase.Loading, out var transitionReason))
            {
                eventBus.Publish(new SceneLoadRejected(route.Id, transitionReason));
                IsLoading = false;
                yield break;
            }

            eventBus.Publish(new SceneLoadStarted(route.Id));

            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(route.ScenePath, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Fail(route.Id, exception.Message);
                yield break;
            }

            if (operation == null)
            {
                Fail(route.Id, $"Unity could not start loading '{route.ScenePath}'.");
                yield break;
            }

            while (!operation.isDone)
            {
                eventBus.Publish(new SceneLoadProgressed(route.Id, Mathf.Clamp01(operation.progress / 0.9f)));
                yield return null;
            }

            yield return null;

            if (route.TargetPhase == GamePhase.Exploring)
            {
                var context = FindAnyObjectByType<EraSceneContext>();
                var contextError = string.Empty;
                if (context == null || !context.PlacePlayer(out contextError))
                {
                    Fail(route.Id, context == null ? "EraSceneContext is missing." : contextError);
                    yield break;
                }

                if (!string.Equals(context.RouteId, route.Id, StringComparison.Ordinal))
                {
                    Fail(route.Id, $"EraSceneContext route '{context.RouteId}' does not match '{route.Id}'.");
                    yield break;
                }

                progressRestorer.Restore(context);
            }

            if (!state.TryTransition(route.TargetPhase, out transitionReason))
            {
                Fail(route.Id, transitionReason);
                yield break;
            }

            IsLoading = false;
            eventBus.Publish(new SceneReady(route.Id));
        }

        private void Fail(string routeId, string reason)
        {
            IsLoading = false;
            state.TryTransition(GamePhase.FatalError, out _);
            eventBus.Publish(new SceneLoadFailed(routeId, reason));
            eventBus.Publish(new FatalErrorRaised(reason));
        }

        private void OnDestroy()
        {
            requestSubscription?.Dispose();
            requestSubscription = null;
        }

#if UNITY_EDITOR
        public void Configure(SceneFlowConfig configuredConfig)
        {
            config = configuredConfig;
        }
#endif
    }
}
