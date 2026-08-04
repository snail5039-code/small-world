using System;
using SmallWorld.Core;
using SmallWorld.Flow;
using SmallWorld.Player;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage8RecordUIGenerator
    {
        public const string RootName = "Stage 8 Record UI";
        private static readonly Color Panel = new Color(0.055f, 0.07f, 0.1f, 0.985f);
        private static readonly Color Accent = new Color(0.25f, 0.76f, 0.78f, 1f);
        private static Font font;

        [MenuItem("Small World/Stage 8/Generate Record Integration")]
        public static void GenerateFromMenu()
        {
            try { GenerateAndValidate(); Debug.Log("[SmallWorld] Stage 8 record UI generated successfully."); }
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

        public static void GenerateAndValidate()
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(Stage6UIGenerator.StandardKoreanFontPath);
            if (font == null) throw new InvalidOperationException("Stage 8 requires the licensed Stage 6 Korean font.");
            Scene scene = EditorSceneManager.OpenScene(SceneCatalog.GetPath(SceneId.RealityRoom), OpenSceneMode.Single);
            GameObject stage6 = GameObject.Find(Stage6UIGenerator.RealityRootName);
            if (stage6 == null) throw new InvalidOperationException("Stage 6 RealityRoom UI is missing.");
            GameObject old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            Transform parent = stage6.transform.Find("Safe Area") ?? stage6.transform;
            CanvasGroup panel = Group(RootName, parent, false);
            Image shade = Image("Record Backdrop", panel.transform, new Color(0f, 0f, 0f, 0.86f));
            Stretch(shade.rectTransform);
            Image card = Image("Record Card", shade.transform, Panel);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 880f), Vector2.zero);

            Text heading = Text("Record Heading", card.transform, "기록", 42, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1180f, 70f), new Vector2(-20f, 365f));
            Text hint = Text("Record Hint", card.transform, "TAB 또는 ESC로 닫기", 19, TextAnchor.MiddleRight);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(500f, 48f), new Vector2(420f, 365f));

            Button inventory = Button("Inventory Tab", card.transform, "인벤토리", new Vector2(-470f, 285f), new Vector2(260f, 62f));
            Button memories = Button("Memory Tab", card.transform, "기억 조각", new Vector2(-190f, 285f), new Vector2(260f, 62f));
            Button records = Button("Records Tab", card.transform, "조사 · 사진 · 이름", new Vector2(160f, 285f), new Vector2(400f, 62f));
            Button close = Button("Close Record Button", card.transform, "닫기", new Vector2(560f, 285f), new Vector2(180f, 62f));

            Text tabTitle = Text("Tab Title", card.transform, "인벤토리", 30, TextAnchor.MiddleLeft);
            SetRect(tabTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1260f, 58f), new Vector2(0f, 205f));
            Image divider = Image("Record Divider", card.transform, Accent);
            SetRect(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2f, 500f), new Vector2(-260f, -65f));
            Text list = Text("Record List", card.transform, string.Empty, 24, TextAnchor.UpperLeft);
            SetRect(list.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(410f, 500f), new Vector2(-505f, -65f));
            Text details = Text("Record Details", card.transform, string.Empty, 23, TextAnchor.UpperLeft);
            SetRect(details.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(850f, 500f), new Vector2(205f, -65f));

            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            Stage6UIController stage6Controller = stage6.GetComponent<Stage6UIController>();
            Stage7DialogueView dialogue = UnityEngine.Object.FindFirstObjectByType<Stage7DialogueView>();
            RealityRoomController room = UnityEngine.Object.FindFirstObjectByType<RealityRoomController>();
            if (player == null || stage6Controller == null || dialogue == null || room == null)
                throw new InvalidOperationException("Stage 8 requires the Stage 6/7 RealityRoom integration.");
            Stage8RecordView view = panel.gameObject.AddComponent<Stage8RecordView>();
            view.Configure(panel, tabTitle, list, details, inventory, memories, records, close,
                player, stage6Controller, dialogue);
            room.ConfigureStage8(view);

            Validate(stage6, view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SceneCatalog.GetPath(SceneId.RealityRoom)))
                throw new InvalidOperationException("Could not save Stage 8 RealityRoom integration.");
            AssetDatabase.SaveAssets();
        }

        private static void Validate(GameObject stage6, Stage8RecordView view)
        {
            CanvasScaler scaler = stage6.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.referenceResolution != new Vector2(1920f, 1080f))
                throw new InvalidOperationException("Stage 8 responsive UI contract is incomplete.");
            if (stage6.GetComponentInChildren<SafeAreaFitter>(true) == null || view == null)
                throw new InvalidOperationException("Stage 8 safe-area integration is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None).Length != 6)
                throw new InvalidOperationException("Stage 8 must preserve all six Stage 5 interactables.");
        }

        private static CanvasGroup Group(string name, Transform parent, bool visible)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            return group;
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Text(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            Image image = Image(name, parent, Accent);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), size, position);
            Button button = image.gameObject.AddComponent<Button>();
            Text text = Text("Label", image.transform, label, 22, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
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
    }
}

