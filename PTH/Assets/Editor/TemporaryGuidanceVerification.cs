using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnknownTechnology;

[InitializeOnLoad]
internal static class TemporaryGuidanceVerification
{
    static TemporaryGuidanceVerification() => EditorApplication.update += Run;

    private static void Run()
    {
        if (!File.Exists("Temp/run-guidance-tests") || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Run;
        File.Delete("Temp/run-guidance-tests");
        var report = "Loaded scene guidance:\n";
        foreach (var item in Resources.FindObjectsOfTypeAll<ExhibitionGuidance>())
        {
            if (!item.gameObject.scene.IsValid()) continue;
            var data = new SerializedObject(item);
            report += item.gameObject.scene.name + " position=" + item.transform.position +
                " bounds=" + item.GetComponent<BoxCollider>().bounds +
                " puzzle=" + data.FindProperty("puzzle").objectReferenceValue +
                " quiz=" + data.FindProperty("quizUI").objectReferenceValue + "\n";
        }
        File.WriteAllText("Temp/guidance-diagnostics.txt", report);
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
        api.Execute(new ExecutionSettings(new Filter {
            testMode = TestMode.EditMode,
            testNames = new[] { "UnknownTechnology.Tests.ExhibitionGuidanceTests" }
        }) { runSynchronously = true });
    }

    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result) =>
            TestRunnerApi.SaveResultToFile(result, "Temp/GuidanceTests.xml");
    }
}
