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
            "Assets/_SmallWorld/Scenes/03_FirstMemory.unity"
        };

        private static readonly Dictionary<string, string> LocalizedText =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Paused"] = "일시정지",
                ["Press Esc to return to the story."] = "Esc를 누르면 이야기로 돌아갑니다.",
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
            Debug.Log("[SmallWorld] Existing scene UI baseline applied to 00_Boot through 03_FirstMemory.");
        }

        public static void ApplyToLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded scene is required.");
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
                foreach (KeyValuePair<string, string> replacement in LocalizedText)
                    if (value.IndexOf(replacement.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        value = value.Replace(replacement.Key, replacement.Value);
                text.text = value;
                if (text.font == null) text.font = fallback;
                text.fontSize = Mathf.Max(12, text.fontSize);
                if (text.text.Contains("�")) text.text = text.text.Replace("�", string.Empty);
                Image card = FindOpaqueBackground(text.transform.parent);
                if (card != null && Contrast(text.color, card.color) < (text.fontSize >= 18 ? 3f : 4.5f))
                    text.color = Contrast(Color.white, card.color) >= Contrast(Color.black, card.color)
                        ? Color.white
                        : Color.black;
                EditorUtility.SetDirty(text);
            }

            foreach (Button button in FindInScene<Button>(scene)) ApplyButton(button, fallback);
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
            label.fontSize = Mathf.Max(14, label.fontSize);
            Image background = button.GetComponent<Image>();
            if (background != null && Contrast(label.color, background.color) < 4.5f)
            {
                background.color = new Color(0.06f, 0.08f, 0.11f, Mathf.Max(0.9f, background.color.a));
                label.color = Color.white;
                EditorUtility.SetDirty(background);
            }
            EditorUtility.SetDirty(label);
        }

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
