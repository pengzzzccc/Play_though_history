using System;
using System.IO;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Central location for everything the game persists: settings, saves and
    /// diagnostics. The Data folder sits next to the Assets folder in the editor
    /// and next to the executable in builds, and is created on demand.
    /// </summary>
    public static class GameData
    {
        public const string FolderName = "Data";

        public static string RootPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, FolderName);
        public static string SettingsPath => Path.Combine(RootPath, "settings.json");
        public static string SavesPath => Path.Combine(RootPath, "Saves");
        public static string DiagnosticsPath => Path.Combine(RootPath, "Diagnostics");

        public static bool TryReadText(string path, out string content)
        {
            try
            {
                content = File.Exists(path) ? File.ReadAllText(path) : null;
                return content != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read '{path}': {exception.Message}");
                content = null;
                return false;
            }
        }

        public static bool TryWriteText(string path, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write '{path}': {exception.Message}");
                return false;
            }
        }
    }
}
