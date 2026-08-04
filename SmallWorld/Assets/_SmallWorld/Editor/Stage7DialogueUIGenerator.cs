using System;
using SmallWorld.Core;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Player;
using SmallWorld.UI.Stage7;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage7DialogueUIGenerator
    {
        public const string RootName = "Stage 7 Dialogue UI";
        private static readonly Color Panel = new Color(0.07f, 0.085f, 0.12f, 0.98f);
        private static readonly Color Accent = new Color(0.25f, 0.76f, 0.78f, 1f);
        private static Font font;

        [MenuItem("Small World/Stage 7/Generate Dialogue Integration")]
        public static void GenerateFromMenu()
        {
            try { GenerateAndValidate(); Debug.Log("[SmallWorld] Stage 7 dialogue UI generated successfully."); }
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
            if (font == null) throw new InvalidOperationException("Stage 7 requires the licensed Stage 6 Korean font.");
            Scene scene = EditorSceneManager.OpenScene(SceneCatalog.GetPath(SceneId.RealityRoom), OpenSceneMode.Single);
            GameObject stage6 = GameObject.Find(Stage6UIGenerator.RealityRootName);
            if (stage6 == null) throw new InvalidOperationException("Stage 6 RealityRoom UI must exist before Stage 7 integration.");
            GameObject old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            Transform parent = stage6.transform.Find("Safe Area") ?? stage6.transform;
            var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            Stretch((RectTransform)root.transform);
            CanvasGroup dialogue = root.GetComponent<CanvasGroup>();

            Image shade = Image("Dialogue Shade", root.transform, new Color(0f, 0f, 0f, 0.22f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            Image card = Image("Dialogue Card", root.transform, Panel);
            SetRect(card.rectTransform, new Vector2(0.5f, 0f), new Vector2(1500f, 410f), new Vector2(0f, 245f));
            Text speaker = Text("Speaker Name", card.transform, "미라", 29, TextAnchor.MiddleLeft);
            SetRect(speaker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1280f, 52f), new Vector2(0f, 145f));
            speaker.color = Accent;
            Text body = Text("Dialogue Body", card.transform, string.Empty, 28, TextAnchor.UpperLeft);
            SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1280f, 105f), new Vector2(0f, 55f));
            Text relationship = Text("Relationship Status", card.transform, "미라와의 관계  0", 19, TextAnchor.MiddleRight);
            SetRect(relationship.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(500f, 42f), new Vector2(390f, 145f));

            Button[] choices = new Button[2];
            choices[0] = Button("Choice 1", card.transform, string.Empty, new Vector2(0f, -35f), new Vector2(1160f, 58f));
            choices[1] = Button("Choice 2", card.transform, string.Empty, new Vector2(0f, -105f), new Vector2(1160f, 58f));
            Button advance = Button("Advance Button", card.transform, "계속", new Vector2(510f, -120f), new Vector2(220f, 58f));
            Button skip = Button("Skip Button", card.transform, "건너뛰기", new Vector2(-520f, -165f), new Vector2(190f, 48f));
            Button history = Button("History Button", card.transform, "기록", new Vector2(-315f, -165f), new Vector2(150f, 48f));
            Toggle autoToggle = Toggle(card.transform, "자동 진행", new Vector2(-100f, -165f));

            CanvasGroup historyGroup = Group("Dialogue History Panel", parent, false);
            Image historyBackdrop = Image("History Backdrop", historyGroup.transform, new Color(0f, 0f, 0f, 0.82f));
            Stretch(historyBackdrop.rectTransform);
            Image historyCard = Image("History Card", historyBackdrop.transform, Panel);
            SetRect(historyCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1120f, 820f), Vector2.zero);
            Text("History Title", historyCard.transform, "대화 기록", 36, TextAnchor.MiddleCenter).rectTransform.anchoredPosition = new Vector2(0f, 350f);
            Text historyText = Text("History Log", historyCard.transform, string.Empty, 23, TextAnchor.UpperLeft);
            SetRect(historyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(960f, 600f), new Vector2(0f, 0f));
            Button close = Button("Close History Button", historyCard.transform, "닫기", new Vector2(0f, -350f), new Vector2(220f, 58f));

            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            if (player == null) throw new InvalidOperationException("RealityRoom player is missing.");
            Stage7DialogueView view = root.AddComponent<Stage7DialogueView>();
            view.Configure(dialogue, speaker, body, relationship, advance, skip, autoToggle, history,
                historyGroup, historyText, close, choices, player);

            Validate(stage6, view);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SceneCatalog.GetPath(SceneId.RealityRoom)))
                throw new InvalidOperationException("Could not save Stage 7 RealityRoom integration.");
            AssetDatabase.SaveAssets();
        }

        private static void Validate(GameObject stage6, Stage7DialogueView view)
        {
            if (stage6.GetComponent<CanvasScaler>() == null || view == null)
                throw new InvalidOperationException("Stage 6/7 responsive UI contract is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None).Length != 6)
                throw new InvalidOperationException("Stage 7 must preserve all six Stage 5 interactables.");
            DialogueDefinition demo = Stage7DemoDialogue.Create();
            var state = new DialogueState();
            if (!demo.CanShowInMenu(state) || demo.Nodes.Count != 5)
                throw new InvalidOperationException("Stage 7 demo dialogue contract is invalid.");
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
            text.rectTransform.sizeDelta = new Vector2(500f, 60f);
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            Image image = Image(name, parent, Accent);
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), size, position);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = Text("Label", image.transform, label, 22, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Toggle Toggle(Transform parent, string label, Vector2 position)
        {
            Image background = Image("Auto Advance Toggle", parent, new Color(1f, 1f, 1f, 0.2f));
            SetRect(background.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(38f, 38f), position);
            Toggle toggle = background.gameObject.AddComponent<Toggle>();
            Image check = Image("Checkmark", background.transform, Accent);
            Stretch(check.rectTransform);
            toggle.targetGraphic = background;
            toggle.graphic = check;
            Text text = Text("Auto Advance Label", parent, label, 19, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(180f, 44f), position + new Vector2(115f, 0f));
            return toggle;
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
