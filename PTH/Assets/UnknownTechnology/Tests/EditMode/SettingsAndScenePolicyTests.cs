using NUnit.Framework;
using UnknownTechnology.Core.Events;
using UnknownTechnology.Core.SceneFlow;
using UnknownTechnology.Core.Settings;
using UnknownTechnology.Core.State;
using UnityEngine;

namespace UnknownTechnology.Tests.EditMode
{
    public sealed class SettingsAndScenePolicyTests
    {
        [Test]
        public void Defaults_DisableYAxisInversion()
        {
            Assert.That(GameSettingsSnapshot.Defaults.InvertY, Is.False);
        }

        [Test]
        public void Settings_AreClampedSavedAndPublished()
        {
            var store = new MemorySettingsStore();
            var bus = new EventBus();
            var published = 0;
            bus.Subscribe<SettingsChanged>(_ => published++);
            var service = new SettingsService(store, bus, false);

            service.Apply(new GameSettingsSnapshot(
                -10f, 999f, true, InteractionMode.Toggle, 9f, true,
                -1f, 2f, 0.5f, 0.25f, 999, false));

            Assert.That(service.Current.MouseSensitivity, Is.EqualTo(GameSettingsSnapshot.MinimumMouseSensitivity));
            Assert.That(service.Current.GamepadSensitivity, Is.EqualTo(GameSettingsSnapshot.MaximumGamepadSensitivity));
            Assert.That(service.Current.UiScale, Is.EqualTo(1.5f));
            Assert.That(service.Current.MasterVolume, Is.Zero);
            Assert.That(service.Current.MusicVolume, Is.EqualTo(1f));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(published, Is.EqualTo(1));

            var reloaded = new SettingsService(store, new EventBus(), false);
            Assert.That(reloaded.Current.InvertY, Is.True);
            Assert.That(reloaded.Current.InteractionMode, Is.EqualTo(InteractionMode.Toggle));
            Assert.That(reloaded.Current.ReducedMotion, Is.True);
        }

        [Test]
        public void PlayerPrefsSettingsStore_RoundTripsAcrossServiceInstances()
        {
            var hadExistingValue = PlayerPrefs.HasKey(PlayerPrefsSettingsStore.StorageKey);
            var existingValue = hadExistingValue ? PlayerPrefs.GetString(PlayerPrefsSettingsStore.StorageKey) : string.Empty;
            try
            {
                PlayerPrefs.DeleteKey(PlayerPrefsSettingsStore.StorageKey);
                var store = new PlayerPrefsSettingsStore();
                var expected = new GameSettingsSnapshot(
                    0.24f, 210f, true, InteractionMode.Toggle, 1.25f, true,
                    0.7f, 0.6f, 0.5f, 0.4f, 0, false);
                store.Save(expected);

                Assert.That(new PlayerPrefsSettingsStore().TryLoad(out var loaded), Is.True);
                Assert.That(loaded.MouseSensitivity, Is.EqualTo(expected.MouseSensitivity));
                Assert.That(loaded.GamepadSensitivity, Is.EqualTo(expected.GamepadSensitivity));
                Assert.That(loaded.InvertY, Is.True);
                Assert.That(loaded.InteractionMode, Is.EqualTo(InteractionMode.Toggle));
                Assert.That(loaded.ReducedMotion, Is.True);
                Assert.That(loaded.UiScale, Is.EqualTo(1.25f));
            }
            finally
            {
                if (hadExistingValue)
                {
                    PlayerPrefs.SetString(PlayerPrefsSettingsStore.StorageKey, existingValue);
                }
                else
                {
                    PlayerPrefs.DeleteKey(PlayerPrefsSettingsStore.StorageKey);
                }

                PlayerPrefs.Save();
            }
        }

        [Test]
        public void SceneAccessPolicy_OnlyAllowsMenuAndAncient()
        {
            var policy = new DefaultSceneAccessPolicy();

            Assert.That(policy.CanEnter(SceneFlowConfig.MainMenuRoute, out _), Is.True);
            Assert.That(policy.CanEnter(SceneFlowConfig.AncientRoute, out _), Is.True);
            Assert.That(policy.CanEnter(SceneFlowConfig.ModernRoute, out var modernReason), Is.False);
            Assert.That(policy.CanEnter(SceneFlowConfig.FutureRoute, out var futureReason), Is.False);
            Assert.That(modernReason, Is.Not.Empty);
            Assert.That(futureReason, Is.Not.Empty);
        }

        [Test]
        public void ContinueProvider_DoesNotInventProgress()
        {
            var provider = new NoContinueTargetProvider();

            Assert.That(provider.TryGetContinueRoute(out var routeId), Is.False);
            Assert.That(routeId, Is.Empty);
        }

        [Test]
        public void SceneConfig_UsesStableRouteIdsAndPaths()
        {
            var config = ScriptableObject.CreateInstance<SceneFlowConfig>();
            config.Configure(new[]
            {
                new SceneRoute(SceneFlowConfig.AncientRoute, "Assets/UnknownTechnology/Scenes/Era_Ancient.unity", GamePhase.Exploring, true)
            });

            Assert.That(config.TryGetRoute(SceneFlowConfig.AncientRoute, out var route), Is.True);
            Assert.That(route.ScenePath, Does.EndWith("Era_Ancient.unity"));
            Assert.That(route.TargetPhase, Is.EqualTo(GamePhase.Exploring));
            Assert.That(route.RequiresAccess, Is.True);
            Assert.That(config.TryGetRoute("unknown", out _), Is.False);
            Object.DestroyImmediate(config);
        }

        private sealed class MemorySettingsStore : ISettingsStore
        {
            private GameSettingsSnapshot settings;
            private bool hasValue;

            public int SaveCount { get; private set; }

            public bool TryLoad(out GameSettingsSnapshot loaded)
            {
                loaded = settings;
                return hasValue;
            }

            public void Save(GameSettingsSnapshot saved)
            {
                settings = saved;
                hasValue = true;
                SaveCount++;
            }
        }
    }
}
