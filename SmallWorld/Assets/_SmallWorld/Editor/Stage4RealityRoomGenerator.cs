using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Core;
using SmallWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Editor
{
    public static class Stage4RealityRoomGenerator
    {
        private const string RootName = "Stage 4 Reality Room";
        private const string MaterialDirectory = "Assets/_SmallWorld/Art/Materials/Stage4";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Small World/Stage 4/Generate Reality Room")]
        public static void GenerateFromMenu()
        {
            try
            {
                GenerateAndValidate();
                Debug.Log("[SmallWorld] Stage 4 reality room generated successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void GenerateFromBatchMode()
        {
            try
            {
                GenerateAndValidate();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void GenerateAndValidate()
        {
            string scenePath = SceneCatalog.GetPath(SceneId.RealityRoom);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveOwnedContent();
            CreateMaterials();

            var root = new GameObject(RootName);
            CreateArchitecture(root.transform);
            CreateFurniture(root.transform);
            CreateLighting(root.transform);
            CreateAudioZone(root.transform);
            PreparePlayer();
            Validate(root);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
                throw new InvalidOperationException("Could not save the Stage 4 Reality Room scene.");
            AssetDatabase.SaveAssets();
        }

        private static void CreateMaterials()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_SmallWorld/Art/Materials/Stage4"));
            AssetDatabase.Refresh();
            Materials.Clear();
            CreateMaterial("WarmWall", new Color(0.77f, 0.72f, 0.65f), 0.05f, 0.15f);
            CreateMaterial("Ceiling", new Color(0.88f, 0.86f, 0.81f), 0f, 0.1f);
            CreateMaterial("DarkWood", new Color(0.16f, 0.105f, 0.075f), 0f, 0.28f);
            CreateMaterial("Wood", new Color(0.33f, 0.20f, 0.12f), 0f, 0.3f);
            CreateMaterial("Fabric", new Color(0.22f, 0.27f, 0.32f), 0f, 0.42f);
            CreateMaterial("Cream", new Color(0.78f, 0.70f, 0.58f), 0f, 0.5f);
            CreateMaterial("Metal", new Color(0.12f, 0.14f, 0.16f), 0.65f, 0.38f);
            CreateMaterial("Screen", new Color(0.025f, 0.075f, 0.085f), 0.15f, 0.7f, new Color(0.04f, 0.34f, 0.35f));
            CreateMaterial("Glass", new Color(0.22f, 0.38f, 0.45f), 0.1f, 0.75f, new Color(0.02f, 0.06f, 0.08f));
            CreateMaterial("Rug", new Color(0.36f, 0.14f, 0.12f), 0f, 0.55f);
            CreateMaterial("ModelHouse", new Color(0.55f, 0.34f, 0.20f), 0f, 0.4f);
        }

        private static void CreateMaterial(string name, Color color, float metallic, float smoothness, Color emission = default)
        {
            string path = $"{MaterialDirectory}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            Materials[name] = material;
        }

        private static void CreateArchitecture(Transform root)
        {
            Transform architecture = Group("Architecture", root);
            Block("Floor", architecture, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), "DarkWood");
            Block("Ceiling", architecture, new Vector3(0f, 3f, 0f), new Vector3(12f, 0.2f, 10f), "Ceiling");
            Block("North Wall", architecture, new Vector3(0f, 1.45f, 5f), new Vector3(12f, 3.1f, 0.2f), "WarmWall");
            Block("West Wall", architecture, new Vector3(-6f, 1.45f, 0f), new Vector3(0.2f, 3.1f, 10f), "WarmWall");
            Block("East Wall A", architecture, new Vector3(6f, 1.45f, -3.5f), new Vector3(0.2f, 3.1f, 3f), "WarmWall");
            Block("East Wall B", architecture, new Vector3(6f, 1.45f, 3.5f), new Vector3(0.2f, 3.1f, 3f), "WarmWall");
            Block("East Window Header", architecture, new Vector3(6f, 2.65f, 0f), new Vector3(0.2f, 0.7f, 4f), "WarmWall");
            Block("East Window Sill", architecture, new Vector3(6f, 0.45f, 0f), new Vector3(0.28f, 0.18f, 4f), "Cream");
            Block("South Wall A", architecture, new Vector3(-3.7f, 1.45f, -5f), new Vector3(4.6f, 3.1f, 0.2f), "WarmWall");
            Block("South Wall B", architecture, new Vector3(2.3f, 1.45f, -5f), new Vector3(7.4f, 3.1f, 0.2f), "WarmWall");
            Block("Door Header", architecture, new Vector3(-1f, 2.65f, -5f), new Vector3(1f, 0.7f, 0.2f), "WarmWall");
            Block("Door", architecture, new Vector3(-1f, 1.1f, -4.92f), new Vector3(0.9f, 2.2f, 0.12f), "Wood");
            Sphere("Door Handle", architecture, new Vector3(-0.68f, 1.05f, -4.82f), new Vector3(0.08f, 0.08f, 0.08f), "Metal");
            Block("Window Glass", architecture, new Vector3(5.94f, 1.55f, 0f), new Vector3(0.05f, 1.9f, 3.6f), "Glass");
            Block("Window Frame Vertical", architecture, new Vector3(5.88f, 1.55f, 0f), new Vector3(0.08f, 1.95f, 0.08f), "Metal");
            Block("Window Frame Horizontal", architecture, new Vector3(5.88f, 1.55f, 0f), new Vector3(0.08f, 0.08f, 3.7f), "Metal");
            CreateCorridor(architecture);
        }

        private static void CreateFurniture(Transform root)
        {
            Transform furniture = Group("Furniture", root);
            CreateBed(furniture);
            CreateDesk(furniture);
            CreateWardrobe(furniture);
            CreateBookshelf(furniture);
            CreateModelHouse(furniture);
            CreateStoryProps(furniture);
            Block("Rug", furniture, new Vector3(0.6f, 0.015f, 0.2f), new Vector3(4.2f, 0.03f, 3.2f), "Rug");
        }

        private static void CreateCorridor(Transform parent)
        {
            Transform corridor = Group("Door Corridor", parent);
            Block("Corridor Floor", corridor, new Vector3(-1f, -0.1f, -6.5f), new Vector3(1.2f, 0.2f, 3f), "DarkWood");
            Block("Corridor Ceiling", corridor, new Vector3(-1f, 3f, -6.5f), new Vector3(1.2f, 0.2f, 3f), "Ceiling");
            Block("Corridor Left Wall", corridor, new Vector3(-1.65f, 1.45f, -6.5f), new Vector3(0.1f, 3.1f, 3f), "WarmWall");
            Block("Corridor Right Wall", corridor, new Vector3(-0.35f, 1.45f, -6.5f), new Vector3(0.1f, 3.1f, 3f), "WarmWall");
            Block("Corridor End", corridor, new Vector3(-1f, 1.45f, -8f), new Vector3(1.4f, 3.1f, 0.15f), "WarmWall");
        }

        private static void CreateBed(Transform parent)
        {
            Transform bed = Group("Bed", parent);
            Block("Bed Frame", bed, new Vector3(-4.25f, 0.32f, 2.9f), new Vector3(2.6f, 0.48f, 3.8f), "Wood");
            Block("Mattress", bed, new Vector3(-4.25f, 0.65f, 2.9f), new Vector3(2.45f, 0.32f, 3.55f), "Cream");
            Block("Blanket", bed, new Vector3(-4.25f, 0.84f, 3.35f), new Vector3(2.3f, 0.10f, 2.3f), "Fabric");
            Block("Pillow", bed, new Vector3(-4.25f, 0.92f, 1.65f), new Vector3(1.45f, 0.25f, 0.65f), "Ceiling");
            Block("Headboard", bed, new Vector3(-4.25f, 1.25f, 4.72f), new Vector3(2.65f, 1.35f, 0.16f), "DarkWood");
            Block("Bedside Table", bed, new Vector3(-2.55f, 0.48f, 3.95f), new Vector3(0.65f, 0.95f, 0.7f), "Wood");
        }

        private static void CreateDesk(Transform parent)
        {
            Transform desk = Group("Computer Desk", parent);
            Block("Desk Top", desk, new Vector3(2.9f, 0.83f, 3.95f), new Vector3(3.8f, 0.16f, 1.35f), "Wood");
            Block("Desk Left Leg", desk, new Vector3(1.25f, 0.4f, 3.95f), new Vector3(0.16f, 0.8f, 1.2f), "Metal");
            Block("Desk Right Leg", desk, new Vector3(4.55f, 0.4f, 3.95f), new Vector3(0.16f, 0.8f, 1.2f), "Metal");
            Block("Monitor", desk, new Vector3(2.9f, 1.65f, 4.35f), new Vector3(1.8f, 1.05f, 0.14f), "Metal");
            Block("Monitor Screen", desk, new Vector3(2.9f, 1.65f, 4.265f), new Vector3(1.62f, 0.87f, 0.025f), "Screen");
            Block("Monitor Stand", desk, new Vector3(2.9f, 1.08f, 4.35f), new Vector3(0.18f, 0.45f, 0.18f), "Metal");
            Block("Keyboard", desk, new Vector3(2.9f, 0.95f, 3.55f), new Vector3(1.35f, 0.07f, 0.42f), "Metal");
            Block("Computer Tower", desk, new Vector3(4.3f, 1.35f, 4.25f), new Vector3(0.5f, 0.95f, 0.75f), "Metal");
            CreateChair(desk);
        }

        private static void CreateChair(Transform parent)
        {
            Transform chair = Group("Desk Chair", parent);
            Block("Chair Seat", chair, new Vector3(2.9f, 0.55f, 2.65f), new Vector3(1.05f, 0.18f, 1f), "Fabric");
            Block("Chair Back", chair, new Vector3(2.9f, 1.25f, 3.08f), new Vector3(1.05f, 1.3f, 0.18f), "Fabric");
            Block("Chair Base", chair, new Vector3(2.9f, 0.25f, 2.65f), new Vector3(0.18f, 0.5f, 0.18f), "Metal");
        }

        private static void CreateWardrobe(Transform parent)
        {
            Transform wardrobe = Group("Wardrobe", parent);
            Block("Wardrobe Body", wardrobe, new Vector3(-4.9f, 1.25f, -2.65f), new Vector3(1.7f, 2.5f, 1.1f), "DarkWood");
            Block("Wardrobe Door Left", wardrobe, new Vector3(-5.34f, 1.25f, -2.08f), new Vector3(0.78f, 2.3f, 0.08f), "Wood");
            Block("Wardrobe Door Right", wardrobe, new Vector3(-4.46f, 1.25f, -2.08f), new Vector3(0.78f, 2.3f, 0.08f), "Wood");
        }

        private static void CreateBookshelf(Transform parent)
        {
            Transform shelf = Group("Bookshelf", parent);
            Block("Bookshelf Body", shelf, new Vector3(4.9f, 1.2f, -3.45f), new Vector3(1.4f, 2.4f, 0.75f), "DarkWood");
            for (int i = 0; i < 4; i++)
                Block($"Shelf {i + 1}", shelf, new Vector3(4.9f, 0.25f + i * 0.58f, -3.02f), new Vector3(1.25f, 0.09f, 0.65f), "Wood");
            for (int i = 0; i < 7; i++)
                Block($"Book {i + 1}", shelf, new Vector3(4.42f + i * 0.15f, 0.53f, -2.97f), new Vector3(0.11f, 0.45f, 0.42f), i % 2 == 0 ? "Rug" : "Fabric");
        }

        private static void CreateModelHouse(Transform parent)
        {
            Transform model = Group("Model House Table", parent);
            Block("Model Table", model, new Vector3(-1.1f, 0.62f, 1f), new Vector3(1.8f, 0.16f, 1.5f), "Wood");
            Block("Model Table Base", model, new Vector3(-1.1f, 0.3f, 1f), new Vector3(0.2f, 0.6f, 0.2f), "Metal");
            Block("Model House Body", model, new Vector3(-1.1f, 0.94f, 1f), new Vector3(0.9f, 0.55f, 0.75f), "ModelHouse");
            GameObject roof = Block("Model House Roof", model, new Vector3(-1.1f, 1.35f, 1f), new Vector3(0.8f, 0.22f, 0.8f), "Rug");
            roof.transform.rotation = Quaternion.Euler(0f, 45f, 45f);
        }

        private static void CreateStoryProps(Transform parent)
        {
            Transform props = Group("Story Props", parent);

            Transform frame = Group("Empty Frame", props);
            Block("Frame Back", frame, new Vector3(-5.86f, 1.6f, 0.8f), new Vector3(0.08f, 0.9f, 0.7f), "Metal");
            Block("Frame Top", frame, new Vector3(-5.80f, 2.08f, 0.8f), new Vector3(0.08f, 0.08f, 0.8f), "Wood");
            Block("Frame Bottom", frame, new Vector3(-5.80f, 1.12f, 0.8f), new Vector3(0.08f, 0.08f, 0.8f), "Wood");
            Block("Frame Left", frame, new Vector3(-5.80f, 1.6f, 0.36f), new Vector3(0.08f, 1f, 0.08f), "Wood");
            Block("Frame Right", frame, new Vector3(-5.80f, 1.6f, 1.24f), new Vector3(0.08f, 1f, 0.08f), "Wood");

            Transform phone = Group("Old Telephone", props);
            Block("Telephone Base", phone, new Vector3(-2.55f, 1.01f, 3.95f), new Vector3(0.48f, 0.18f, 0.38f), "Metal");
            Block("Telephone Receiver", phone, new Vector3(-2.55f, 1.18f, 3.95f), new Vector3(0.62f, 0.12f, 0.16f), "DarkWood");
            Sphere("Telephone Dial", phone, new Vector3(-2.55f, 1.11f, 3.73f), new Vector3(0.18f, 0.04f, 0.18f), "Cream");

            Transform clock = Group("Midnight Clock", props);
            Sphere("Clock Face", clock, new Vector3(0.6f, 1.85f, 4.86f), new Vector3(0.7f, 0.7f, 0.08f), "Cream");
            Block("Clock Hour Hand", clock, new Vector3(0.6f, 2.01f, 4.80f), new Vector3(0.04f, 0.30f, 0.03f), "Metal");
            Block("Clock Minute Hand", clock, new Vector3(0.6f, 2.05f, 4.77f), new Vector3(0.035f, 0.42f, 0.03f), "Metal");
        }

        private static void CreateLighting(Transform root)
        {
            Transform lighting = Group("Lighting", root);
            Light ceiling = LightObject("Warm Ceiling Light", lighting, new Vector3(0f, 2.72f, 0f), LightType.Point, new Color(1f, 0.68f, 0.42f), 5.5f, 9f);
            ceiling.shadows = LightShadows.Soft;
            Light desk = LightObject("Monitor Glow", lighting, new Vector3(2.9f, 1.65f, 3.95f), LightType.Point, new Color(0.2f, 0.75f, 0.78f), 1.5f, 3.2f);
            desk.shadows = LightShadows.None;
            Light window = LightObject("Cool Window Fill", lighting, new Vector3(5.3f, 1.8f, 0f), LightType.Point, new Color(0.42f, 0.58f, 0.75f), 2.3f, 5f);
            window.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.105f, 0.1f);
        }

        private static void CreateAudioZone(Transform root)
        {
            var zoneObject = new GameObject("Reality Room Audio Zone");
            zoneObject.transform.SetParent(root, false);
            AudioReverbZone zone = zoneObject.AddComponent<AudioReverbZone>();
            zone.minDistance = 2f;
            zone.maxDistance = 18f;
            zone.reverbPreset = AudioReverbPreset.Room;
        }

        private static void PreparePlayer()
        {
            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            if (player == null) throw new InvalidOperationException("Stage 3 player is missing from Reality Room.");
            player.transform.position = new Vector3(0f, 0.05f, -3.6f);
            player.transform.rotation = Quaternion.identity;

            GameObject oldCamera = GameObject.Find("Main Camera");
            if (oldCamera != null && oldCamera.GetComponentInParent<FirstPersonPlayerController>() == null)
                UnityEngine.Object.DestroyImmediate(oldCamera);
        }

        private static void Validate(GameObject root)
        {
            string[] required =
            {
                "Architecture", "Floor", "Ceiling", "Door", "Window Glass", "Furniture", "Bed",
                "Computer Desk", "Monitor Screen", "Wardrobe", "Bookshelf", "Model House Table",
                "Door Corridor", "Lighting", "Reality Room Audio Zone", "Empty Frame", "Old Telephone", "Midnight Clock"
            };
            foreach (string name in required)
            {
                if (FindChild(root.transform, name) == null)
                    throw new InvalidOperationException($"Required Stage 4 object is missing: {name}");
            }

            if (root.GetComponentsInChildren<Collider>(true).Length < 25)
                throw new InvalidOperationException("Stage 4 collision coverage is incomplete.");
            if (root.GetComponentsInChildren<Light>(true).Length < 3)
                throw new InvalidOperationException("Stage 4 lighting setup is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Reality Room requires exactly one AudioListener.");
            if (UnityEngine.Object.FindObjectsByType<FirstPersonPlayerController>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Reality Room requires exactly one player.");
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static void RemoveOwnedContent()
        {
            foreach (string name in new[]
                     {
                         RootName, "Stage 3 Test Space", "Reality Room Placeholder", "Directional Light"
                     })
            {
                GameObject existing = GameObject.Find(name);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 scale, string material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = Materials[material];
            return block;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, string material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent);
            sphere.transform.position = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = Materials[material];
            return sphere;
        }

        private static Light LightObject(string name, Transform parent, Vector3 position, LightType type, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            return light;
        }
    }
}
