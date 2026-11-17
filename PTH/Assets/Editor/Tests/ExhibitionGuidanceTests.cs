using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownTechnology.Tests
{
    public class ExhibitionGuidanceTests
    {
        private Scene scene;
        private ExhibitionGuidance guidance;
        private PapyrusWallPuzzle puzzle;
        private CanvasQuizUI quiz;
        private GamePhase previousPhase;
        private bool previousQuizOpen;

        [SetUp]
        public void SetUp()
        {
            previousPhase = Game.Phase;
            previousQuizOpen = CanvasQuizUI.IsQuizOpen;
            scene = SceneManager.CreateScene("Guidance test " + Guid.NewGuid());
            puzzle = CreateObject("Puzzle").AddComponent<PapyrusWallPuzzle>();
            quiz = CreateObject("Quiz").AddComponent<CanvasQuizUI>();
            SetField(puzzle, "state", new PapyrusPuzzleState(new[] { "answer" }));
            guidance = CreateObject("Room guidance").AddComponent<ExhibitionGuidance>();
        }

        [TearDown]
        public void TearDown()
        {
            Invoke(guidance, "OnDisable");
            EditorSceneManager.CloseScene(scene, true);
            SetProperty(typeof(Game), nameof(Game.Phase), previousPhase);
            SetProperty(typeof(CanvasQuizUI), nameof(CanvasQuizUI.IsQuizOpen), previousQuizOpen);
        }

        [Test]
        public void MissingReferences_ResolveInactiveObjectsInTheSameScene()
        {
            // Reproduce an open scene whose serialized references are still empty.
            Assert.IsNull(GetField(guidance, "puzzle"));
            Assert.IsNull(GetField(guidance, "quizUI"));
            Invoke(guidance, "OnEnable");

            Assert.AreSame(puzzle, GetField(guidance, "puzzle"));
            Assert.AreSame(quiz, GetField(guidance, "quizUI"));
        }

        [Test]
        public void RoomEntryThenIntroduction_QueueTheNextPromptWhileDialogueIsOpen()
        {
            Invoke(guidance, "OnEnable");
            var player = CreateObject("Player").AddComponent<FirstPersonPlayer>();
            Invoke(guidance, "OnTriggerEnter", player.GetComponent<CharacterController>());
            Assert.AreEqual("Interact with Jimmy", PromptText());

            SetProperty(typeof(Game), nameof(Game.Phase), GamePhase.Exploring);
            SetProperty(typeof(CanvasQuizUI), nameof(CanvasQuizUI.IsQuizOpen), true);
            ((Action)GetField(quiz, "IntroductionFinished"))();
            Assert.AreEqual("Interact with Artefact", PromptText());
            var prompt = GetField(guidance, "guidance");
            Invoke(guidance, "Update");
            Assert.AreEqual(3f, prompt.GetType().GetProperty("Remaining").GetValue(prompt));
            Assert.IsFalse((bool)GetProperty(guidance, "CanShowGuidance"));

            SetProperty(typeof(CanvasQuizUI), nameof(CanvasQuizUI.IsQuizOpen), false);
            Assert.IsTrue((bool)GetProperty(guidance, "CanShowGuidance"));
        }

        [Test]
        public void PuzzleCompletion_WaitsForCheckFeedbackBeforePromptingJimmy()
        {
            Invoke(guidance, "OnEnable");
            var player = CreateObject("Player").AddComponent<FirstPersonPlayer>();
            Invoke(guidance, "OnTriggerEnter", player.GetComponent<CharacterController>());
            var state = (PapyrusPuzzleState)GetField(puzzle, "state");
            state.Unlock();
            state.TryStart();
            state.TryPlace(0, "answer");
            state.CheckAnswers();
            SetField(puzzle, "checkingAnswers", true);
            Invoke(guidance, "Update");
            Assert.IsFalse((bool)GetField(guidance, "completionGuidanceShown"));

            SetField(puzzle, "checkingAnswers", false);
            Invoke(guidance, "Update");
            Assert.IsTrue((bool)GetField(guidance, "completionGuidanceShown"));
            Assert.AreEqual("Interact with Jimmy", PromptText());
        }

        private GameObject CreateObject(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        private string PromptText() => (string)GetField(GetField(guidance, "guidance"), "text");
        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void SetProperty(Type type, string name, object value) =>
            type.GetProperty(name, BindingFlags.Public | BindingFlags.Static).SetValue(null, value);
        private static void Invoke(object target, string name, params object[] args) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
