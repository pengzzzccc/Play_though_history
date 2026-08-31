using System.Collections;
using NUnit.Framework;
using UnknownTechnology.Bootstrap;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.State;
using UnknownTechnology.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace UnknownTechnology.Tests.PlayMode
{
    public sealed class BootstrapAndSceneFlowPlayModeTests
    {
        private const string BootstrapScenePath = "Assets/UnknownTechnology/Scenes/Bootstrap.unity";

        [UnityTest]
        public IEnumerator BootstrapToMenuToAncient_IsPlayableUniqueAndGamepadCancelIsSafe()
        {
            var existing = Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include);
            foreach (var item in existing)
            {
                Object.Destroy(item.gameObject);
            }

            yield return null;
            GameContextProvider.Reset();

            var load = SceneManager.LoadSceneAsync(BootstrapScenePath, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "Bootstrap scene is not available in Build Settings.");
            while (!load.isDone)
            {
                yield return null;
            }

            yield return WaitForSceneAndPhase("MainMenu", GamePhase.MainMenu, 600);
            AssertUniqueRuntimeObjects(expectPlayer: false);

            var context = GameContextProvider.Current;
            var rejectionCount = 0;
            using var rejection = context.EventBus.Subscribe<SceneLoadRejected>(_ => rejectionCount++);
            Assert.That(context.SceneFlow.RequestLoad(SceneFlowConfig.AncientRoute), Is.True);
            Assert.That(context.SceneFlow.RequestLoad(SceneFlowConfig.AncientRoute), Is.False, "A second load transaction was accepted.");
            Assert.That(rejectionCount, Is.EqualTo(1));

            yield return WaitForSceneAndPhase("Era_Ancient", GamePhase.Exploring, 600);
            AssertUniqueRuntimeObjects(expectPlayer: true);
            Assert.That(context.SceneFlow.RequestLoad(SceneFlowConfig.ModernRoute), Is.False);

            var deviceLostCount = 0;
            var deviceRegainedCount = 0;
            using var lostSubscription = context.EventBus.Subscribe<InputDeviceLost>(_ => deviceLostCount++);
            using var regainedSubscription = context.EventBus.Subscribe<InputDeviceRegained>(_ => deviceRegainedCount++);
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var inputRouter = context.Input as InputRouter;
            Assert.That(inputRouter, Is.Not.Null);
            inputRouter.PlayerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
            yield return null;
            Assert.That(context.Input.CurrentControlScheme, Is.EqualTo(Core.Input.ControlScheme.Gamepad));
            InputSystem.RemoveDevice(gamepad);
            yield return null;
            Assert.That(deviceLostCount, Is.EqualTo(1));
            InputSystem.AddDevice(gamepad);
            yield return null;
            Assert.That(deviceRegainedCount, Is.EqualTo(1));
            inputRouter.PlayerInput.SwitchCurrentControlScheme("Gamepad", gamepad);
            yield return null;
            Assert.That(context.Input.CurrentControlScheme, Is.EqualTo(Core.Input.ControlScheme.Gamepad));

            LogAssert.Expect(LogType.Error, "A duplicate GameBootstrap was rejected.");
            var duplicate = new GameObject("Duplicate Game Root Test");
            duplicate.SetActive(false);
            duplicate.AddComponent<GameBootstrap>();
            duplicate.SetActive(true);
            yield return null;
            Assert.That(Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include), Has.Length.EqualTo(1));

            Assert.That(context.State.TryPause(out var pauseReason), Is.True, pauseReason);
            yield return null;
            Assert.That(context.State.Current.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(context.Input.Move, Is.EqualTo(Vector2.zero));
            Assert.That(inputRouter.ActiveMapName, Is.EqualTo(InputRouter.UiMapName));

            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.East));
            yield return null;

            Assert.That(context.State.Current.Phase, Is.EqualTo(GamePhase.Exploring));
            for (var frame = 0; frame < 3 && inputRouter.ActiveMapName != InputRouter.GameplayMapName; frame++)
            {
                yield return null;
            }

            Assert.That(inputRouter.ActiveMapName, Is.EqualTo(InputRouter.GameplayMapName));
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            yield return null;
            InputSystem.RemoveDevice(gamepad);
        }

        private static IEnumerator WaitForSceneAndPhase(string sceneName, GamePhase phase, int maximumFrames)
        {
            for (var frame = 0; frame < maximumFrames; frame++)
            {
                if (SceneManager.GetActiveScene().name == sceneName &&
                    GameContextProvider.IsReady &&
                    GameContextProvider.Current.State.Current.Phase == phase)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for scene '{sceneName}' and phase '{phase}'.");
        }

        private static void AssertUniqueRuntimeObjects(bool expectPlayer)
        {
            Assert.That(Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include), Has.Length.EqualTo(1));
            var contexts = Object.FindObjectsByType<EraSceneContext>(FindObjectsInactive.Include);
            Assert.That(contexts, expectPlayer ? Has.Length.EqualTo(1) : Has.Length.EqualTo(0));
            if (expectPlayer)
            {
                Assert.That(contexts[0].IsValid, Is.True);
            }
        }
    }
}
