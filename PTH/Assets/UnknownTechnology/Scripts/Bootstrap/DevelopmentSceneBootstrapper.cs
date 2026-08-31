using UnknownTechnology.Core.State;
using UnityEngine;

namespace UnknownTechnology.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public sealed class DevelopmentSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameBootstrap bootstrapPrefab;

        private void Awake()
        {
            if (!GameContextProvider.IsReady && GameBootstrap.Instance == null && bootstrapPrefab != null)
            {
                Instantiate(bootstrapPrefab);
            }
        }

#if UNITY_EDITOR
        public void Configure(GameBootstrap configuredBootstrapPrefab)
        {
            bootstrapPrefab = configuredBootstrapPrefab;
        }
#endif
    }
}
