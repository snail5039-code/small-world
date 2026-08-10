using System;
using SmallWorld.Core;
using SmallWorld.Player;
using SmallWorld.Puzzle.Stage9Integration;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmallWorld.Editor
{
    public static class Stage10SaveIntegrationGenerator
    {
        public const string RootName = "Stage 10 Save Integration";
        public const string WhiteChairName = "White Save Chair";
        private static Font font;

        [MenuItem("Small World/Stage 10/Generate Save Integration")]
        public static void GenerateFromMenu()
        {
            try { GenerateAndValidate(); Debug.Log("[SmallWorld] Stage 10 save integration generated."); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void GenerateFromBatchMode()
        {
            try { GenerateAndValidate(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); throw; }
        }

        public static void GenerateAndValidate()
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(Stage6UIGenerator.StandardKoreanFontPath);
            if (font == null) throw new InvalidOperationException("Stage 10 requires the Stage 6 Korean font.");
            Scene scene = EditorSceneManager.OpenScene(SceneCatalog.GetPath(SceneId.RealityRoom), OpenSceneMode.Single);
            GameObject old = GameObject.Find(RootName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject(RootName);

            FirstPersonPlayerController player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerController>();
            Stage7DialogueView dialogue = UnityEngine.Object.FindFirstObjectByType<Stage7DialogueView>();
            Stage8RecordView records = UnityEngine.Object.FindFirstObjectByType<Stage8RecordView>();
            PhotoPuzzleView puzzle = UnityEngine.Object.FindFirstObjectByType<PhotoPuzzleView>();
            GameObject stage6 = GameObject.Find(Stage6UIGenerator.RealityRootName);
            if (player == null || dialogue == null || records == null || puzzle == null || stage6 == null)
                throw new InvalidOperationException("Stage 10 requires completed Stage 6-9 RealityRoom integration.");

            RealityRoomSaveCoordinator coordinator = root.AddComponent<RealityRoomSaveCoordinator>();
            Stage10ManualSavePanel panel = CreatePanel(stage6.transform.Find("Safe Area") ?? stage6.transform);
            coordinator.Configure(player, dialogue, records, puzzle, panel);

            CreateWhiteChair(root.transform, coordinator);
            GameObject trigger = new GameObject("Reality Entry Auto Save Trigger", typeof(BoxCollider), typeof(Stage10AutoSaveTrigger));
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.position = new Vector3(0f, 1f, -2.5f);
            BoxCollider box = trigger.GetComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(5f, 2f, 1f);
            trigger.GetComponent<Stage10AutoSaveTrigger>().Configure(coordinator, "reality.entry");

            Validate(coordinator, panel);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SceneCatalog.GetPath(SceneId.RealityRoom)))
                throw new InvalidOperationException("Could not save Stage 10 RealityRoom integration.");
            AssetDatabase.SaveAssets();
        }

        private static void CreateWhiteChair(Transform parent, RealityRoomSaveCoordinator coordinator)
        {
            GameObject chair = new GameObject(WhiteChairName, typeof(BoxCollider), typeof(WhiteChairSavePoint));
            chair.transform.SetParent(parent, false); chair.transform.position = new Vector3(-3.8f, 0f, 2.5f);
            BoxCollider collider = chair.GetComponent<BoxCollider>(); collider.center = new Vector3(0f, .85f, 0f); collider.size = new Vector3(1.2f, 1.7f, 1.2f);
            Renderer[] renderers = { Part("Seat", chair.transform, new Vector3(0f, .55f, 0f), new Vector3(1.15f, .18f, 1.05f)),
                Part("Back", chair.transform, new Vector3(0f, 1.2f, .45f), new Vector3(1.15f, 1.2f, .18f)),
                Part("Base", chair.transform, new Vector3(0f, .25f, 0f), new Vector3(.18f, .5f, .18f)) };
            chair.GetComponent<WhiteChairSavePoint>().ConfigureSavePoint(coordinator, renderers);
        }

        private static Renderer Part(string name, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition; part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            Renderer renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            return renderer;
        }

        private static Stage10ManualSavePanel CreatePanel(Transform parent)
        {
            GameObject root = new GameObject("Manual Save Slots", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Stage10ManualSavePanel));
            root.transform.SetParent(parent, false); RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(900f, 620f);
            root.GetComponent<Image>().color = new Color(.05f, .06f, .08f, .98f);
            Text heading = Label("Heading", root.transform, "저장 슬롯", 38); SetRect(heading.rectTransform, new Vector2(0f, 245f), new Vector2(700f, 70f));
            var saves = new Button[3]; var loads = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                Label("Slot " + (i + 1), root.transform, "슬롯 " + (i + 1), 26).rectTransform.anchoredPosition = new Vector2(-280f, 135f - i * 120f);
                saves[i] = MakeButton("Save Slot " + (i + 1), root.transform, "저장", new Vector2(30f, 135f - i * 120f));
                loads[i] = MakeButton("Load Slot " + (i + 1), root.transform, "불러오기", new Vector2(270f, 135f - i * 120f));
            }
            Button close = MakeButton("Close Save Panel", root.transform, "닫기", new Vector2(0f, -245f));
            Stage10ManualSavePanel panel = root.GetComponent<Stage10ManualSavePanel>(); panel.Configure(root.GetComponent<CanvasGroup>(), saves, loads, close); return panel;
        }

        private static Button MakeButton(string name, Transform parent, string text, Vector2 position)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            SetRect((RectTransform)go.transform, position, new Vector2(200f, 70f)); go.GetComponent<Image>().color = new Color(.65f, .45f, .25f, 1f);
            Text label = Label("Label", go.transform, text, 24); label.alignment = TextAnchor.MiddleCenter; Stretch(label.rectTransform); return go.GetComponent<Button>();
        }

        private static Text Label(string name, Transform parent, string text, int size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
            ((RectTransform)go.transform).sizeDelta = new Vector2(300f, 70f); return label;
        }
        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Validate(RealityRoomSaveCoordinator coordinator, Stage10ManualSavePanel panel)
        {
            if (coordinator == null || panel == null || GameObject.Find(WhiteChairName)?.GetComponent<WhiteChairSavePoint>() == null)
                throw new InvalidOperationException("Stage 10 save integration is incomplete.");
            if (UnityEngine.Object.FindObjectsByType<Stage10AutoSaveTrigger>(FindObjectsSortMode.None).Length != 1)
                throw new InvalidOperationException("Stage 10 requires exactly one entry auto-save trigger.");
        }
    }
}
