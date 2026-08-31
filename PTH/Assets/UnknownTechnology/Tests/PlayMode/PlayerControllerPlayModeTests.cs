using System.Collections;
using NUnit.Framework;
using UnknownTechnology.Core.Input;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Gameplay.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnknownTechnology.Tests.PlayMode
{
    public sealed class PlayerControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator CharacterController_MovesGroundsCollidesAndStopsWhenLocked()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test Floor";
            floor.transform.SetPositionAndRotation(new Vector3(0f, -0.25f, 2f), Quaternion.identity);
            floor.transform.localScale = new Vector3(10f, 0.5f, 10f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Test Wall";
            wall.transform.position = new Vector3(0f, 1f, 2f);
            wall.transform.localScale = new Vector3(5f, 2f, 0.4f);

            var player = new GameObject("Test Player");
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            var motor = player.AddComponent<PlayerMotor>();
            motor.Configure(4f);
            Physics.SyncTransforms();
            yield return null;

            for (var frame = 0; frame < 90; frame++)
            {
                motor.Tick(Vector2.up, false, true, 1f / 60f);
                yield return null;
            }

            Assert.That(player.transform.position.z, Is.GreaterThan(0.5f));
            Assert.That(player.transform.position.z, Is.LessThan(1.6f), "The CharacterController passed through the wall.");
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.1f), "The player fell through the floor.");
            var lockedPosition = player.transform.position;

            for (var frame = 0; frame < 10; frame++)
            {
                motor.Tick(Vector2.up, false, false, 1f / 60f);
                yield return null;
            }

            Assert.That(Vector2.Distance(
                new Vector2(lockedPosition.x, lockedPosition.z),
                new Vector2(player.transform.position.x, player.transform.position.z)), Is.LessThan(0.01f));
            Assert.That(motor.NormalizedSpeed, Is.Zero);

            Object.Destroy(player);
            Object.Destroy(wall);
            Object.Destroy(floor);
        }

        [UnityTest]
        public IEnumerator CharacterController_JumpsOnceLandsAndRejectsLockedJump()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Jump Test Floor";
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.5f, 8f);

            var player = new GameObject("Jump Test Player");
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            var motor = player.AddComponent<PlayerMotor>();
            motor.Configure(4f, 1.2f);
            Physics.SyncTransforms();

            for (var frame = 0; frame < 5; frame++)
            {
                motor.Tick(Vector2.zero, false, true, 1f / 60f);
                yield return null;
            }

            var groundedHeight = player.transform.position.y;
            motor.Tick(Vector2.zero, true, true, 1f / 60f);
            var maximumHeight = player.transform.position.y;
            for (var frame = 0; frame < 120; frame++)
            {
                var secondRequestWhileAirborne = frame == 15;
                motor.Tick(Vector2.zero, secondRequestWhileAirborne, true, 1f / 60f);
                maximumHeight = Mathf.Max(maximumHeight, player.transform.position.y);
                yield return null;
            }

            Assert.That(maximumHeight - groundedHeight, Is.GreaterThan(0.8f));
            Assert.That(maximumHeight - groundedHeight, Is.LessThan(1.5f), "An airborne jump request caused a double jump.");
            Assert.That(controller.isGrounded, Is.True, "The player did not land after jumping.");

            var landedHeight = player.transform.position.y;
            motor.Tick(Vector2.zero, true, false, 1f / 60f);
            var lockedMaximumHeight = player.transform.position.y;
            for (var frame = 0; frame < 10; frame++)
            {
                motor.Tick(Vector2.zero, false, false, 1f / 60f);
                lockedMaximumHeight = Mathf.Max(lockedMaximumHeight, player.transform.position.y);
                yield return null;
            }

            Assert.That(lockedMaximumHeight - landedHeight, Is.LessThan(0.03f), "A locked player was allowed to jump.");
            Object.Destroy(player);
            Object.Destroy(floor);
        }

        [Test]
        public void FirstPersonCamera_ClampsPitchAndHonoursInvertY()
        {
            var player = new GameObject("Camera Test Player");
            var pivot = new GameObject("Pitch Pivot").transform;
            pivot.SetParent(player.transform, false);
            var controller = player.AddComponent<FirstPersonCameraController>();
            controller.Configure(player.transform, pivot);
            var normal = CreateSettings(false);
            var inverted = CreateSettings(true);

            controller.Tick(new Vector2(10f, 10000f), ControlScheme.KeyboardMouse, normal, true, 1f / 60f);
            Assert.That(controller.Pitch, Is.EqualTo(-80f));
            controller.ResetView();
            controller.Tick(new Vector2(0f, 10f), ControlScheme.KeyboardMouse, inverted, true, 1f / 60f);
            Assert.That(controller.Pitch, Is.GreaterThan(0f));
            var yawBeforeLock = player.transform.eulerAngles.y;
            controller.Tick(new Vector2(100f, 100f), ControlScheme.KeyboardMouse, inverted, false, 1f / 60f);
            Assert.That(player.transform.eulerAngles.y, Is.EqualTo(yawBeforeLock));

            Object.DestroyImmediate(player);
        }

        private static GameSettingsSnapshot CreateSettings(bool invertY)
        {
            return new GameSettingsSnapshot(
                0.12f, 150f, invertY, InteractionMode.Hold, 1f, false,
                1f, 1f, 1f, 1f, 0, true);
        }
    }
}
