using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Flow;
using SmallWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage15StoryRouteGenerator
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private const string RealityRoomPath = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private static readonly string[] Ids = { "prologue", "chapter-1", "chapter-2", "chapter-3", "chapter-4", "chapter-5", "chapter-6" };
        private static readonly string[] Names = { "Prologue - The White Room", "Chapter 1 - The Fourth Place", "Chapter 2 - Last Platform", "Chapter 3 - A Perfect Day", "Chapter 4 - Faceless Office", "Chapter 5 - Cemetery Without a Funeral", "Chapter 6 - City in the Window" };

        [MenuItem("Small World/Stage 15/Generate Story Route Skeleton")]
        public static void Generate()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null) throw new InvalidOperationException($"Missing input actions at {InputPath}.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform player = CreatePlayer(actions);
            var routeRoot = new GameObject("Stage 15 Story Route");
            StoryRouteController route = routeRoot.AddComponent<StoryRouteController>();
            routeRoot.AddComponent<StoryRouteProgressAdapter>();
            StoryRouteNode[] nodes = CreateRoute(routeRoot.transform, route);
            route.Configure(player, nodes);
            CreateLighting(routeRoot.transform);
            CreateFinalGate(routeRoot.transform, route, nodes.Length);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save Stage 15 story route.");
            IntegrateRealityRoom();
            AddBuildScene();
            AssetDatabase.SaveAssets();
        }

        private static StoryRouteNode[] CreateRoute(Transform root, StoryRouteController route)
        {
            var nodes = new StoryRouteNode[Ids.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                float z = i * 18f;
                var hub = new GameObject($"{i:00} {Names[i]}");
                hub.transform.SetParent(root, false);
                CreateBlock("Hub Floor", hub.transform, new Vector3(0f, -0.1f, z), new Vector3(14f, 0.2f, 14f));
                Transform arrival = new GameObject("Arrival").transform;
                arrival.SetParent(hub.transform, false);
                arrival.position = new Vector3(0f, 0.05f, z - 4.5f);
                Transform dialogue = CreateMarker("Dialogue Entry", hub.transform, new Vector3(-4f, 0.75f, z), route, Ids[i], StoryRouteStep.Dialogue, "Inspect dialogue entry", $"{Names[i]} dialogue completed.");
                Transform puzzle = CreateMarker("Puzzle Entry", hub.transform, new Vector3(0f, 0.75f, z + 2f), route, Ids[i], StoryRouteStep.Puzzle, "Inspect puzzle entry", $"{Names[i]} puzzle completed.");
                Transform memory = CreateMarker("Memory Space Entry", hub.transform, new Vector3(4f, 0.75f, z), route, Ids[i], StoryRouteStep.Memory, "Inspect memory-space entry", $"{Names[i]} memory-space completed.");
                nodes[i] = new StoryRouteNode { Id = Ids[i], DisplayName = Names[i], Arrival = arrival, DialogueEntry = dialogue, PuzzleEntry = puzzle, MemoryEntry = memory };
                if (i < nodes.Length - 1)
                {
                    GameObject gate = CreateBlock("Next Chapter Gate", hub.transform, new Vector3(0f, 1.25f, z + 6f), new Vector3(3f, 2.5f, 0.35f));
                    gate.AddComponent<StoryRouteInteractable>().ConfigureTravel(route, i + 1, $"Continue to {Names[i + 1]}");
                }
            }
            return nodes;
        }

        private static Transform CreateMarker(string name, Transform parent, Vector3 position,
            StoryRouteController route, string nodeId, StoryRouteStep step, string prompt, string feedback)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.2f, 0.75f, 1.2f);
            marker.AddComponent<StoryRouteInteractable>().ConfigureMarker(route, nodeId, step, prompt, feedback);
            return marker.transform;
        }

        private static void CreateFinalGate(Transform root, StoryRouteController route, int nodeCount)
        {
            float z = (nodeCount - 1) * 18f + 6f;
            GameObject gate = CreateBlock("Final Chapter Locked Gate", root, new Vector3(0f, 1.75f, z), new Vector3(5f, 3.5f, 0.5f));
            gate.AddComponent<StoryRouteInteractable>().ConfigureFinalGate(route, "Inspect the final chapter gate");
        }

        private static void IntegrateRealityRoom()
        {
            Scene scene = EditorSceneManager.OpenScene(RealityRoomPath, OpenSceneMode.Single);
            GameObject existing = GameObject.Find("Stage 15 Story Route Entry");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            GameObject entry = CreateBlock("Stage 15 Story Route Entry", null, new Vector3(3.25f, 1f, -7.75f), new Vector3(1.5f, 2f, 0.25f));
            entry.AddComponent<StoryRouteEntryInteractable>().ConfigureEntry();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, RealityRoomPath)) throw new InvalidOperationException("Could not integrate the Stage 15 route entry.");
        }

        private static Transform CreatePlayer(InputActionAsset actions)
        {
            var playerObject = new GameObject("First Person Player");
            playerObject.transform.position = new Vector3(0f, 0.05f, -4.5f);
            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f; character.radius = 0.32f; character.center = new Vector3(0f, 0.9f, 0f);
            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.fieldOfView = 85f; camera.nearClipPlane = 0.05f; cameraObject.AddComponent<AudioListener>();
            AudioSource source = playerObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            PlayerFootstepEmitter footsteps = playerObject.AddComponent<PlayerFootstepEmitter>(); footsteps.Configure(source);
            PlayerInteractionDetector detector = playerObject.AddComponent<PlayerInteractionDetector>(); detector.Configure(camera.transform, 2.5f);
            Image crosshair = CreateHud(detector);
            playerObject.AddComponent<FirstPersonPlayerController>().Configure(camera, actions, detector, footsteps, crosshair);
            return playerObject.transform;
        }

        private static Image CreateHud(PlayerInteractionDetector detector)
        {
            var hud = new GameObject("Player HUD", typeof(RectTransform));
            Canvas canvas = hud.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform)); crosshairObject.transform.SetParent(hud.transform, false);
            RectTransform crosshairRect = (RectTransform)crosshairObject.transform; crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f); crosshairRect.sizeDelta = new Vector2(4f, 4f);
            Image crosshair = crosshairObject.AddComponent<Image>(); crosshair.color = new Color(1f, 1f, 1f, 0.8f); crosshair.raycastTarget = false;
            var ui = new GameObject("Interaction UI", typeof(RectTransform)); ui.transform.SetParent(hud.transform, false);
            RectTransform rect = (RectTransform)ui.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            InteractionPromptView view = ui.AddComponent<InteractionPromptView>(); view.Configure(CreateText("Prompt", ui.transform, 0.33f), CreateText("Feedback", ui.transform, 0.72f)); detector.ConfigureView(view);
            return crosshair;
        }

        private static Text CreateText(string name, Transform parent, float y)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, y); rect.sizeDelta = new Vector2(900f, 100f);
            Text text = go.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; return text;
        }

        private static void CreateLighting(Transform root)
        {
            var lightObject = new GameObject("Route Directional Light"); lightObject.transform.SetParent(root, false); lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name; block.transform.SetParent(parent, true); block.transform.position = position; block.transform.localScale = scale; return block;
        }

        private static void AddBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
