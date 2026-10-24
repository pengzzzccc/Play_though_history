using System.Reflection;
using NUnit.Framework;

namespace UnknownTechnology.Tests
{
    // UT-01: Game.SetPhase / TryPause / TryResume follow the phase state diagram.
    public class GamePhaseTests
    {
        private static readonly PropertyInfo PhaseProperty =
            typeof(Game).GetProperty(nameof(Game.Phase), BindingFlags.Public | BindingFlags.Static);
        private static readonly PropertyInfo ResumePhaseProperty =
            typeof(Game).GetProperty(nameof(Game.ResumePhase), BindingFlags.Public | BindingFlags.Static);

        [TearDown]
        public void TearDown()
        {
            ForcePhase(GamePhase.Boot, GamePhase.Boot);
        }

        [TestCase(GamePhase.Boot, GamePhase.Boot, true)]
        [TestCase(GamePhase.Boot, GamePhase.MainMenu, true)]
        [TestCase(GamePhase.Boot, GamePhase.Loading, true)]
        [TestCase(GamePhase.Boot, GamePhase.Exploring, true)]
        [TestCase(GamePhase.Boot, GamePhase.Paused, false)]
        [TestCase(GamePhase.Boot, GamePhase.Flashback, false)]
        [TestCase(GamePhase.MainMenu, GamePhase.Boot, false)]
        [TestCase(GamePhase.MainMenu, GamePhase.MainMenu, true)]
        [TestCase(GamePhase.MainMenu, GamePhase.Loading, true)]
        [TestCase(GamePhase.MainMenu, GamePhase.Exploring, false)]
        [TestCase(GamePhase.MainMenu, GamePhase.Paused, false)]
        [TestCase(GamePhase.MainMenu, GamePhase.Flashback, false)]
        [TestCase(GamePhase.Loading, GamePhase.Boot, false)]
        [TestCase(GamePhase.Loading, GamePhase.MainMenu, true)]
        [TestCase(GamePhase.Loading, GamePhase.Loading, true)]
        [TestCase(GamePhase.Loading, GamePhase.Exploring, true)]
        [TestCase(GamePhase.Loading, GamePhase.Paused, false)]
        [TestCase(GamePhase.Loading, GamePhase.Flashback, false)]
        [TestCase(GamePhase.Exploring, GamePhase.Boot, false)]
        [TestCase(GamePhase.Exploring, GamePhase.MainMenu, false)]
        [TestCase(GamePhase.Exploring, GamePhase.Loading, true)]
        [TestCase(GamePhase.Exploring, GamePhase.Exploring, true)]
        [TestCase(GamePhase.Exploring, GamePhase.Paused, true)]
        [TestCase(GamePhase.Exploring, GamePhase.Flashback, true)]
        [TestCase(GamePhase.Paused, GamePhase.Boot, false)]
        [TestCase(GamePhase.Paused, GamePhase.MainMenu, false)]
        [TestCase(GamePhase.Paused, GamePhase.Loading, true)]
        [TestCase(GamePhase.Paused, GamePhase.Exploring, false)]
        [TestCase(GamePhase.Paused, GamePhase.Paused, true)]
        [TestCase(GamePhase.Paused, GamePhase.Flashback, false)]
        [TestCase(GamePhase.Flashback, GamePhase.Boot, false)]
        [TestCase(GamePhase.Flashback, GamePhase.MainMenu, false)]
        [TestCase(GamePhase.Flashback, GamePhase.Loading, true)]
        [TestCase(GamePhase.Flashback, GamePhase.Exploring, true)]
        [TestCase(GamePhase.Flashback, GamePhase.Paused, false)]
        [TestCase(GamePhase.Flashback, GamePhase.Flashback, true)]
        public void SetPhase_FollowsTransitionRules(GamePhase source, GamePhase target, bool allowed)
        {
            ForcePhase(source, source == GamePhase.Paused ? GamePhase.Exploring : GamePhase.Boot);

            var result = Game.SetPhase(target);

            Assert.AreEqual(allowed, result);
            Assert.AreEqual(allowed ? target : source, Game.Phase);
        }

        [Test]
        public void TryPause_FromExploring_RemembersResumePhase()
        {
            ForcePhase(GamePhase.Exploring, GamePhase.Boot);

            Assert.IsTrue(Game.TryPause());
            Assert.AreEqual(GamePhase.Paused, Game.Phase);
            Assert.AreEqual(GamePhase.Exploring, Game.ResumePhase);
        }

        [Test]
        public void TryResume_FromPaused_ReturnsToResumePhase()
        {
            ForcePhase(GamePhase.Paused, GamePhase.Exploring);

            Assert.IsTrue(Game.TryResume());
            Assert.AreEqual(GamePhase.Exploring, Game.Phase);
            Assert.AreEqual(GamePhase.Boot, Game.ResumePhase);
        }

        [TestCase(GamePhase.Boot)]
        [TestCase(GamePhase.MainMenu)]
        [TestCase(GamePhase.Loading)]
        [TestCase(GamePhase.Exploring)]
        [TestCase(GamePhase.Flashback)]
        public void TryResume_WhenNotPaused_ReturnsFalse(GamePhase source)
        {
            ForcePhase(source, GamePhase.Boot);

            Assert.IsFalse(Game.TryResume());
            Assert.AreEqual(source, Game.Phase);
        }

        [Test]
        public void SaveAndQuit_FromPaused_ClearsResumePhase()
        {
            ForcePhase(GamePhase.Paused, GamePhase.Exploring);

            Assert.IsTrue(Game.SetPhase(GamePhase.Loading));
            Assert.AreEqual(GamePhase.Loading, Game.Phase);
            Assert.AreEqual(GamePhase.Boot, Game.ResumePhase);
        }

        private static void ForcePhase(GamePhase phase, GamePhase resumePhase)
        {
            PhaseProperty.SetValue(null, phase);
            ResumePhaseProperty.SetValue(null, resumePhase);
        }
    }
}
