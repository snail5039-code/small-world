using System;
using SmallWorld.Core;
using SmallWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage3PlayerGenerator
    {
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Small World/Stage 3/Generate Player Test Space")]
        public static void GenerateFromMenu()
        {
            try
            {
                GenerateAndValidate();
                Debug.Log("[SmallWorld] Stage 3 player test space generated successfully.");
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
            RemoveExistingPlayerAndCamera();

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null) throw new InvalidOperationException($"Missing input actions at {InputActionsPath}.");

            CreateTestSpace();
            FirstPersonPlayerController player = CreatePlayer(actions);
            if (player.GetComponent<CharacterController>() == null || player.GetComponentInChildren<Camera>() == null)
                throw new InvalidOperationException("Generated player is incomplete.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new InvalidOperationException("Could not save RealityRoom.");
        }

        private static FirstPersonPlayerController CreatePlayer(InputActionAsset actions)
        {
            var root = new GameObject("First Person Player");
            root.transform.position = new Vector3(0f, 0.05f, -3.5f);
            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.32f;
            character.center = new Vector3(0f, 0.9f, 0f);
            character.stepOffset = 0.3f;

            var cameraObject = new GameObject("Player Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 85f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            AudioSource source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            PlayerFootstepEmitter footsteps = root.AddComponent<PlayerFootstepEmitter>();
            footsteps.Configure(source);
            PlayerInteractionDetector detector = root.AddComponent<PlayerInteractionDetector>();
            detector.Configure(camera.transform, 2f);

            Image crosshair = CreateCrosshair();
            FirstPersonPlayerController controller = root.AddComponent<FirstPersonPlayerController>();
            controller.Configure(camera, actions, detector, footsteps, crosshair);
            return controller;
        }

        private static Image CreateCrosshair()
        {
            var canvasObject = new GameObject("Player HUD", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var dotObject = new GameObject("Crosshair", typeof(RectTransform));
            dotObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)dotObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(4f, 4f);
            Image image = dotObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.8f);
            image.raycastTarget = false;
            return image;
        }

        private static void CreateTestSpace()
        {
            GameObject old = GameObject.Find("Stage 3 Test Space");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Stage 3 Test Space");
            CreateBlock("Floor", root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 12f));
            CreateBlock("North Wall", root.transform, new Vector3(0f, 1.5f, 6f), new Vector3(12f, 3f, 0.2f));
            CreateBlock("South Wall", root.transform, new Vector3(0f, 1.5f, -6f), new Vector3(12f, 3f, 0.2f));
            CreateBlock("East Wall", root.transform, new Vector3(6f, 1.5f, 0f), new Vector3(0.2f, 3f, 12f));
            CreateBlock("West Wall", root.transform, new Vector3(-6f, 1.5f, 0f), new Vector3(0.2f, 3f, 12f));
            // Keep the target inside the detector's two-metre range from the spawn camera.
            CreateBlock("Raycast Target", root.transform, new Vector3(0f, 1f, -1.8f), new Vector3(0.7f, 2f, 0.4f));
        }

        private static void CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
        }

        private static void RemoveExistingPlayerAndCamera()
        {
            foreach (FirstPersonPlayerController player in UnityEngine.Object.FindObjectsByType<FirstPersonPlayerController>(FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(player.gameObject);
            GameObject hud = GameObject.Find("Player HUD");
            if (hud != null) UnityEngine.Object.DestroyImmediate(hud);
        }
    }
}
