#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalLogicLab.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Digital Logic Lab/Setup Project", false, 1)]
        public static void SetupProject()
        {
            Debug.Log("[DigitalLogicLab] Setting up project folders and main scene...");

            // 1. Ensure required folders exist
            string[] folders = new string[]
            {
                "Assets/Scenes",
                "Assets/Scripts",
                "Assets/Scripts/Core",
                "Assets/Scripts/UI",
                "Assets/Scripts/Lab",
                "Assets/Scripts/Logic",
                "Assets/Scripts/Quiz",
                "Assets/Scripts/Theory",
                "Assets/Scripts/Input",
                "Assets/Editor",
                "Assets/Materials",
                "Assets/Prefabs",
                "Assets/UI",
                "Assets/Textures"
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            AssetDatabase.Refresh();

            // 2. Import TextMesh Pro essentials if needed
            ImportTMPEssentials();

            // 3. Create and Save Assets/Scenes/Main.unity
            string scenePath = "Assets/Scenes/Main.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add basic main camera and directional light to the scene
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.14f); // Deep navy lab ambience
            camObj.transform.position = new Vector3(0f, 1.6f, -3.8f);
            camObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.95f, 0.98f, 1f);
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Add runtime bootstrap anchor
            GameObject bootstrapObj = new GameObject("AppBootstrap");
            bootstrapObj.AddComponent<DigitalLogicLab.Core.AppBootstrap>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[DigitalLogicLab] Saved scene to {scenePath}");

            // 4. Update EditorBuildSettings
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            bool sceneAlreadyInBuild = false;
            foreach (var s in currentScenes)
            {
                if (s.path == scenePath)
                {
                    sceneAlreadyInBuild = true;
                    break;
                }
            }

            if (!sceneAlreadyInBuild)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
                for (int i = 0; i < currentScenes.Length; i++)
                {
                    newScenes[i] = currentScenes[i];
                }
                newScenes[currentScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[DigitalLogicLab] Added {scenePath} to Build Settings.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DigitalLogicLab] Setup completed successfully!");
        }

        private static void ImportTMPEssentials()
        {
            try
            {
                // In Unity 2022+ / Unity 6, TMP Essential Resources is bundled in com.unity.ugui
                string packagePath = Path.Combine(EditorApplication.applicationContentsPath, "Resources", "PackageManager", "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(packagePath))
                {
                    Debug.Log($"[DigitalLogicLab] Importing TMP Essential Resources from {packagePath}...");
                    AssetDatabase.ImportPackage(packagePath, false);
                    return;
                }

                // Fallback search in EditorData
                string searchDir = Path.Combine(EditorApplication.applicationContentsPath, "Resources");
                if (Directory.Exists(searchDir))
                {
                    var foundFiles = Directory.GetFiles(searchDir, "*TMP Essential Resources*.unitypackage", SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        Debug.Log($"[DigitalLogicLab] Importing TMP package from {foundFiles[0]}...");
                        AssetDatabase.ImportPackage(foundFiles[0], false);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DigitalLogicLab] TMP essentials import note: " + ex.Message);
            }
        }
    }
}
#endif
