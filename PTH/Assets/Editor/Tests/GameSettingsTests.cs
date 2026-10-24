using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UnknownTechnology.Tests
{
    // UT-02: GameSettings clamps every value into range and falls back to
    // defaults for an unusable file. Load tests use the real Data/settings.json,
    // which is backed up and restored around each test.
    public class GameSettingsTests
    {
        private static readonly MethodInfo SanitizeMethod =
            typeof(GameSettings).GetMethod("Sanitize", BindingFlags.NonPublic | BindingFlags.Instance);

        private string originalSettingsJson;
        private bool hadSettingsFile;
        private int originalQualityLevel;

        [SetUp]
        public void SetUp()
        {
            hadSettingsFile = File.Exists(GameData.SettingsPath);
            originalSettingsJson = hadSettingsFile ? File.ReadAllText(GameData.SettingsPath) : null;
            originalQualityLevel = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void TearDown()
        {
            if (hadSettingsFile)
            {
                File.WriteAllText(GameData.SettingsPath, originalSettingsJson);
            }
            else if (File.Exists(GameData.SettingsPath))
            {
                File.Delete(GameData.SettingsPath);
            }

            QualitySettings.SetQualityLevel(originalQualityLevel, true);
        }

        [TestCase(5f, 1f)]
        [TestCase(-1f, 0.02f)]
        [TestCase(0.02f, 0.02f)]
        [TestCase(1f, 1f)]
        [TestCase(0.5f, 0.5f)]
        public void Sanitize_ClampsMouseSensitivity(float input, float expected)
        {
            var settings = new GameSettings { mouseSensitivity = input };
            Sanitize(settings);
            Assert.AreEqual(expected, settings.mouseSensitivity, 0.0001f);
        }

        [TestCase(0f, 30f)]
        [TestCase(1000f, 360f)]
        [TestCase(30f, 30f)]
        [TestCase(360f, 360f)]
        public void Sanitize_ClampsGamepadSensitivity(float input, float expected)
        {
            var settings = new GameSettings { gamepadSensitivity = input };
            Sanitize(settings);
            Assert.AreEqual(expected, settings.gamepadSensitivity, 0.0001f);
        }

        [TestCase(0.5f, 1f)]
        [TestCase(2f, 1.5f)]
        [TestCase(1.25f, 1.25f)]
        public void Sanitize_ClampsUiScale(float input, float expected)
        {
            var settings = new GameSettings { uiScale = input };
            Sanitize(settings);
            Assert.AreEqual(expected, settings.uiScale, 0.0001f);
        }

        [TestCase(-1f, 0f)]
        [TestCase(2f, 1f)]
        [TestCase(0.5f, 0.5f)]
        public void Sanitize_ClampsAllVolumes(float input, float expected)
        {
            var settings = new GameSettings
            {
                masterVolume = input,
                musicVolume = input,
                sfxVolume = input,
                uiVolume = input
            };
            Sanitize(settings);
            Assert.AreEqual(expected, settings.masterVolume, 0.0001f);
            Assert.AreEqual(expected, settings.musicVolume, 0.0001f);
            Assert.AreEqual(expected, settings.sfxVolume, 0.0001f);
            Assert.AreEqual(expected, settings.uiVolume, 0.0001f);
        }

        [Test]
        public void Sanitize_ClampsQualityLevelToHighestLevel()
        {
            var settings = new GameSettings { qualityLevel = 999 };
            Sanitize(settings);
            Assert.AreEqual(Mathf.Max(0, QualitySettings.names.Length - 1), settings.qualityLevel);
        }

        [Test]
        public void Sanitize_NegativeQualityLevelUsesCurrentLevel()
        {
            var settings = new GameSettings { qualityLevel = -1 };
            Sanitize(settings);
            Assert.AreEqual(QualitySettings.GetQualityLevel(), settings.qualityLevel);
        }

        [Test]
        public void Load_OutOfRangeFile_IsClamped()
        {
            WriteSettings("{\"version\":2,\"mouseSensitivity\":5,\"gamepadSensitivity\":1000,\"uiScale\":0.5,\"masterVolume\":-1,\"musicVolume\":2}");

            var settings = GameSettings.Load();

            Assert.AreEqual(1f, settings.mouseSensitivity, 0.0001f);
            Assert.AreEqual(360f, settings.gamepadSensitivity, 0.0001f);
            Assert.AreEqual(1f, settings.uiScale, 0.0001f);
            Assert.AreEqual(0f, settings.masterVolume, 0.0001f);
            Assert.AreEqual(1f, settings.musicVolume, 0.0001f);
        }

        [Test]
        public void Load_WrongVersion_ReturnsDefaults()
        {
            WriteSettings("{\"version\":1,\"mouseSensitivity\":0.5,\"gamepadSensitivity\":200,\"musicVolume\":0.1}");

            AssertDefaults(GameSettings.Load());
        }

        [Test]
        public void Load_InvalidJson_ReturnsDefaults()
        {
            WriteSettings("{ this is not json");

            AssertDefaults(GameSettings.Load());
        }

        private static void Sanitize(GameSettings settings)
        {
            SanitizeMethod.Invoke(settings, null);
        }

        private static void WriteSettings(string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GameData.SettingsPath));
            File.WriteAllText(GameData.SettingsPath, json);
        }

        private static void AssertDefaults(GameSettings settings)
        {
            Assert.AreEqual(GameSettings.SchemaVersion, settings.version);
            Assert.AreEqual(0.12f, settings.mouseSensitivity, 0.0001f);
            Assert.AreEqual(150f, settings.gamepadSensitivity, 0.0001f);
            Assert.AreEqual(1f, settings.uiScale, 0.0001f);
            Assert.AreEqual(1f, settings.masterVolume, 0.0001f);
            Assert.AreEqual(0.8f, settings.musicVolume, 0.0001f);
            Assert.AreEqual(1f, settings.sfxVolume, 0.0001f);
            Assert.AreEqual(1f, settings.uiVolume, 0.0001f);
            Assert.IsFalse(settings.invertY);
            Assert.IsFalse(settings.reducedMotion);
        }
    }
}
