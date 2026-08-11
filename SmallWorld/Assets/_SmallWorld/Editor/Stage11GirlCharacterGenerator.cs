using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Character;
using SmallWorld.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Editor
{
    public static class Stage11GirlCharacterGenerator
    {
        private const string RootName = "Stage 11 Girl Character";
        private const string MaterialDirectory = "Assets/_SmallWorld/Art/Materials/Stage11";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Small World/Stage 11/Generate Girl Character")]
        public static void GenerateFromMenu()
        {
            try { GenerateAndValidate(); Debug.Log("[SmallWorld] Stage 11 girl character generated successfully."); }
            catch (Exception exception) { Debug.LogException(exception); }
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
            Scene scene = EditorSceneManager.OpenScene(SceneCatalog.GetPath(SceneId.RealityRoom), OpenSceneMode.Single);
            GameObject oldRoot = GameObject.Find(RootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
            CreateMaterials();

            var root = new GameObject(RootName);
            Transform character = Group("Girl Character", root.transform);
            character.position = new Vector3(0.4f, 0f, 1.9f);
            character.rotation = Quaternion.Euler(0f, 180f, 0f);
            GirlCharacterController controller = character.gameObject.AddComponent<GirlCharacterController>();
            GirlCharacterRuntimeBridgeComponent bridge = character.gameObject.AddComponent<GirlCharacterRuntimeBridgeComponent>();
            CharacterController movement = character.GetComponent<CharacterController>();
            movement.height = 1.72f;
            movement.radius = 0.32f;
            movement.center = new Vector3(0f, 0.86f, 0f);
            movement.skinWidth = 0.04f;
            movement.stepOffset = 0.2f;

            Transform presentation = Group("Replaceable Visual Root", character);
            Transform head = BuildPrototype(presentation, out Renderer face);
            Transform[] waypoints = CreateWaypoints(root.transform);
            Camera playerCamera = Camera.main;
            controller.Configure(presentation, head, face, waypoints, playerCamera != null ? playerCamera.transform : null);
            bridge.Configure(controller);

            Validate(root, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the Reality Room scene.");
            AssetDatabase.SaveAssets();
        }

        private static Transform BuildPrototype(Transform parent, out Renderer faceRenderer)
        {
            Transform body = Group("Prototype Body", parent);
            Primitive("Dress", PrimitiveType.Capsule, body, new Vector3(0f, 0.88f, 0f), new Vector3(0.68f, 0.88f, 0.48f), "Dress");
            Primitive("Collar", PrimitiveType.Cylinder, body, new Vector3(0f, 1.30f, 0f), new Vector3(0.38f, 0.06f, 0.38f), "Collar");

            Transform head = Group("Head Look Target", body);
            head.localPosition = new Vector3(0f, 1.52f, 0f);
            Primitive("Head", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.48f, 0.52f, 0.46f), "Skin");
            Primitive("Hair Cap", PrimitiveType.Sphere, head, new Vector3(0f, 0.11f, 0.05f), new Vector3(0.53f, 0.47f, 0.50f), "Hair");
            GameObject face = Primitive("Expression Face", PrimitiveType.Cube, head, new Vector3(0f, 0f, 0.235f), new Vector3(0.28f, 0.12f, 0.018f), "Face");
            faceRenderer = face.GetComponent<Renderer>();

            Primitive("Left Arm", PrimitiveType.Capsule, body, new Vector3(-0.40f, 0.92f, 0f), new Vector3(0.16f, 0.52f, 0.16f), "Skin");
            Primitive("Right Arm", PrimitiveType.Capsule, body, new Vector3(0.40f, 0.92f, 0f), new Vector3(0.16f, 0.52f, 0.16f), "Skin");
            Primitive("Left Leg", PrimitiveType.Capsule, body, new Vector3(-0.18f, 0.35f, 0f), new Vector3(0.19f, 0.48f, 0.19f), "Stocking");
            Primitive("Right Leg", PrimitiveType.Capsule, body, new Vector3(0.18f, 0.35f, 0f), new Vector3(0.19f, 0.48f, 0.19f), "Stocking");
            return head;
        }

        private static Transform[] CreateWaypoints(Transform parent)
        {
            Transform group = Group("Room Patrol Waypoints", parent);
            Vector3[] worldPositions =
            {
                new Vector3(0.4f, 0f, 1.9f),
                new Vector3(1.8f, 0f, 1.6f),
                new Vector3(1.9f, 0f, -0.8f),
                new Vector3(-0.2f, 0f, -1.1f)
            };
            var points = new Transform[worldPositions.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Group($"Waypoint {i + 1}", group);
                points[i].position = worldPositions[i];
            }
            return points;
        }

        private static void CreateMaterials()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "_SmallWorld/Art/Materials/Stage11"));
            AssetDatabase.Refresh();
            Materials.Clear();
            CreateMaterial("Skin", new Color(0.92f, 0.72f, 0.62f));
            CreateMaterial("Hair", new Color(0.13f, 0.09f, 0.08f));
            CreateMaterial("Dress", new Color(0.34f, 0.46f, 0.62f));
            CreateMaterial("Collar", new Color(0.92f, 0.88f, 0.78f));
            CreateMaterial("Stocking", new Color(0.18f, 0.20f, 0.25f));
            CreateMaterial("Face", new Color(0.62f, 0.48f, 0.42f), true);
        }

        private static void CreateMaterial(string name, Color color, bool emission = false)
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
            material.SetFloat("_Smoothness", 0.28f);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.25f);
            }
            EditorUtility.SetDirty(material);
            Materials[name] = material;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, string material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.GetComponent<Renderer>().sharedMaterial = Materials[material];
            Collider collider = item.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return item;
        }

        private static Transform Group(string name, Transform parent)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            return item.transform;
        }

        private static void Validate(GameObject root, GirlCharacterController controller)
        {
            if (controller.VisualRoot == null) throw new InvalidOperationException("Replaceable visual root is missing.");
            if (controller.GetComponent<GirlCharacterRuntimeBridgeComponent>() == null) throw new InvalidOperationException("Girl character runtime bridge component is missing.");
            if (root.GetComponentsInChildren<Renderer>(true).Length < 9) throw new InvalidOperationException("Prototype character presentation is incomplete.");
            if (root.transform.Find("Girl Character/Replaceable Visual Root") == null) throw new InvalidOperationException("Replaceable character presentation is missing.");
            if (root.transform.Find("Room Patrol Waypoints") == null) throw new InvalidOperationException("Character waypoints are missing.");
            if (UnityEngine.Object.FindObjectsByType<GirlCharacterController>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Reality Room requires exactly one girl character.");
        }
    }
}
