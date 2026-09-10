using UnityEngine;

namespace UnknownTechnology
{
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
