using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology
{
    public class SaveRuntime : MonoBehaviour
    {
        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (Game.Phase == GamePhase.Exploring || Game.Phase == GamePhase.Flashback)
            {
                SaveSystem.AddPlayTime(Time.unscaledDeltaTime);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                SaveSystem.EndSession();
                return;
            }

            if (SaveSystem.HasSession && SaveSystem.IsSaveableScene(scene.name))
            {
                StartCoroutine(RestoreNextFrame(scene.name));
            }
        }

        private IEnumerator RestoreNextFrame(string sceneName)
        {
            yield return null;

            if (!SaveSystem.HasSession || SceneManager.GetActiveScene().name != sceneName)
            {
                yield break;
            }

            SaveSystem.RestoreActiveScene();
            SaveSystem.SaveNow();
        }
    }
}
