using System;
using System.IO;
using SmallWorld.Core;
using SmallWorld.Flow;
using SmallWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage2SceneGenerator
    {
        private static readonly Color Background = new Color(0.035f, 0.045f, 0.07f, 1f);
        private static readonly Color Panel = new Color(0.09f, 0.11f, 0.16f, 0.96f);
        private static readonly Color Accent = new Color(0.3f, 0.72f, 0.92f, 1f);

        [MenuItem("Small World/Stage 2/Generate Flow Scenes")]
        public static void GenerateFromMenu()
        {
            try
            {
                GenerateAndValidate();
                Debug.Log("[SmallWorld] Stage 2 flow scenes generated successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void GenerateFromBatchMode()
        {
            int exitCode = 0;
            try
            {
                GenerateAndValidate();
            }
            catch (Exception exception)
            {
                exitCode = 1;
                Debug.LogException(exception);
            }
            finally
            {
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(exitCode);
                }
            }

            if (exitCode != 0)
            {
                throw new InvalidOperationException("Stage 2 scene generation failed.");
            }
        }

        private static void GenerateAndValidate()
        {
            EnsureDirectory(Path.GetDirectoryName(SceneCatalog.GetPath(SceneId.Boot)));
            CreateBootScene();
            CreateMainMenuScene();
            CreateRealityRoomScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneCatalog.GetPath(SceneId.Boot), true),
                new EditorBuildSettingsScene(SceneCatalog.GetPath(SceneId.MainMenu), true),
                new EditorBuildSettingsScene(SceneCatalog.GetPath(SceneId.RealityRoom), true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateGeneratedScenes();
        }

        private static void CreateBootScene()
        {
            Scene scene = NewScene(SceneId.Boot);
            CreateCamera();

            var bootstrapRoot = new GameObject("Bootstrapper");
            Bootstrapper bootstrapper = bootstrapRoot.AddComponent<Bootstrapper>();

            Canvas canvas = CreateCanvas("Loading Canvas", 100);
            CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
            Image backdrop = CreateImage("Backdrop", CanvasContent(canvas), Background);
            Stretch(backdrop.rectTransform);

            Text label = CreateText("Loading Label", backdrop.transform, "불러오는 중", 28, TextAnchor.MiddleCenter);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(340f, 60f), new Vector2(0f, 40f));

            Slider slider = CreateSlider(backdrop.transform);
            var loadingView = canvas.gameObject.AddComponent<LoadingScreenView>();
            loadingView.Configure(group, slider);
            bootstrapper.Configure(loadingView);

            Save(scene, SceneId.Boot);
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = NewScene(SceneId.MainMenu);
            CreateCamera();
            CreateEventSystem();

            Canvas canvas = CreateCanvas("Main Menu Canvas");
            Image backdrop = CreateImage("Backdrop", CanvasContent(canvas), Background);
            Stretch(backdrop.rectTransform);
            Image panel = CreateImage("Menu Panel", backdrop.transform, Panel);
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 460f), Vector2.zero);

            Text title = CreateText("Title", panel.transform, "SMALL WORLD", 42, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(440f, 100f), new Vector2(0f, 125f));

            Button newGame = CreateButton("New Game Button", panel.transform, "새 게임", new Vector2(0f, 10f));
            Button quit = CreateButton("Quit Button", panel.transform, "종료", new Vector2(0f, -85f));

            TitleScreenController controller = panel.gameObject.AddComponent<TitleScreenController>();
            controller.Configure(newGame, quit);
            Save(scene, SceneId.MainMenu);
        }

        private static void CreateRealityRoomScene()
        {
            Scene scene = NewScene(SceneId.RealityRoom);
            Camera camera = CreateCamera();
            camera.transform.position = new Vector3(0f, 1.6f, -5f);

            var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
            room.name = "Reality Room Placeholder";
            room.transform.position = new Vector3(0f, 0f, 2f);
            room.transform.localScale = new Vector3(5f, 0.2f, 5f);

            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

            var flow = new GameObject("Reality Room Flow");
            flow.AddComponent<RealityRoomController>();

            Canvas canvas = CreateCanvas("Reality Room UI");
            Text hint = CreateText("Return Hint", CanvasContent(canvas), "Esc · 메뉴 열기", 20, TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(420f, 50f), new Vector2(0f, 45f));
            Save(scene, SceneId.RealityRoom);
        }

        private static Scene NewScene(SceneId id)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = SceneCatalog.GetName(id);
            return scene;
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Canvas CreateCanvas(string name, int sortingOrder = 0)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            GameObject safeArea = new GameObject("Safe Area", typeof(RectTransform));
            safeArea.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)safeArea.transform);
            safeArea.AddComponent<SafeAreaFitter>();
            return canvas;
        }

        private static Transform CanvasContent(Canvas canvas) => canvas.transform.Find("Safe Area") ?? canvas.transform;

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
        {
            Image image = CreateImage(name, parent, Accent);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(340f, 70f), position);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", image.transform, label, 24, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Slider CreateSlider(Transform parent)
        {
            Image background = CreateImage("Loading Progress", parent, new Color(1f, 1f, 1f, 0.18f));
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420f, 18f), new Vector2(0f, -25f));
            Slider slider = background.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Image fill = CreateImage("Fill", background.transform, Accent);
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            return slider;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Save(Scene scene, SceneId id)
        {
            string path = SceneCatalog.GetPath(id);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException($"Could not save generated scene '{path}'.");
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new InvalidOperationException("SceneCatalog returned an invalid scene directory.");
            }

            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(absolutePath);
        }

        private static void ValidateGeneratedScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 3 || scenes[0].path != SceneCatalog.GetPath(SceneId.Boot))
            {
                throw new InvalidOperationException("Stage 2 Build Settings order is invalid.");
            }

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!scene.enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) == null)
                {
                    throw new InvalidOperationException($"Generated scene is missing or disabled: {scene.path}");
                }
            }
        }
    }
}
