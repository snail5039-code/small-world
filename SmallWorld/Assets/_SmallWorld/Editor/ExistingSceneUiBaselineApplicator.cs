using System;
using System.Collections.Generic;
using System.Linq;
using SmallWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Editor
{
    public static class ExistingSceneUiBaselineApplicator
    {
        public const string MenuPath = "Small World/UI/Apply Baseline To Existing Scenes";
        public static readonly string[] TargetScenes =
        {
            "Assets/_SmallWorld/Scenes/00_Boot.unity",
            "Assets/_SmallWorld/Scenes/01_MainMenu.unity",
            "Assets/_SmallWorld/Scenes/02_RealityRoom.unity",
            "Assets/_SmallWorld/Scenes/03_FirstMemory.unity",
            "Assets/_SmallWorld/Scenes/04_StoryRoute.unity"
        };

        private static readonly string[] ModalLayerTokens =
        {
            "Pause", "Settings", "Dialogue", "Record", "Journal", "Inventory", "Inspection",
            "Puzzle", "Save", "Loading"
        };

        private static readonly Dictionary<string, string> LocalizedText =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Paused"] = "일시정지",
                ["Press Esc to return to the story."] = "Esc를 누르면 이야기로 돌아갑니다.",
                ["LOADING"] = "불러오는 중...",
                ["ESC  -  RETURN TO MENU"] = "Esc  -  메뉴로 돌아가기",
                ["No route records have been collected yet."] = "아직 수집한 기록이 없습니다.",
                ["Button"] = "확인",
                ["New Game"] = "새 게임",
                ["Continue"] = "이어하기",
                ["Settings"] = "설정",
                ["Quit"] = "종료",
                ["Resume"] = "계속하기",
                ["Return to Title"] = "타이틀로 돌아가기",
                ["Save"] = "저장",
                ["Load"] = "불러오기",
                ["Close"] = "닫기"
            };

        [MenuItem(MenuPath)]
        public static void ApplyAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;
            foreach (string path in TargetScenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ApplyToLoadedScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrWhiteSpace(previous) && TargetScenes.Contains(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[SmallWorld] Existing scene UI baseline applied to 00_Boot through 04_StoryRoute.");
        }

        public static void ApplyToLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded scene is required.");
            foreach (GameObject item in FindInScene<GameObject>(scene))
            {
                if (!item.activeInHierarchy || item.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                item.name = item.name.Replace("Placeholder", "Runtime Panel");
                EditorUtility.SetDirty(item);
            }
            Canvas[] canvases = FindInScene<Canvas>(scene);
            foreach (Canvas canvas in canvases)
            {
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ??
                    Undo.AddComponent<CanvasScaler>(canvas.gameObject);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                if (canvas.renderMode != RenderMode.WorldSpace) EnsureSafeArea(canvas);
                EditorUtility.SetDirty(scaler);
            }

            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Text text in FindInScene<Text>(scene))
            {
                string value = text.text ?? string.Empty;
                string trimmed = value.Trim();
                KeyValuePair<string, string> replacement = LocalizedText.FirstOrDefault(item =>
                    string.Equals(item.Key, trimmed, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(replacement.Key)) value = replacement.Value;
                if (string.IsNullOrWhiteSpace(value) && ContainsAny(text.name, "Inspection Title"))
                    value = "조사";
                text.text = value;
                if (text.font == null) text.font = fallback;
                text.fontSize = Mathf.Max(14, text.fontSize);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = Mathf.Max(12, text.fontSize);
                text.lineSpacing = Mathf.Max(1f, text.lineSpacing);
                if (text.text.Contains("�")) text.text = text.text.Replace("�", string.Empty);
                Image card = FindOpaqueBackground(text.transform.parent);
                if (card != null && Contrast(text.color, card.color) < (text.fontSize >= 18 ? 3f : 4.5f))
                    text.color = Contrast(Color.white, card.color) >= Contrast(Color.black, card.color)
                        ? Color.white
                        : Color.black;
                EditorUtility.SetDirty(text);
            }

            foreach (Button button in FindInScene<Button>(scene)) ApplyButton(button, fallback);
            ApplySemanticTheme(scene);
            OrderScreenLayers(scene);
            foreach (CanvasGroup group in FindInScene<CanvasGroup>(scene))
            {
                if (!group.gameObject.activeInHierarchy || group.alpha <= 0.01f)
                {
                    group.interactable = false;
                    group.blocksRaycasts = false;
                    EditorUtility.SetDirty(group);
                }
            }

            EnsureRealityModalOwnership(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void EnsureSafeArea(Canvas canvas)
        {
            if (canvas.GetComponentInChildren<SafeAreaFitter>(true) != null) return;
            var safe = new GameObject("Safe Area", typeof(RectTransform), typeof(SafeAreaFitter));
            Undo.RegisterCreatedObjectUndo(safe, "Create UI safe area");
            RectTransform rect = safe.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Transform[] children = canvas.transform.Cast<Transform>().Where(child => child != rect).ToArray();
            foreach (Transform child in children) Undo.SetTransformParent(child, rect, "Move UI under safe area");
        }

        private static void ApplyButton(Button button, Font fallback)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                Vector2 size = rect.rect.size;
                if (Mathf.Abs(size.x) < 44f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 44f);
                if (Mathf.Abs(size.y) < 44f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44f);
                EditorUtility.SetDirty(rect);
            }
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            if (string.IsNullOrWhiteSpace(label.text)) label.text = LocalizeObjectName(button.name);
            if (label.font == null) label.font = fallback;
            label.fontSize = Mathf.Max(16, label.fontSize);
            Image background = button.GetComponent<Image>();
            if (background != null)
            {
                background.color = SmallWorldUiTheme.SurfaceRaised;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.78f, 0.57f, 1f);
                colors.pressedColor = new Color(0.86f, 0.48f, 0.2f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.45f, 0.48f, 0.52f, 0.65f);
                button.colors = colors;
                label.color = SmallWorldUiTheme.PrimaryText;
                EditorUtility.SetDirty(background);
                EditorUtility.SetDirty(button);
            }
            EditorUtility.SetDirty(label);
        }

        private static void ApplySemanticTheme(Scene scene)
        {
            foreach (Image image in FindInScene<Image>(scene))
            {
                string name = image.name ?? string.Empty;
                if (ContainsAny(name, "Backdrop", "Shade", "Dimmer"))
                    image.color = new Color(0.015f, 0.02f, 0.03f, Mathf.Max(0.68f, image.color.a));
                else if (ContainsAny(name, "Card", "Dialog Box", "Dialogue Box"))
                    image.color = SmallWorldUiTheme.SurfaceRaised;
                else continue;
                EditorUtility.SetDirty(image);
            }

            foreach (Text text in FindInScene<Text>(scene))
            {
                string name = text.name ?? string.Empty;
                if (ContainsAny(name, "Title", "Heading"))
                {
                    int size = Mathf.Max(20, text.fontSize);
                    SmallWorldUiTheme.ApplyText(text, SmallWorldTextRole.Title);
                    text.fontSize = size;
                    text.resizeTextMaxSize = size;
                }
                else if (ContainsAny(name, "Prompt"))
                {
                    int size = Mathf.Max(17, text.fontSize);
                    SmallWorldUiTheme.ApplyText(text, SmallWorldTextRole.Prompt);
                    text.fontSize = size;
                    text.resizeTextMaxSize = size;
                }
                else if (ContainsAny(name, "Feedback", "Status", "Hint"))
                {
                    int size = Mathf.Max(15, text.fontSize);
                    SmallWorldUiTheme.ApplyText(text, SmallWorldTextRole.Feedback);
                    text.fontSize = size;
                    text.resizeTextMaxSize = size;
                }
                else continue;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                EditorUtility.SetDirty(text);
            }
        }

        private static void OrderScreenLayers(Scene scene)
        {
            foreach (Canvas canvas in FindInScene<Canvas>(scene).Where(item => item.renderMode != RenderMode.WorldSpace))
            {
                Transform parent = canvas.GetComponentInChildren<SafeAreaFitter>(true)?.transform ?? canvas.transform;
                Transform[] children = parent.Cast<Transform>().ToArray();
                foreach (Transform child in children.OrderBy(LayerRank).ThenBy(item => item.GetSiblingIndex()))
                    child.SetAsLastSibling();
            }
        }

        private static int LayerRank(Transform item)
        {
            string name = item.name ?? string.Empty;
            if (ContainsAny(name, "HUD", "Objective", "Guidance")) return 0;
            if (ContainsAny(name, "Prompt", "Feedback")) return 1;
            return ModalLayerTokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) ? 2 : 0;
        }

        private static bool ContainsAny(string value, params string[] tokens) =>
            tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);

        private static void EnsureRealityModalOwnership(Scene scene)
        {
            if (!scene.path.EndsWith("02_RealityRoom.unity", StringComparison.OrdinalIgnoreCase)) return;
            string[] names = { "Pause Panel", "Settings Panel", "Inspection Panel", "Stage 7 Dialogue UI",
                "Stage 8 Record UI", "Stage 9 Photo Puzzle UI", "Stage 10 Save Integration" };
            foreach (string name in names)
            {
                GameObject root = FindInScene<GameObject>(scene).FirstOrDefault(item => item.name == name);
                if (root == null || root.GetComponentInChildren<CanvasGroup>(true) != null) continue;
                CanvasGroup group = Undo.AddComponent<CanvasGroup>(root);
                bool visible = root.activeInHierarchy;
                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
                EditorUtility.SetDirty(group);
            }
        }

        private static string LocalizeObjectName(string value)
        {
            if (LocalizedText.TryGetValue((value ?? string.Empty).Replace(" Button", string.Empty), out string localized))
                return localized;
            return "확인";
        }

        private static T[] FindInScene<T>(Scene scene) where T : Object =>
            Resources.FindObjectsOfTypeAll<T>().Where(item =>
                item != null && (item is Component component ? component.gameObject.scene == scene :
                    item is GameObject gameObject && gameObject.scene == scene)).ToArray();

        private static float Contrast(Color first, Color second)
        {
            float bright = Mathf.Max(Luminance(first), Luminance(second));
            float dark = Mathf.Min(Luminance(first), Luminance(second));
            return (bright + 0.05f) / (dark + 0.05f);
        }

        private static Image FindOpaqueBackground(Transform current)
        {
            while (current != null)
            {
                Image image = current.GetComponent<Image>();
                if (image != null && image.color.a >= 0.75f) return image;
                current = current.parent;
            }
            return null;
        }

        private static float Luminance(Color color) => 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        private static float Linear(float value) => value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
    }
}
