using System.Collections.Generic;
using NUnit.Framework;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.State;

namespace UnknownTechnology.Tests.EditMode
{
    public sealed class GameStateStoreTests
    {
        [Test]
        public void LegalFlow_TransitionsFromBootToExploring()
        {
            var store = CreateStore(out _);

            Assert.That(store.TryTransition(GamePhase.Loading, out var loadingReason), Is.True, loadingReason);
            Assert.That(store.TryTransition(GamePhase.Exploring, out var exploringReason), Is.True, exploringReason);
            Assert.That(store.Current.Phase, Is.EqualTo(GamePhase.Exploring));
        }

        [Test]
        public void InvalidTransition_IsRejectedWithoutChangingState()
        {
            var store = CreateStore(out _);

            Assert.That(store.TryTransition(GamePhase.Quiz, out var reason), Is.False);
            Assert.That(reason, Does.Contain("not allowed"));
            Assert.That(store.Current.Phase, Is.EqualTo(GamePhase.Boot));
        }

        [TestCase(GamePhase.Exploring)]
        [TestCase(GamePhase.Dialogue)]
        [TestCase(GamePhase.Restoration)]
        [TestCase(GamePhase.Quiz)]
        public void PauseAndResume_ReturnsToPreviousPausablePhase(GamePhase phase)
        {
            var store = CreateStore(out _);
            ReachPhase(store, phase);

            Assert.That(store.TryPause(out var pauseReason), Is.True, pauseReason);
            Assert.That(store.Current.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(store.Current.ResumePhase, Is.EqualTo(phase));
            Assert.That(store.TryResume(out var resumeReason), Is.True, resumeReason);
            Assert.That(store.Current.Phase, Is.EqualTo(phase));
        }

        [Test]
        public void PhaseChanges_PublishPreviousAndCurrentValues()
        {
            var store = CreateStore(out var bus);
            var received = new List<GamePhaseChanged>();
            bus.Subscribe<GamePhaseChanged>(received.Add);

            store.TryTransition(GamePhase.MainMenu, out _);

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].Previous, Is.EqualTo(GamePhase.Boot));
            Assert.That(received[0].Current, Is.EqualTo(GamePhase.MainMenu));
        }

        [Test]
        public void TransitionMatrix_AcceptsOnlyDocumentedDirectTransitions()
        {
            foreach (GamePhase source in System.Enum.GetValues(typeof(GamePhase)))
            {
                foreach (GamePhase target in System.Enum.GetValues(typeof(GamePhase)))
                {
                    var store = CreateStore(out _);
                    ReachAnyPhase(store, source);

                    var accepted = store.TryTransition(target, out _);

                    Assert.That(accepted, Is.EqualTo(IsExpectedDirectTransition(source, target)),
                        $"Unexpected transition result for {source} -> {target}.");
                }
            }
        }

        private static GameStateStore CreateStore(out EventBus bus)
        {
            bus = new EventBus();
            return new GameStateStore(bus);
        }

        private static void ReachPhase(GameStateStore store, GamePhase phase)
        {
            Assert.That(store.TryTransition(GamePhase.Exploring, out var reason), Is.True, reason);
            if (phase != GamePhase.Exploring)
            {
                Assert.That(store.TryTransition(phase, out reason), Is.True, reason);
            }
        }

        private static void ReachAnyPhase(GameStateStore store, GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Boot:
                    return;
                case GamePhase.MainMenu:
                case GamePhase.Loading:
                case GamePhase.Exploring:
                case GamePhase.FatalError:
                    Assert.That(store.TryTransition(phase, out var directReason), Is.True, directReason);
                    return;
                case GamePhase.Dialogue:
                case GamePhase.Restoration:
                case GamePhase.Quiz:
                    Assert.That(store.TryTransition(GamePhase.Exploring, out var exploringReason), Is.True, exploringReason);
                    Assert.That(store.TryTransition(phase, out var gameplayReason), Is.True, gameplayReason);
                    return;
                case GamePhase.Paused:
                    Assert.That(store.TryTransition(GamePhase.Exploring, out var pauseSetupReason), Is.True, pauseSetupReason);
                    Assert.That(store.TryPause(out var pauseReason), Is.True, pauseReason);
                    return;
                case GamePhase.Completed:
                    Assert.That(store.TryTransition(GamePhase.Loading, out var loadingReason), Is.True, loadingReason);
                    Assert.That(store.TryTransition(GamePhase.Completed, out var completedReason), Is.True, completedReason);
                    return;
                default:
                    Assert.Fail($"No setup path is defined for {phase}.");
                    return;
            }
        }

        private static bool IsExpectedDirectTransition(GamePhase source, GamePhase target)
        {
            if (source == target)
            {
                return true;
            }

            if (target == GamePhase.Paused)
            {
                return source == GamePhase.Exploring || source == GamePhase.Dialogue ||
                       source == GamePhase.Restoration || source == GamePhase.Quiz;
            }

            if (source == GamePhase.Paused)
            {
                return false;
            }

            if (target == GamePhase.FatalError)
            {
                return source != GamePhase.FatalError;
            }

            return source switch
            {
                GamePhase.Boot => target == GamePhase.Loading || target == GamePhase.MainMenu || target == GamePhase.Exploring,
                GamePhase.MainMenu => target == GamePhase.Loading,
                GamePhase.Loading => target == GamePhase.MainMenu || target == GamePhase.Exploring || target == GamePhase.Completed,
                GamePhase.Exploring => target == GamePhase.Dialogue || target == GamePhase.Restoration ||
                                       target == GamePhase.Quiz || target == GamePhase.Loading || target == GamePhase.Completed,
                GamePhase.Dialogue => target == GamePhase.Exploring,
                GamePhase.Restoration => target == GamePhase.Exploring,
                GamePhase.Quiz => target == GamePhase.Exploring || target == GamePhase.Loading || target == GamePhase.Completed,
                GamePhase.Completed => target == GamePhase.Loading || target == GamePhase.MainMenu,
                GamePhase.FatalError => target == GamePhase.Loading || target == GamePhase.MainMenu,
                _ => false
            };
        }
    }
}
