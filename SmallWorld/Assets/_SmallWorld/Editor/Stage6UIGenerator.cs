using System;
using System.IO;
using SmallWorld.Core;
using SmallWorld.Flow;
using SmallWorld.Player;
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
    public static class Stage6UIGenerator
    {
        public const string StandardKoreanFontPath =
            "Assets/_SmallWorld/UI/Fonts/NotoSansKR/NotoSansKR-Variable.ttf";
        public const string FontEditorPrefsKey = "SmallWorld.Stage6.KoreanFontAssetPath";
        public const string MainMenuRootName = "Stage 6 Main Menu UI";
        public const string RealityRootName = "Stage 6 Reality Room UI";

        private static readonly Color Backdrop = new Color(0.025f, 0.03f, 0.05f, 0.97f);
        private static readonly Color Panel = new Color(0.07f, 0.085f, 0.12f, 0.98f);
        private static readonly Color Accent = new Color(0.25f, 0.76f, 0.78f, 1f);
        private const string RequiredKoreanText = "둘만의 작은 세계 새 게임 이어하기 설정 종료";
        private static Font font;

        [MenuItem("Small World/Stage 6/Generate UI Integration")]
        public static void GenerateFromMenu()
        {
            try
            {
                GenerateAndValidate();
                Debug.Log("[SmallWorld] Stage 6 UI integration generated successfully.");
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

        public static void GenerateAndValidate()
        {
            ResolveFont();
            IntegrateMainMenu();
            IntegrateRealityRoom();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool TryResolveInjectedKoreanFont(out Font injectedFont, out string blocker)
        {
            string path = EditorPrefs.GetString(FontEditorPrefsKey, string.Empty);
            injectedFont = string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
            blocker = injectedFont != null
                ? string.Empty
                : $"Korean font is not configured. Import a licensed project-owned Font asset and set EditorPrefs key '{FontEditorPrefsKey}' to its Assets path.";
            return injectedFont != null;
        }

        private static void ResolveFont()
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(StandardKoreanFontPath);
            if (font == null && !TryResolveInjectedKoreanFont(out font, out string blocker))
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                Debug.LogWarning($"[SmallWorld] Stage 6 Korean text rendering blocker: {blocker} The generated UI uses Unity's fallback font until injected.");
            }
            if (font == null) throw new InvalidOperationException("No usable UI font is available.");

            foreach (char character in RequiredKoreanText)
            {
                if (char.IsWhiteSpace(character) || font.HasCharacter(character)) continue;
                string path = AssetDatabase.GetAssetPath(font);
                throw new InvalidOperationException(
                    $"Stage 6 Korean font '{path}' does not support required character '{character}' (U+{(int)character:X4}).");
            }
        }

        private static void IntegrateMainMenu()
        {
            string path = SceneCatalog.GetPath(SceneId.MainMenu);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RemoveOwnedRoot(MainMenuRootName);
            GameObject root = CreateCanvasRoot(MainMenuRootName, 50);
            EnsureSingleEventSystem(root.transform);

            Stage6UIController ui = root.AddComponent<Stage6UIController>();
            CanvasGroup title = CreatePanel("Title Panel", root.transform, true);
            CreateImage("Backdrop", title.transform, Backdrop, true);
            Text heading = CreateText("Title", title.transform, "둘만의 작은 세계", 46, TextAnchor.MiddleCenter);
            SetRect(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700f, 90f), new Vector2(0f, 230f));
            Button newGame = CreateButton("New Game Button", title.transform, "새 게임", new Vector2(0f, 80f));
            Button continueGame = CreateButton("Continue Button", title.transform, "이어하기", new Vector2(0f, -10f));
            continueGame.interactable = false;
            Button settings = CreateButton("Settings Button", title.transform, "설정", new Vector2(0f, -100f));
            Button quit = CreateButton("Quit Button", title.transform, "종료", new Vector2(0f, -190f));

            SettingsBundle settingsBundle = CreateSettingsPanel(root.transform);
            CanvasGroup gameplay = CreatePanel("Gameplay Placeholder", root.transform, false);
            CanvasGroup inspection = CreatePanel("Inspection Placeholder", root.transform, false);
            CanvasGroup paused = CreatePanel("Pause Placeholder", root.transform, false);
            LoadingBundle loading = CreateLoadingPanel(root.transform);
            ui.Configure(title, settingsBundle.group, gameplay, inspection, paused, loading.group,
                newGame, continueGame, settings, quit, null, null, null, false);
            root.AddComponent<Stage6SettingsBinding>().Configure(ui, settingsBundle.view);

            TitleScreenController flow = UnityEngine.Object.FindFirstObjectByType<TitleScreenController>();
            if (flow == null) throw new InvalidOperationException("MainMenu TitleScreenController is missing.");
            flow.ConfigureStage6(ui);
            ValidateCommon(root, ui, MainMenuRootName);
            Save(scene, path);
        }

        private static void IntegrateRealityRoom()
        {
            string path = SceneCatalog.GetPath(SceneId.RealityRoom);
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RemoveOwnedRoot(RealityRootName);
            GameObject root = CreateCanvasRoot(RealityRootName, 40);
            EnsureSingleEventSystem(root.transform);

            Stage6UIController ui = root.AddComponent<Stage6UIController>();
            CanvasGroup title = CreatePanel("Title Placeholder", root.transform, false);
            SettingsBundle settings = CreateSettingsPanel(root.transform);
            CanvasGroup gameplay = CreatePanel("Gameplay HUD", root.transform, true);
            CreateText("Interaction Contract Hint", gameplay.transform, "E  상호작용", 18,
                TextAnchor.MiddleRight).rectTransform.anchoredPosition = new Vector2(-36f, 32f);

            InspectionBundle inspection = CreateInspectionPanel(root.transform);
            CanvasGroup paused = CreatePanel("Pause Panel", root.transform, false);
            Image pauseBackdrop = CreateImage("Pause Backdrop", paused.transform, new Color(0f, 0f, 0f, 0.72f), true);
            Image pauseCard = CreateImage("Pause Card", pauseBackdrop.transform, Panel, false);
            SetRect(pauseCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 560f), Vector2.zero);
            CreateText("Pause Title", pauseCard.transform, "일시 정지", 38, TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(0f, 205f);
            Button resume = CreateButton("Resume Button", pauseCard.transform, "계속하기", new Vector2(0f, 100f));
            Button pauseSettings = CreateButton("Pause Settings Button", pauseCard.transform, "설정", new Vector2(0f, 10f));
            Button mainMenu = CreateButton("Main Menu Button", pauseCard.transform, "메인 메뉴", new Vector2(0f, -80f));
            Button quit = CreateButton("Quit Button", pauseCard.transform, "종료", new Vector2(0f, -170f));
            LoadingBundle loading = CreateLoadingPanel(root.transform);
            NotificationQueueView notifications = CreateNotification(root.transform);

            ui.Configure(title, settings.group, gameplay, inspection.group, paused, loading.group,
                null, null, null, quit, resume, pauseSettings, mainMenu, true);
            root.AddComponent<Stage6SettingsBinding>().Configure(ui, settings.view);

            RealityRoomController flow = UnityEngine.Object.FindFirstObjectByType<RealityRoomController>();
            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            PlayerInteractionDetector detector = UnityEngine.Object.FindFirstObjectByType<PlayerInteractionDetector>();
            if (flow == null || player == null || detector == null)
                throw new InvalidOperationException("RealityRoom Stage 2-5 flow/player/interaction components are incomplete.");
            flow.ConfigureStage6(ui, inspection.view, notifications, loading.view, player, detector);
            ValidateCommon(root, ui, RealityRootName);
            if (UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None).Length != 6)
                throw new InvalidOperationException("Stage 6 must preserve all six Stage 5 interactables.");
            Save(scene, path);
        }

        private static GameObject CreateCanvasRoot(string name, int sortingOrder)
        {
            var root = new GameObject(name, typeof(RectTransform));
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            GameObject safeArea = new GameObject("Safe Area", typeof(RectTransform));
            safeArea.transform.SetParent(root.transform, false);
            Stretch((RectTransform)safeArea.transform);
            safeArea.AddComponent<SafeAreaFitter>();
            return root;
        }

        private static SettingsBundle CreateSettingsPanel(Transform parent)
        {
            CanvasGroup group = CreatePanel("Settings Panel", parent, false);
            Image backdrop = CreateImage("Settings Backdrop", group.transform, new Color(0f, 0f, 0f, 0.75f), true);
            Image card = CreateImage("Settings Card", backdrop.transform, Panel, false);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(720f, 820f), Vector2.zero);
            CreateText("Settings Title", card.transform, "설정", 38, TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(0f, 345f);
            Slider master = CreateLabeledSlider(card.transform, "전체 음량", 245f);
            Slider music = CreateLabeledSlider(card.transform, "음악", 155f);
            Slider sfx = CreateLabeledSlider(card.transform, "효과음", 65f);
            Slider voice = CreateLabeledSlider(card.transform, "음성", -25f);
            Toggle fullscreen = CreateToggle(card.transform, "전체 화면", -120f);
            InputField width = CreateInput(card.transform, "Width", "1920", new Vector2(-130f, -215f));
            InputField height = CreateInput(card.transform, "Height", "1080", new Vector2(130f, -215f));
            Button apply = CreateButton("Apply Button", card.transform, "적용", new Vector2(-180f, -320f));
            Button cancel = CreateButton("Cancel Button", card.transform, "취소", new Vector2(180f, -320f));
            SettingsPanelView view = group.gameObject.AddComponent<SettingsPanelView>();
            view.Configure(master, music, sfx, voice, fullscreen, width, height, apply, cancel);
            return new SettingsBundle { group = group, view = view };
        }

        private static InspectionBundle CreateInspectionPanel(Transform parent)
        {
            CanvasGroup group = CreatePanel("Inspection Panel", parent, false);
            Image backdrop = CreateImage("Inspection Backdrop", group.transform, new Color(0f, 0f, 0f, 0.78f), true);
            Image card = CreateImage("Inspection Card", backdrop.transform, Panel, false);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 580f), Vector2.zero);
            Text title = CreateText("Inspection Title", card.transform, string.Empty, 36, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(780f, 70f), new Vector2(0f, 190f));
            Text body = CreateText("Inspection Body", card.transform, string.Empty, 25, TextAnchor.UpperLeft);
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 260f), new Vector2(0f, 0f));
            Button close = CreateButton("Inspection Close Button", card.transform, "닫기", new Vector2(0f, -220f));
            InspectionView view = group.gameObject.AddComponent<InspectionView>();
            view.Configure(group, title, body, close);
            return new InspectionBundle { group = group, view = view };
        }

        private static LoadingBundle CreateLoadingPanel(Transform parent)
        {
            CanvasGroup group = CreatePanel("Loading Panel", parent, false);
            Image backdrop = CreateImage("Loading Backdrop", group.transform, Backdrop, true);
            Text status = CreateText("Loading Status", backdrop.transform, "불러오는 중...", 26, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 60f), new Vector2(0f, 45f));
            Slider progress = CreateSlider(backdrop.transform, new Vector2(0f, -25f), new Vector2(520f, 20f));
            Stage6LoadingView view = group.gameObject.AddComponent<Stage6LoadingView>();
            view.Configure(group, progress, status);
            return new LoadingBundle { group = group, view = view };
        }

        private static NotificationQueueView CreateNotification(Transform parent)
        {
            CanvasGroup group = CreatePanel("Notification Panel", parent, false);
            group.interactable = false;
            group.blocksRaycasts = false;
            Text message = CreateText("Notification Message", group.transform, string.Empty, 24, TextAnchor.MiddleCenter);
            SetRect(message.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(760f, 80f), Vector2.zero);
            NotificationQueueView view = group.gameObject.AddComponent<NotificationQueueView>();
            view.Configure(group, message);
            return view;
        }

        private static CanvasGroup CreatePanel(string name, Transform parent, bool visible)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent.Find("Safe Area") ?? parent, false);
            Stretch((RectTransform)gameObject.transform);
            CanvasGroup group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            return group;
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool stretch)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            if (stretch) Stretch(image.rectTransform);
            return image;
        }

        private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 60f);
            Text text = gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
        {
            Image image = CreateImage(name, parent, Accent, false);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(340f, 68f), position);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText("Label", image.transform, label, 23, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Slider CreateLabeledSlider(Transform parent, string label, float y)
        {
            Text title = CreateText(label + " Label", parent, label, 21, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, 45f), new Vector2(-235f, y));
            return CreateSlider(parent, new Vector2(90f, y), new Vector2(410f, 22f));
        }

        private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
        {
            Image background = CreateImage("Slider", parent, new Color(1f, 1f, 1f, 0.2f), false);
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), size, position);
            Slider slider = background.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Image fill = CreateImage("Fill", background.transform, Accent, true);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string label, float y)
        {
            Image background = CreateImage("Fullscreen Toggle", parent, new Color(1f, 1f, 1f, 0.2f), false);
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(42f, 42f), new Vector2(-190f, y));
            Toggle toggle = background.gameObject.AddComponent<Toggle>();
            Image check = CreateImage("Checkmark", background.transform, Accent, true);
            toggle.targetGraphic = background;
            toggle.graphic = check;
            Text text = CreateText("Fullscreen Label", parent, label, 22, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(300f, 50f), new Vector2(40f, y));
            return toggle;
        }

        private static InputField CreateInput(Transform parent, string name, string value, Vector2 position)
        {
            Image background = CreateImage(name, parent, new Color(1f, 1f, 1f, 0.14f), false);
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220f, 55f), position);
            InputField field = background.gameObject.AddComponent<InputField>();
            Text text = CreateText("Text", background.transform, value, 22, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            field.textComponent = text;
            field.contentType = InputField.ContentType.IntegerNumber;
            field.text = value;
            return field;
        }

        private static void EnsureSingleEventSystem(Transform owner)
        {
            EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length > 1) throw new InvalidOperationException("Scene contains more than one EventSystem before Stage 6 integration.");
            if (systems.Length == 1) return;
            var eventSystem = new GameObject("Stage 6 EventSystem");
            eventSystem.transform.SetParent(owner, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void ValidateCommon(GameObject root, Stage6UIController ui, string expectedName)
        {
            if (root.name != expectedName || ui == null) throw new InvalidOperationException("Stage 6 owned root is incomplete.");
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1920f, 1080f) ||
                !Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f))
                throw new InvalidOperationException("Stage 6 CanvasScaler contract is invalid.");
            if (UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Stage 6 requires exactly one EventSystem per scene.");
        }

        private static void RemoveOwnedRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Could not save Stage 6 scene '{path}'.");
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
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private struct SettingsBundle { public CanvasGroup group; public SettingsPanelView view; }
        private struct InspectionBundle { public CanvasGroup group; public InspectionView view; }
        private struct LoadingBundle { public CanvasGroup group; public Stage6LoadingView view; }
    }
}
