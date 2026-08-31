using System.Reflection;
using NUnit.Framework;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnknownTechnology.Gameplay.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownTechnology.Tests.EditMode
{
    public sealed class InputRouterTests : InputTestFixture
    {
        private const string AssetPath = "Assets/UnknownTechnology/Input/UnknownTechnologyActions.asset";

        [TearDown]
        public void DestroyTestRouters()
        {
            foreach (var router in Object.FindObjectsByType<InputRouter>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(router.gameObject);
            }
        }

        [Test]
        public void ProductionAsset_HasRequiredMapsActionsAndSchemes()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, "Run Unknown Technology/Build Playable Prototype before tests.");

            AssertActions(asset, InputRouter.GameplayMapName, "Move", "Look", "Interact", "Jump", "Tool", "Notebook", "Pause");
            AssertActions(asset, InputRouter.RestorationMapName, "MovePiece", "RotatePiece", "SelectPlace", "CyclePiece", "Hint", "Cancel", "Pause");
            AssertActions(asset, InputRouter.UiMapName, "Navigate", "Submit", "Cancel", "Point", "Click", "Scroll");
            Assert.That(asset.controlSchemes, Has.Count.EqualTo(2));
            Assert.That(asset.FindControlSchemeIndex("Keyboard&Mouse"), Is.GreaterThanOrEqualTo(0));
            Assert.That(asset.FindControlSchemeIndex("Gamepad"), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ProductionBindings_ReadKeyboardMouseAndGamepad_WithDeadzone()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(source, Is.Not.Null, "The production input asset has not been generated.");
            var clonedAsset = Object.Instantiate(source);
            var gameplay = clonedAsset.FindActionMap(InputRouter.GameplayMapName, true);
            var move = gameplay.FindAction("Move", true);
            var jump = gameplay.FindAction("Jump", true);
            var interact = gameplay.FindAction("Interact", true);

            clonedAsset.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            gameplay.Enable();
            Press(keyboard.wKey);
            InputSystem.Update();
            Assert.That(move.ReadValue<Vector2>().y, Is.GreaterThan(0.9f));
            Release(keyboard.wKey);
            InputSystem.Update();

            gameplay.Disable();
            clonedAsset.bindingMask = InputBinding.MaskByGroup("Gamepad");
            gameplay.Enable();
            Set(gamepad.leftStick, new Vector2(0.05f, 0.05f));
            InputSystem.Update();
            Assert.That(move.ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
            Set(gamepad.leftStick, new Vector2(0.6f, 0.25f));
            InputSystem.Update();
            Assert.That(move.ReadValue<Vector2>().x, Is.GreaterThan(0.4f));

            var jumpCount = 0;
            var interactCount = 0;
            jump.performed += _ => jumpCount++;
            interact.performed += _ => interactCount++;
            PressAndRelease(gamepad.buttonSouth);
            Assert.That(jumpCount, Is.EqualTo(1));
            Assert.That(interactCount, Is.Zero, "Jump and Interact share the gamepad South binding.");
            PressAndRelease(gamepad.buttonWest);
            Assert.That(interactCount, Is.EqualTo(1));

            gameplay.Disable();
            Object.DestroyImmediate(clonedAsset);
        }

        [Test]
        public void PhaseChanges_EnableExactlyOneExpectedMapAndPauseHasNoMove()
        {
            var root = CreateRouter(out var router, out var state, out var playerInput, out var clonedAsset);

            Assert.That(state.TryTransition(GamePhase.Exploring, out var reason), Is.True, reason);
            FlushDeferredPhase(router);
            Assert.That(router.ActiveMapName, Is.EqualTo(InputRouter.GameplayMapName));
            Assert.That(EnabledMapCount(playerInput.actions), Is.EqualTo(1));

            Assert.That(state.TryTransition(GamePhase.Restoration, out reason), Is.True, reason);
            FlushDeferredPhase(router);
            Assert.That(router.ActiveMapName, Is.EqualTo(InputRouter.RestorationMapName));
            Assert.That(EnabledMapCount(playerInput.actions), Is.EqualTo(1));

            Assert.That(state.TryPause(out reason), Is.True, reason);
            FlushDeferredPhase(router);
            Assert.That(router.ActiveMapName, Is.EqualTo(InputRouter.UiMapName));
            Assert.That(router.Move, Is.EqualTo(Vector2.zero));
            Assert.That(EnabledMapCount(playerInput.actions), Is.EqualTo(1));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clonedAsset);
        }

        [TestCase(GamePhase.Boot, "")]
        [TestCase(GamePhase.MainMenu, InputRouter.UiMapName)]
        [TestCase(GamePhase.Loading, "")]
        [TestCase(GamePhase.Exploring, InputRouter.GameplayMapName)]
        [TestCase(GamePhase.Dialogue, InputRouter.UiMapName)]
        [TestCase(GamePhase.Restoration, InputRouter.RestorationMapName)]
        [TestCase(GamePhase.Quiz, InputRouter.UiMapName)]
        [TestCase(GamePhase.Paused, InputRouter.UiMapName)]
        [TestCase(GamePhase.Completed, InputRouter.UiMapName)]
        [TestCase(GamePhase.FatalError, "")]
        public void EveryPhase_SelectsExactlyItsExpectedActionMap(GamePhase phase, string expectedMap)
        {
            var root = CreateRouter(out var router, out _, out var playerInput, out var clonedAsset);

            router.ApplyPhase(phase);

            Assert.That(router.ActiveMapName, Is.EqualTo(expectedMap));
            Assert.That(EnabledMapCount(playerInput.actions), Is.EqualTo(string.IsNullOrEmpty(expectedMap) ? 0 : 1));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clonedAsset);
        }

        [Test]
        public void LowFrequencyActions_AreRaisedOncePerPress()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var root = CreateRouter(out var router, out var state, out var playerInput, out var clonedAsset);
            state.TryTransition(GamePhase.Exploring, out _);
            FlushDeferredPhase(router);
            var interactions = 0;
            var jumps = 0;
            var pauses = 0;
            router.InteractPerformed += () => interactions++;
            router.JumpPerformed += () => jumps++;
            router.PausePerformed += () => pauses++;

            PressAndRelease(keyboard.eKey);
            PressAndRelease(keyboard.spaceKey);
            PressAndRelease(keyboard.escapeKey);

            Assert.That(interactions, Is.EqualTo(1));
            Assert.That(jumps, Is.EqualTo(1));
            Assert.That(pauses, Is.EqualTo(1));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clonedAsset);
        }

        [Test]
        public void GamepadEast_WhenCancelChangesPhase_KeepsCallbackContextValidUntilFrameEnd()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var root = CreateRouter(out var router, out var state, out var playerInput, out var clonedAsset);
            Assert.That(state.TryTransition(GamePhase.Exploring, out var reason), Is.True, reason);
            FlushDeferredPhase(router);
            Assert.That(state.TryPause(out reason), Is.True, reason);
            FlushDeferredPhase(router);

            InputControl observedControl = null;
            router.CancelPerformed += () => state.TryResume(out _);
            playerInput.actions.FindAction($"{InputRouter.UiMapName}/Cancel", true).performed +=
                context => observedControl = context.control;

            PressAndRelease(gamepad.buttonEast);

            Assert.That(state.Current.Phase, Is.EqualTo(GamePhase.Exploring));
            Assert.That(observedControl, Is.SameAs(gamepad.buttonEast));
            Assert.That(router.ActiveMapName, Is.EqualTo(InputRouter.UiMapName), "The UI map changed inside its own callback.");
            FlushDeferredPhase(router);
            Assert.That(router.ActiveMapName, Is.EqualTo(InputRouter.GameplayMapName));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(clonedAsset);
        }

        private static GameObject CreateRouter(
            out InputRouter router,
            out GameStateStore state,
            out PlayerInput playerInput,
            out InputActionAsset clonedAsset)
        {
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(source, Is.Not.Null, "The production input asset has not been generated.");
            clonedAsset = Object.Instantiate(source);
            var root = new GameObject("Input Router Test");
            playerInput = root.AddComponent<PlayerInput>();
            playerInput.actions = clonedAsset;
            playerInput.defaultControlScheme = "Keyboard&Mouse";
            playerInput.neverAutoSwitchControlSchemes = false;
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            playerInput.ActivateInput();
            router = root.AddComponent<InputRouter>();
            var bus = new EventBus();
            state = new GameStateStore(bus);
            router.Initialize(bus, state, new UnknownTechnology.Core.Settings.SettingsService(new MemorySettingsStore(), bus, false));
            return root;
        }

        private static void FlushDeferredPhase(InputRouter router)
        {
            var lateUpdate = typeof(InputRouter).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null);
            lateUpdate.Invoke(router, null);
        }

        private static int EnabledMapCount(InputActionAsset asset)
        {
            var count = 0;
            foreach (var map in asset.actionMaps)
            {
                if (map.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static void AssertActions(InputActionAsset asset, string mapName, params string[] actions)
        {
            var map = asset.FindActionMap(mapName);
            Assert.That(map, Is.Not.Null, $"Missing action map {mapName}.");
            foreach (var action in actions)
            {
                Assert.That(map.FindAction(action), Is.Not.Null, $"Missing action {mapName}/{action}.");
            }
        }

        private sealed class MemorySettingsStore : ISettingsStore
        {
            public bool TryLoad(out GameSettingsSnapshot settings)
            {
                settings = default;
                return false;
            }

            public void Save(GameSettingsSnapshot settings)
            {
            }
        }
    }
}
