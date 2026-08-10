using System;
using SmallWorld.Core;
using SmallWorld.Flow;
using SmallWorld.Player;
using SmallWorld.Puzzle.Stage9Integration;
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
    public static class Stage9PhotoPuzzleGenerator
    {
        public const string RootName = "Stage 9 Photo Puzzle UI";
        private static readonly Color Panel = new Color(0.06f, 0.075f, 0.10f, 0.99f);
        private static readonly Color Accent = new Color(0.72f, 0.48f, 0.24f, 1f);
        private static Font font;

        [MenuItem("Small World/Stage 9/Generate Photo Puzzle Integration")]
        public static void GenerateFromMenu()
        {
            try { GenerateAndValidate(); Debug.Log("[SmallWorld] Stage 9 photo puzzle generated successfully."); }
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
            if (font == null) throw new InvalidOperationException("Stage 9 requires the licensed Stage 6 Korean font.");
            Scene scene = EditorSceneManager.OpenScene(SceneCatalog.GetPath(SceneId.RealityRoom), OpenSceneMode.Single);
            GameObject stage6 = GameObject.Find(Stage6UIGenerator.RealityRootName);
            GameObject recordRoot = GameObject.Find(Stage8RecordUIGenerator.RootName);
            GameObject frame = GameObject.Find("Empty Frame");
            GameObject roof = GameObject.Find("Model House Roof");
            if (stage6 == null || recordRoot == null || frame == null || roof == null)
                throw new InvalidOperationException("Stage 9 requires the Stage 4 and Stage 6-8 RealityRoom integration.");
            GameObject old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            Transform parent = stage6.transform.Find("Safe Area") ?? stage6.transform;
            CanvasGroup panel = Group(RootName, parent, false);
            Image shade = Image("Photo Puzzle Backdrop", panel.transform, new Color(0f, 0f, 0f, 0.88f));
            Stretch(shade.rectTransform);
            Image card = Image("Photo Puzzle Card", shade.transform, Panel);
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1280f, 760f), Vector2.zero);
            Text heading = Text("Photo Puzzle Heading", card.transform, "사진 조각 순서 맞추기", 40, TextAnchor.MiddleCenter);
            SetRect(heading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1050f, 70f), new Vector2(0f, 285f));
            Text instruction = Text("Photo Puzzle Instruction", card.transform, "사진의 흔적이 이어지는 순서대로 조각을 선택하세요.", 24, TextAnchor.MiddleCenter);
            SetRect(instruction.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1050f, 60f), new Vector2(0f, 205f));
            Text progress = Text("Photo Puzzle Progress", card.transform, "진행 0 / 3", 22, TextAnchor.MiddleCenter);
            SetRect(progress.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600f, 50f), new Vector2(0f, 145f));

            Button[] pieces =
            {
                Button("Door Photo Piece", card.transform, "현관 조각\n어두운 문", new Vector2(-360f, 10f), new Vector2(280f, 240f)),
                Button("Window Photo Piece", card.transform, "창문 조각\n밝은 테두리", Vector2.zero, new Vector2(280f, 240f)),
                Button("Roof Photo Piece", card.transform, "지붕 조각\n붉은 능선", new Vector2(360f, 10f), new Vector2(280f, 240f))
            };
            Text feedback = Text("Photo Puzzle Feedback", card.transform, "사진의 흔적을 따라 조각을 골라 보세요.", 23, TextAnchor.MiddleCenter);
            SetRect(feedback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1060f, 80f), new Vector2(0f, -180f));
            Button close = Button("Close Photo Puzzle Button", card.transform, "닫기", new Vector2(0f, -285f), new Vector2(220f, 60f));

            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            Stage6UIController stage6Controller = stage6.GetComponent<Stage6UIController>();
            Stage7DialogueView dialogue = UnityEngine.Object.FindFirstObjectByType<Stage7DialogueView>();
            Stage8RecordView records = recordRoot.GetComponent<Stage8RecordView>();
            RealityRoomController room = UnityEngine.Object.FindFirstObjectByType<RealityRoomController>();
            if (player == null || stage6Controller == null || dialogue == null || records == null || room == null)
                throw new InvalidOperationException("Stage 9 input and record dependencies are incomplete.");

            PhotoPuzzleView view = panel.gameObject.AddComponent<PhotoPuzzleView>();
            view.Configure(panel, instruction, feedback, progress, pieces, close, player, stage6Controller,
                dialogue, records, roof);
            InspectableInteractable oldFrame = frame.GetComponent<InspectableInteractable>();
            if (oldFrame != null) UnityEngine.Object.DestroyImmediate(oldFrame);
            PhotoPuzzleInteractable interactable = frame.GetComponent<PhotoPuzzleInteractable>();
            if (interactable == null) interactable = frame.AddComponent<PhotoPuzzleInteractable>();
            interactable.ConfigurePuzzle(view, frame.GetComponentsInChildren<Renderer>(true));
            room.ConfigureStage9(view);

            Validate(view, frame, roof);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SceneCatalog.GetPath(SceneId.RealityRoom)))
                throw new InvalidOperationException("Could not save Stage 9 RealityRoom integration.");
            AssetDatabase.SaveAssets();
        }

        private static void Validate(PhotoPuzzleView view, GameObject frame, GameObject roof)
        {
            if (view == null || view.StorageKey != PhotoPuzzleView.PersistenceKey ||
                frame.GetComponent<PhotoPuzzleInteractable>() == null || roof == null)
                throw new InvalidOperationException("Stage 9 photo puzzle integration is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None).Length != 6)
                throw new InvalidOperationException("Stage 9 must preserve all six Stage 5 interaction targets.");
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
            Text text = Text("Label", image.transform, label, 23, TextAnchor.MiddleCenter);
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
