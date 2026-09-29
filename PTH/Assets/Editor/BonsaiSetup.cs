using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology
{
    /// <summary>
    /// One-shot helper that turns the imported bonsai model into a grabbable
    /// prefab: mesh + tree material, convex mesh collider, rigidbody and the
    /// CarryableItem component. Run via Unknown Technology → Setup Bonsai
    /// Grabbable while an Era scene is open; it also drops one instance next
    /// to the player spawn.
    /// </summary>
    public static class BonsaiSetup
    {
        private const string ModelPath = "Assets/Model/bonsai.obj";
        private const string MaterialPath = "Assets/Model/tree.mat";
        private const string PrefabPath = "Assets/Prefabs/Bonsai.prefab";
        private static readonly Vector3 SpawnPosition = new Vector3(1.5f, 0.05f, -3f);

        [MenuItem("Unknown Technology/Setup Bonsai Grabbable")]
        public static void Setup()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"Bonsai setup: material missing at {MaterialPath}.");
                return;
            }

            var mesh = LoadMesh();
            if (mesh == null)
            {
                Debug.LogError($"Bonsai setup: no mesh found in {ModelPath}.");
                return;
            }

            var root = new GameObject("Bonsai");
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            var collider = root.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
            root.AddComponent<Rigidbody>().mass = 2f;
            root.AddComponent<CarryableItem>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                Debug.LogError($"Bonsai setup: could not save prefab to {PrefabPath}.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            GameObject spawned = null;
            if (scene.isLoaded)
            {
                spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (spawned != null)
                {
                    spawned.transform.position = SpawnPosition;
                    Selection.activeGameObject = spawned;
                }
            }

            AssetDatabase.SaveAssets();
            var message = $"Bonsai prefab created at {PrefabPath}";
            message += spawned != null
                ? $" and placed at {SpawnPosition} in '{scene.name}'."
                : ". Open an Era scene and run the menu again to place an instance.";
            message += " Wire its CarryableItem target slot in the inspector if it should snap into a slot.";
            Debug.Log(message);
        }

        private static Mesh LoadMesh()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (asset is Mesh mesh)
                {
                    return mesh;
                }
            }

            return null;
        }
    }
}
