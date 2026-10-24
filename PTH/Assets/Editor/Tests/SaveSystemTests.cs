using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnknownTechnology.Tests
{
    // UT-03: SaveSystem.ReadSummary returns valid saves and sets aside unusable
    // files as slot_{n}.corrupt.json. Uses slots 901-905 so real saves are untouched.
    public class SaveSystemTests
    {
        private const int MissingSlot = 901;
        private const int ValidSlot = 902;
        private const int EmptySlot = 903;
        private const int InvalidJsonSlot = 904;
        private const int WrongVersionSlot = 905;

        private static readonly int[] TestSlots = { MissingSlot, ValidSlot, EmptySlot, InvalidJsonSlot, WrongVersionSlot };

        [SetUp]
        public void SetUp()
        {
            DeleteTestFiles();
            Directory.CreateDirectory(GameData.SavesPath);
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestFiles();
        }

        [Test]
        public void ReadSummary_MissingFile_ReturnsNull()
        {
            Assert.IsNull(SaveSystem.ReadSummary(MissingSlot));
            Assert.IsFalse(File.Exists(CorruptPath(MissingSlot)));
        }

        [Test]
        public void ReadSummary_ValidFile_ReturnsData()
        {
            var save = new SaveData
            {
                slot = ValidSlot,
                currentScene = "Era_Ancient",
                playTimeSeconds = 120f
            };
            File.WriteAllText(SaveSystem.PathFor(ValidSlot), JsonUtility.ToJson(save, true));

            var loaded = SaveSystem.ReadSummary(ValidSlot);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(ValidSlot, loaded.slot);
            Assert.AreEqual("Era_Ancient", loaded.currentScene);
            Assert.AreEqual(120f, loaded.playTimeSeconds, 0.0001f);
            Assert.IsTrue(File.Exists(SaveSystem.PathFor(ValidSlot)));
        }

        [TestCase(EmptySlot, "")]
        [TestCase(InvalidJsonSlot, "{ this is not json")]
        [TestCase(WrongVersionSlot, "{\"version\":2,\"slot\":905,\"currentScene\":\"Era_Ancient\"}")]
        public void ReadSummary_UnusableFile_ReturnsNullAndSetsFileAside(int slot, string content)
        {
            File.WriteAllText(SaveSystem.PathFor(slot), content);

            Assert.IsNull(SaveSystem.ReadSummary(slot));
            Assert.IsFalse(File.Exists(SaveSystem.PathFor(slot)));
            Assert.IsTrue(File.Exists(CorruptPath(slot)));
        }

        private static string CorruptPath(int slot)
        {
            return Path.Combine(GameData.SavesPath, $"slot_{slot}.corrupt.json");
        }

        private static void DeleteTestFiles()
        {
            foreach (var slot in TestSlots)
            {
                GameData.TryDelete(SaveSystem.PathFor(slot));
                GameData.TryDelete(CorruptPath(slot));
            }
        }
    }
}
