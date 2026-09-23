using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdLamp.EditorTools
{
    /// <summary>Editor helpers for the vertical slice. The world itself is built at runtime by SliceBootstrap.</summary>
    public static class SliceMenu
    {
        const string ScenePath = "Assets/_Project/Scenes/Slice.unity";

        [MenuItem("Tools/The Third Lamp/Open Slice Scene", priority = 0)]
        public static void OpenScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                RecreateScene();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/The Third Lamp/Recreate Slice Scene", priority = 1)]
        public static void RecreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("SliceBootstrap").AddComponent<SliceBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
        }

        static void AddSceneToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        /// <summary>
        /// The slice creates materials from code with Shader.Find, so the shaders must be in builds.
        /// </summary>
        [MenuItem("Tools/The Third Lamp/Include Runtime Shaders In Builds", priority = 20)]
        public static void IncludeShaders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            string[] names =
            {
                "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit",
                "Standard", "Unlit/Texture", "GUI/Text Shader",
            };
            foreach (var n in names)
            {
                var shader = Shader.Find(n);
                if (shader == null) continue;
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedProperties();
            Debug.Log("[ThirdLamp] Runtime shaders added to Always Included Shaders.");
        }

        [MenuItem("Tools/The Third Lamp/Delete Checkpoint Save", priority = 40)]
        public static void DeleteSave()
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "thirdlamp_checkpoint.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            Debug.Log("[ThirdLamp] Checkpoint deleted.");
        }

        [MenuItem("Tools/The Third Lamp/Report Render Pipeline", priority = 41)]
        public static void ReportPipeline()
        {
            var rp = GraphicsSettings.defaultRenderPipeline;
            Debug.Log(rp == null
                ? "[ThirdLamp] Built-in pipeline active. For URP post-processing: Create > Rendering > URP Asset (with Universal Renderer), then assign it in Project Settings > Graphics and Quality."
                : "[ThirdLamp] Render pipeline: " + rp.name);
        }
    }
}
