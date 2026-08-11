using System.IO;
using SmallWorld.Core;
using SmallWorld.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Editor
{
    public static class Stage12MemorySceneGenerator
    {
        public static void GenerateFromBatchMode() => Generate();

        [MenuItem("Small World/Stage 12/Generate First Memory Scene")]
        public static void Generate()
        {
            string path = SceneCatalog.GetPath(SceneId.FirstMemory);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("First Memory Space");
            var controller = root.AddComponent<Stage12MemorySpaceController>();
            var safe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            safe.name = "Safe Zone";
            safe.transform.position = new Vector3(0f, 0.05f, 2f);
            safe.transform.localScale = new Vector3(2f, 0.05f, 2f);
            controller.GetType().GetField("safeZone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(controller, safe.transform);
            var light = new GameObject("Memory Light").AddComponent<Light>();
            light.type = LightType.Point; light.intensity = 2f; light.range = 12f; light.color = new Color(0.55f, 0.7f, 1f);
            light.transform.position = new Vector3(0f, 3f, 0f);
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(current);
            bool exists = false; foreach (var entry in list) if (entry.path == path) exists = true;
            if (!exists) list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
