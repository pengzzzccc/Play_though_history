using System;
using System.Collections.Generic;
using UnknownTechnology.Core.State;
using UnityEngine;

namespace UnknownTechnology.Core.SceneFlow
{
    [Serializable]
    public sealed class SceneRoute
    {
        [SerializeField] private string id;
        [SerializeField] private string scenePath;
        [SerializeField] private GamePhase targetPhase;
        [SerializeField] private bool requiresAccess;

        public SceneRoute(string id, string scenePath, GamePhase targetPhase, bool requiresAccess)
        {
            this.id = id;
            this.scenePath = scenePath;
            this.targetPhase = targetPhase;
            this.requiresAccess = requiresAccess;
        }

        public string Id => id;
        public string ScenePath => scenePath;
        public GamePhase TargetPhase => targetPhase;
        public bool RequiresAccess => requiresAccess;
    }

    [CreateAssetMenu(menuName = "Unknown Technology/Scene Flow Config", fileName = "SceneFlowConfig")]
    public sealed class SceneFlowConfig : ScriptableObject
    {
        public const string MainMenuRoute = "menu";
        public const string AncientRoute = "era.ancient";
        public const string ModernRoute = "era.modern";
        public const string FutureRoute = "era.future";

        [SerializeField] private List<SceneRoute> routes = new();

        public IReadOnlyList<SceneRoute> Routes => routes;

        public bool TryGetRoute(string routeId, out SceneRoute route)
        {
            route = routes.Find(candidate => string.Equals(candidate.Id, routeId, StringComparison.Ordinal));
            return route != null;
        }

#if UNITY_EDITOR
        public void Configure(IEnumerable<SceneRoute> configuredRoutes)
        {
            routes = new List<SceneRoute>(configuredRoutes);
        }
#endif
    }
}
