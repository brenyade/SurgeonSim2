using System.Collections.Generic;
using System.IO;
using TraumaSurgeon.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TraumaSurgeon.EditorTools
{
    /// <summary>
    /// Editor-side project setup and health checks. Runs once on load to make sure the Bootstrap
    /// scene is in the build list, and warns if the project is configured for an input backend the
    /// game does not use.
    /// </summary>
    [InitializeOnLoad]
    public static class TraumaSurgeonSetup
    {
        public const string BootstrapScenePath = "Assets/TraumaSurgeon/Scenes/Bootstrap.unity";
        private const string SetupDoneKey = "TraumaSurgeon.SetupChecked";

        static TraumaSurgeonSetup()
        {
            EditorApplication.delayCall += RunChecks;
        }

        /// <summary>
        /// Shaders resolved at runtime with Shader.Find are stripped from player builds unless
        /// something references them, so every shader the runtime material library can pick has to
        /// be in Graphics Settings > Always Included Shaders.
        /// </summary>
        private static readonly string[] RequiredShaders =
        {
            "Standard",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Legacy Shaders/Diffuse",
            "Sprites/Default",
            "UI/Default"
        };

        private static void RunChecks()
        {
            EnsureBuildSettings();

            if (SessionState.GetBool(SetupDoneKey, false))
            {
                return;
            }

            SessionState.SetBool(SetupDoneKey, true);
            CheckInputBackend();
            EnsureShadersIncluded();
        }

        /// <summary>Adds the runtime shaders to Always Included Shaders if they are missing.</summary>
        public static void EnsureShadersIncluded()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                return;
            }

            var so = new SerializedObject(assets[0]);
            SerializedProperty list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null || !list.isArray)
            {
                return;
            }

            var existing = new HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var shader = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (shader != null)
                {
                    existing.Add(shader.name);
                }
            }

            bool changed = false;
            foreach (string name in RequiredShaders)
            {
                Shader shader = Shader.Find(name);
                if (shader == null || existing.Contains(shader.name))
                {
                    continue;
                }

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                existing.Add(shader.name);
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Trauma Surgeon] Added runtime shaders to Always Included Shaders " +
                          "so they survive player builds.");
            }
        }

        [MenuItem("Trauma Surgeon/Open Bootstrap Scene", priority = 0)]
        public static void OpenBootstrapScene()
        {
            if (!File.Exists(BootstrapScenePath))
            {
                Debug.LogError($"[Trauma Surgeon] Bootstrap scene missing at {BootstrapScenePath}");
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(BootstrapScenePath);
        }

        [MenuItem("Trauma Surgeon/Fix Project Setup", priority = 20)]
        public static void FixProjectSetup()
        {
            EnsureBuildSettings();
            EnsureShadersIncluded();
            SetLegacyInputBackend();
            Debug.Log("[Trauma Surgeon] Project setup checked. Build settings and shaders updated.");
        }

        /// <summary>Adds the Bootstrap scene to the build list as scene 0 if it is missing.</summary>
        public static void EnsureBuildSettings()
        {
            if (!File.Exists(BootstrapScenePath))
            {
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool present = scenes.Exists(s => s.path == BootstrapScenePath);

            if (present && scenes.Count > 0 && scenes[0].path == BootstrapScenePath && scenes[0].enabled)
            {
                return;
            }

            scenes.RemoveAll(s => s.path == BootstrapScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(BootstrapScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Trauma Surgeon] Added Bootstrap.unity to the build settings.");
        }

        /// <summary>
        /// The game polls the classic Input class, so the project needs "Input Manager (Old)" or
        /// "Both" as its active input handling. Warn rather than silently forcing an editor restart.
        /// </summary>
        private static void CheckInputBackend()
        {
            int handler = GetActiveInputHandler();
            if (handler == 1)
            {
                Debug.LogWarning(
                    "[Trauma Surgeon] Active Input Handling is set to 'Input System Package (New)'. " +
                    "This project uses the classic Input Manager. " +
                    "Use the menu item 'Trauma Surgeon/Fix Project Setup' or set " +
                    "Project Settings > Player > Active Input Handling to 'Both'.");
            }
        }

        private static int GetActiveInputHandler()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0)
            {
                return 0;
            }

            var so = new SerializedObject(settings[0]);
            SerializedProperty property = so.FindProperty("activeInputHandler");
            return property != null ? property.intValue : 0;
        }

        private static void SetLegacyInputBackend()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0)
            {
                return;
            }

            var so = new SerializedObject(settings[0]);
            SerializedProperty property = so.FindProperty("activeInputHandler");
            if (property == null || property.intValue == 2 || property.intValue == 0)
            {
                return;
            }

            property.intValue = 2;   // Both
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[Trauma Surgeon] Active Input Handling set to 'Both'. " +
                             "Unity will ask to restart the editor - accept it.");
        }

        [MenuItem("Trauma Surgeon/Play From Bootstrap", priority = 1)]
        public static void PlayFromBootstrap()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            OpenBootstrapScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Trauma Surgeon/Open Save Folder", priority = 40)]
        public static void OpenSaveFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "TraumaSurgeon");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Trauma Surgeon/Delete All Saves", priority = 41)]
        public static void DeleteAllSaves()
        {
            string path = Path.Combine(Application.persistentDataPath, "TraumaSurgeon");
            if (!Directory.Exists(path))
            {
                Debug.Log("[Trauma Surgeon] No save folder to delete.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete all saves?",
                    $"This permanently deletes every career and settings file in:\n{path}",
                    "Delete", "Cancel"))
            {
                return;
            }

            Directory.Delete(path, true);
            Debug.Log("[Trauma Surgeon] Save folder deleted.");
        }
    }
}
