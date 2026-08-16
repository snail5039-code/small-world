using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SmallWorld.Core;
using SmallWorld.Flow;
using SmallWorld.Player;
using SmallWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage12MemorySceneGenerator
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string RealityRoomEntryName = "First Memory Entry";

        public static void GenerateFromBatchMode()
        {
            try
            {
                Generate();
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("Small World/Stage 12/Generate First Memory Scene")]
        public static void Generate()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null) throw new InvalidOperationException($"Missing input actions at {InputActionsPath}.");

            string path = SceneCatalog.GetPath(SceneId.FirstMemory);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("First Memory Space");
            Stage12MemorySpaceController spaceController = root.AddComponent<Stage12MemorySpaceController>();
            Stage13MemoryPuzzleController puzzleController = root.AddComponent<Stage13MemoryPuzzleController>();

            CreateRoom(root.transform);
            Transform safeZone = CreateSafeZone(root.transform);
            Light memoryLight = CreateMemoryLight(root.transform);
            Renderer[] markers = CreatePuzzleMarkers(root.transform);
            GameObject exitSeal = CreateExit(root.transform);
            WireRuntimeAdapters(puzzleController, spaceController, markers, exitSeal);
            CreatePlayer(actions);

            SetSerializedField(spaceController, "safeZone", safeZone);
            SetSerializedField(spaceController, "memoryLight", memoryLight);
            SetSerializedField(spaceController, "memoryMarkers", markers);

            Validate(spaceController, puzzleController, markers);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("Could not save the first memory scene.");

            IntegrateRealityRoomEntry();

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(entry => entry.path == path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static void IntegrateRealityRoomEntry()
        {
            string path = SceneCatalog.GetPath(SceneId.RealityRoom);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject existing = GameObject.Find(RealityRoomEntryName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            GameObject entry = CreateBlock(RealityRoomEntryName, null,
                new Vector3(-1f, 1.25f, -7.85f), new Vector3(1f, 2.5f, 0.2f));
            FirstMemoryEntryInteractable adapter = entry.AddComponent<FirstMemoryEntryInteractable>();
            adapter.ConfigureEntry();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("Could not save the first-memory entry in the white room.");
        }

        private static void CreateRoom(Transform parent)
        {
            var room = new GameObject("Memory Room Architecture");
            room.transform.SetParent(parent, false);
            CreateBlock("Floor", room.transform, new Vector3(0f, -0.1f, 2f), new Vector3(12f, 0.2f, 14f));
            CreateBlock("North Wall", room.transform, new Vector3(0f, 2f, 9f), new Vector3(12f, 4f, 0.2f));
            CreateBlock("South Wall Left", room.transform, new Vector3(-3.5f, 2f, -5f), new Vector3(5f, 4f, 0.2f));
            CreateBlock("South Wall Right", room.transform, new Vector3(3.5f, 2f, -5f), new Vector3(5f, 4f, 0.2f));
            CreateBlock("East Wall", room.transform, new Vector3(6f, 2f, 2f), new Vector3(0.2f, 4f, 14f));
            CreateBlock("West Wall", room.transform, new Vector3(-6f, 2f, 2f), new Vector3(0.2f, 4f, 14f));
            CreateBlock("Ceiling", room.transform, new Vector3(0f, 4f, 2f), new Vector3(12f, 0.2f, 14f));
        }

        private static Transform CreateSafeZone(Transform parent)
        {
            GameObject safe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            safe.name = "Arrival Safe Zone";
            safe.transform.SetParent(parent, false);
            safe.transform.localPosition = new Vector3(0f, 0.03f, -2.5f);
            safe.transform.localScale = new Vector3(2f, 0.03f, 2f);
            return safe.transform;
        }

        private static Light CreateMemoryLight(Transform parent)
        {
            var lightObject = new GameObject("Memory Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(0f, 3.3f, 2f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 2f;
            light.range = 14f;
            light.color = new Color(0.55f, 0.7f, 1f);
            return light;
        }

        private static Renderer[] CreatePuzzleMarkers(Transform parent)
        {
            var puzzle = new GameObject("Sequence Puzzle 1-2-3");
            puzzle.transform.SetParent(parent, false);
            var renderers = new Renderer[3];
            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject pedestal = CreateBlock($"Memory Pedestal {i + 1}", puzzle.transform,
                    new Vector3((i - 1) * 2.25f, 0.55f, 3.5f), new Vector3(1.2f, 1.1f, 1.2f));
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Memory Marker {i + 1}";
                marker.transform.SetParent(pedestal.transform, false);
                marker.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                marker.transform.localScale = Vector3.one * 0.55f;
                renderers[i] = marker.GetComponent<Renderer>();
            }
            return renderers;
        }

        private static GameObject CreateExit(Transform parent)
        {
            var exit = new GameObject("White Room Exit");
            exit.transform.SetParent(parent, false);
            CreateBlock("Exit Left Pillar", exit.transform, new Vector3(-1.25f, 1.5f, 8.75f), new Vector3(0.5f, 3f, 0.5f));
            CreateBlock("Exit Right Pillar", exit.transform, new Vector3(1.25f, 1.5f, 8.75f), new Vector3(0.5f, 3f, 0.5f));
            CreateBlock("Exit Header", exit.transform, new Vector3(0f, 3.25f, 8.75f), new Vector3(3f, 0.5f, 0.5f));
            return CreateBlock("Exit Seal", exit.transform, new Vector3(0f, 1.5f, 8.9f), new Vector3(2f, 3f, 0.2f));
        }

        private static void WireRuntimeAdapters(Stage13MemoryPuzzleController puzzleController,
            Stage12MemorySpaceController spaceController, Renderer[] markers, GameObject exitSeal)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                MemoryPuzzleChoiceInteractable adapter =
                    markers[i].gameObject.AddComponent<MemoryPuzzleChoiceInteractable>();
                adapter.ConfigureChoice(puzzleController, i + 1, $"Select memory {i + 1}");
            }

            MemoryExitInteractable exitAdapter = exitSeal.AddComponent<MemoryExitInteractable>();
            exitAdapter.ConfigureExit(spaceController);
        }

        private static void CreatePlayer(InputActionAsset actions)
        {
            var playerObject = new GameObject("First Person Player");
            playerObject.transform.position = new Vector3(0f, 0.05f, -2.5f);
            CharacterController character = playerObject.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.32f;
            character.center = new Vector3(0f, 0.9f, 0f);
            character.stepOffset = 0.3f;

            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 85f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            AudioSource source = playerObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            PlayerFootstepEmitter footsteps = playerObject.AddComponent<PlayerFootstepEmitter>();
            footsteps.Configure(source);
            PlayerInteractionDetector detector = playerObject.AddComponent<PlayerInteractionDetector>();
            detector.Configure(camera.transform, 2f);

            Image crosshair = CreateHud(detector);
            FirstPersonPlayerController controller = playerObject.AddComponent<FirstPersonPlayerController>();
            controller.Configure(camera, actions, detector, footsteps, crosshair);
        }

        private static Image CreateHud(PlayerInteractionDetector detector)
        {
            var hud = new GameObject("Player HUD", typeof(RectTransform));
            Canvas canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = hud.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = new GameObject("Safe Area", typeof(RectTransform));
            safeArea.transform.SetParent(hud.transform, false);
            RectTransform safeRect = (RectTransform)safeArea.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaFitter>();

            var crosshairObject = new GameObject("Crosshair", typeof(RectTransform));
            crosshairObject.transform.SetParent(hud.transform, false);
            RectTransform crosshairRect = (RectTransform)crosshairObject.transform;
            crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.sizeDelta = new Vector2(4f, 4f);
            Image crosshair = crosshairObject.AddComponent<Image>();
            crosshair.color = new Color(1f, 1f, 1f, 0.8f);
            crosshair.raycastTarget = false;

            var interactionUi = new GameObject("Interaction UI", typeof(RectTransform));
            interactionUi.transform.SetParent(safeArea.transform, false);
            RectTransform interactionRect = (RectTransform)interactionUi.transform;
            interactionRect.anchorMin = Vector2.zero;
            interactionRect.anchorMax = Vector2.one;
            interactionRect.offsetMin = interactionRect.offsetMax = Vector2.zero;
            InteractionPromptView promptView = interactionUi.AddComponent<InteractionPromptView>();
            Text prompt = CreateText("Interaction Prompt", interactionUi.transform, new Vector2(0.5f, 0.33f), 25);
            Text feedback = CreateText("Interaction Feedback", interactionUi.transform, new Vector2(0.5f, 0.72f), 27);
            promptView.Configure(prompt, feedback);
            detector.ConfigureView(promptView);
            return crosshair;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchor, int size)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(820f, 100f);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            return block;
        }

        private static void SetSerializedField<T>(object target, string name, T value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            field.SetValue(target, value);
        }

        private static void Validate(Stage12MemorySpaceController spaceController,
            Stage13MemoryPuzzleController puzzleController, Renderer[] markers)
        {
            if (spaceController == null || puzzleController == null)
                throw new InvalidOperationException("Memory flow controllers are missing.");
            if (markers == null || markers.Length != 3)
                throw new InvalidOperationException("The sequence puzzle requires exactly three markers.");
            if (UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>() == null ||
                UnityEngine.Object.FindFirstObjectByType<PlayerInteractionDetector>() == null)
                throw new InvalidOperationException("The first memory player connection is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<MemoryPuzzleChoiceInteractable>(FindObjectsSortMode.None).Length != 3 ||
                UnityEngine.Object.FindFirstObjectByType<MemoryExitInteractable>() == null)
                throw new InvalidOperationException("The Stage 14 memory interaction wiring is incomplete.");
        }
    }
}
