using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnknownTechnology
{
    // Match the build's Bootstrap -> MainMenu flow when pressing Play in any scene.
    [InitializeOnLoad]
    public static class PlayModeStartup
    {
        static PlayModeStartup()
        {
            EditorApplication.delayCall += Configure;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    Configure();
            };
        }

        private static void Configure()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorSceneManager.playModeStartScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Bootstrap.unity");
        }
    }
}
