using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownTechnology
{
    /// <summary>
    /// Invisible trigger volume that sends the player to another level.
    /// Drop an instance into a scene, drag the target .unity scene into the
    /// Target Scene field and walk into the trigger: the screen fades out, the
    /// target scene loads, the player starts at that scene's PlayerSpawn and
    /// the screen fades back in.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class LevelTransition : MonoBehaviour
    {
#if UNITY_EDITOR
        [Tooltip("Scene to load when the player enters this trigger. Drag a .unity asset here.")]
        [SerializeField] private SceneAsset targetScene;
#endif
        [SerializeField, HideInInspector] private string targetSceneName;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;

        public string TargetSceneName => targetSceneName;
        public float FadeOutDuration => fadeOutDuration;
        public float FadeInDuration => fadeInDuration;

        private void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 2.5f, 0.6f);
            box.center = new Vector3(0f, 1.25f, 0f);

            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<FirstPersonPlayer>() == null)
            {
                return;
            }

            if (Game.Phase != GamePhase.Exploring || CanvasQuizUI.IsQuizOpen || SceneTransition.IsTransitioning)
            {
                return;
            }

            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("LevelTransition: no target scene is configured. Drag a .unity scene into the Target Scene field.", this);
                return;
            }

            if (!SceneTransition.RequestTransition(targetSceneName, fadeOutDuration, fadeInDuration))
            {
                Debug.LogWarning("LevelTransition: could not start a transition to '" + targetSceneName + "'.", this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            targetSceneName = targetScene != null ? targetScene.name : string.Empty;

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = true;
            }

            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            if (targetScene == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(targetScene);
            if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
            {
                Debug.LogWarning("LevelTransition: scene '" + path + "' is not in Build Settings, so it cannot be loaded at runtime.", this);
            }
        }
#endif

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
