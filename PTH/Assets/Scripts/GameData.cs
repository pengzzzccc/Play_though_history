using System;
using System.IO;
using UnityEngine;

namespace UnknownTechnology
{
    /// <summary>
    /// Central location for everything the game persists. The game directory is
    /// the folder that holds the Assets folder in the editor and the executable
    /// in builds; it gets a Data folder (settings, diagnostics) and a Saves
    /// folder, both created on demand. WebGL has no writable game directory, so
    /// there everything goes under persistentDataPath (browser storage).
    /// </summary>
    public static class GameData
    {
        public const string FolderName = "Data";
        public const string SavesFolderName = "Saves";

        public static string GameDirectory => Application.platform == RuntimePlatform.WebGLPlayer
            ? Application.persistentDataPath
            : Directory.GetParent(Application.dataPath).FullName;

        public static string RootPath => Path.Combine(GameDirectory, FolderName);
        public static string SettingsPath => Path.Combine(RootPath, "settings.json");
        public static string SavesPath => Path.Combine(GameDirectory, SavesFolderName);
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

        public static bool TryWriteTextAtomic(string path, string content)
        {
            var temporaryPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporaryPath, content);
                if (!File.Exists(path))
                {
                    File.Move(temporaryPath, path);
                    return true;
                }

                try
                {
                    File.Replace(temporaryPath, path, null);
                }
                catch (PlatformNotSupportedException)
                {
                    File.Delete(path);
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write '{path}': {exception.Message}");
                TryDelete(temporaryPath);
                return false;
            }
        }

        public static bool TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not delete '{path}': {exception.Message}");
                return false;
            }
        }

        public static bool TryMove(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not move '{sourcePath}': {exception.Message}");
                return false;
            }
        }
    }
}
