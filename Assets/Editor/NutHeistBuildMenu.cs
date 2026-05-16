#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NutHeist.EditorTools
{
    public static class NutHeistBuildMenu
    {
        [MenuItem("Nut Heist/Builds/Mac Standalone (.app)", priority = 20)]
        public static void BuildMac()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                scenes = new[] { "Assets/Scenes/MainLevel.unity" };
                EditorUtility.DisplayDialog("Nut Heist", "No scenes in Build Settings. Run Nut Heist ▸ Full Project Setup first.", "OK");
                return;
            }

            string path = "Builds/Mac/NutHeist.app";
            EnsureParent(path);
            BuildPipeline.BuildPlayer(scenes, path, BuildTarget.StandaloneOSX, BuildOptions.None);
        }

        [MenuItem("Nut Heist/Builds/Windows Standalone (.exe)", priority = 21)]
        public static void BuildWin()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                scenes = new[] { "Assets/Scenes/MainLevel.unity" };
                EditorUtility.DisplayDialog("Nut Heist", "No scenes in Build Settings. Run Nut Heist ▸ Full Project Setup first.", "OK");
                return;
            }

            string path = "Builds/Win/NutHeist.exe";
            EnsureParent(path);
            BuildPipeline.BuildPlayer(scenes, path, BuildTarget.StandaloneWindows64, BuildOptions.None);
        }

        static void EnsureParent(string filePath)
        {
            string dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
        }
    }
}
#endif
