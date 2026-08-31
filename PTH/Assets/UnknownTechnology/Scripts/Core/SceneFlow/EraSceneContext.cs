using UnityEngine;

namespace UnknownTechnology.Core.SceneFlow
{
    public sealed class EraSceneContext : MonoBehaviour
    {
        [SerializeField] private string routeId;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform playerRoot;

        public string RouteId => routeId;
        public Transform SpawnPoint => spawnPoint;
        public Transform PlayerRoot => playerRoot;
        public bool IsValid => !string.IsNullOrWhiteSpace(routeId) && spawnPoint != null && playerRoot != null;

        public bool PlacePlayer(out string error)
        {
            if (!IsValid)
            {
                error = "EraSceneContext requires a route ID, spawn point, and player root.";
                return false;
            }

            var controller = playerRoot.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            playerRoot.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void Configure(string configuredRouteId, Transform configuredSpawnPoint, Transform configuredPlayerRoot)
        {
            routeId = configuredRouteId;
            spawnPoint = configuredSpawnPoint;
            playerRoot = configuredPlayerRoot;
        }
#endif
    }
}
