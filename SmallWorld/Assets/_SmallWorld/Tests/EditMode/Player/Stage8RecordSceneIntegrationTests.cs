using System;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage8RecordSceneIntegrationTests
    {
        [Test]
        public void RealityRoom_AddsResponsiveRecordOverlayAndKeepsStage5Targets()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject stage6 = GameObject.Find("Stage 6 Reality Room UI");
            GameObject stage8 = GameObject.Find("Stage 8 Record UI");

            Assert.That(stage6, Is.Not.Null);
            Assert.That(stage6.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(stage6.GetComponentInChildren<SafeAreaFitter>(true), Is.Not.Null);
            Assert.That(stage8, Is.Not.Null);
            Assert.That(stage8.GetComponent<Stage8RecordView>(), Is.Not.Null);
            InteractableBase[] interactables = Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            Assert.That(Array.FindAll(interactables,
                item => item.GetType().FullName != "SmallWorld.Flow.FirstMemoryEntryInteractable" &&
                    item.GetType().FullName != "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(7));
            Assert.That(Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.FirstMemoryEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(System.Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("Stage 10 Save Integration"), Is.Not.Null);

            Component room = Object.FindFirstObjectByType(GetRealityRoomControllerType()) as Component;
            Assert.That(room, Is.Not.Null);
            var serializedRoom = new SerializedObject(room);
            Assert.That(serializedRoom.FindProperty("recordView").objectReferenceValue,
                Is.SameAs(stage8.GetComponent<Stage8RecordView>()));
        }

        [Test]
        public void RecordView_ShowsAllThreeTabsAndRaisesOnlyNewRecordNotifications()
        {
            var root = new GameObject("Record View Test", typeof(CanvasGroup));
            var titleObject = new GameObject("Title", typeof(Text));
            var listObject = new GameObject("List", typeof(Text));
            var detailsObject = new GameObject("Details", typeof(Text));
            var view = root.AddComponent<Stage8RecordView>();
            int notifications = 0;

            try
            {
                view.Configure(root.GetComponent<CanvasGroup>(), titleObject.GetComponent<Text>(),
                    listObject.GetComponent<Text>(), detailsObject.GetComponent<Text>(), null, null, null, null,
                    null, null, null);
                view.NewRecordAdded += _ => notifications++;
                Assert.That(view.AddRecord(new InventoryRecord("key", RecordKind.KeyItem, "열쇠")), Is.True);
                Assert.That(view.AddRecord(new InventoryRecord("memory", RecordKind.MemoryFragment, "자정")), Is.True);
                Assert.That(view.AddRecord(new InventoryRecord("photo", RecordKind.Photo, "빈 액자")), Is.True);
                Assert.That(view.AddRecord(new InventoryRecord("photo", RecordKind.Photo, "중복")), Is.False);
                Assert.That(notifications, Is.EqualTo(3));

                view.SelectInventory();
                Assert.That(titleObject.GetComponent<Text>().text, Is.EqualTo("인벤토리"));
                Assert.That(listObject.GetComponent<Text>().text, Does.Contain("열쇠"));
                view.SelectMemories();
                Assert.That(titleObject.GetComponent<Text>().text, Is.EqualTo("기억 조각"));
                Assert.That(listObject.GetComponent<Text>().text, Does.Contain("자정"));
                view.SelectRecords();
                Assert.That(titleObject.GetComponent<Text>().text, Is.EqualTo("조사 · 사진 · 이름 기록"));
                Assert.That(detailsObject.GetComponent<Text>().text, Does.Contain("[사진] 빈 액자"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(titleObject);
                Object.DestroyImmediate(listObject);
                Object.DestroyImmediate(detailsObject);
            }
        }

        [TestCase(UIState.Paused)]
        [TestCase(UIState.Inspection)]
        [TestCase(UIState.Settings)]
        public void RecordView_DoesNotOpenWhileStage6OverlayOwnsInput(UIState state)
        {
            var stage6Object = new GameObject("Stage 6 Overlay State");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(state);
            var root = new GameObject("Record Overlay Contract", typeof(CanvasGroup));
            var view = root.AddComponent<Stage8RecordView>();

            try
            {
                view.Configure(root.GetComponent<CanvasGroup>(), null, null, null, null, null, null, null,
                    null, stage6, null);
                Assert.That(view.Open(), Is.False);
                Assert.That(view.IsOpen, Is.False);
            }
            finally
            {
                Time.timeScale = 1f;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(stage6Object);
            }
        }

        [Test]
        public void RecordView_DoesNotOpenWhileDialogueOwnsInput()
        {
            var stage6Object = new GameObject("Stage 6 Gameplay State");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(UIState.Gameplay);
            var dialogueObject = new GameObject("Active Dialogue", typeof(CanvasGroup));
            var dialogue = dialogueObject.AddComponent<Stage7DialogueView>();
            dialogue.Configure(dialogueObject.GetComponent<CanvasGroup>(), null, null, null, null, null, null,
                null, null, null, null, null, null, stage6);
            dialogue.StartDialogue(Stage7DemoDialogue.Create());
            var root = new GameObject("Record Dialogue Contract", typeof(CanvasGroup));
            var view = root.AddComponent<Stage8RecordView>();

            try
            {
                view.Configure(root.GetComponent<CanvasGroup>(), null, null, null, null, null, null, null,
                    null, stage6, dialogue);
                Assert.That(view.Open(), Is.False);
                Assert.That(view.IsOpen, Is.False);
                Assert.That(dialogue.IsDialogueActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(dialogueObject);
                Object.DestroyImmediate(stage6Object);
            }
        }

        [Test]
        public void ActiveRecordView_ClosesWhenDialogueTakesInputOwnership()
        {
            var stage6Object = new GameObject("Stage 6 Gameplay Ownership");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(UIState.Gameplay);
            var dialogueObject = new GameObject("Dialogue Ownership", typeof(CanvasGroup));
            var dialogue = dialogueObject.AddComponent<Stage7DialogueView>();
            dialogue.Configure(dialogueObject.GetComponent<CanvasGroup>(), null, null, null, null, null, null,
                null, null, null, null, null, null, stage6);
            var root = new GameObject("Record Ownership", typeof(CanvasGroup));
            var view = root.AddComponent<Stage8RecordView>();

            try
            {
                view.Configure(root.GetComponent<CanvasGroup>(), null, null, null, null, null, null, null,
                    null, stage6, dialogue);
                Assert.That(view.Open(), Is.True);
                dialogue.StartDialogue(Stage7DemoDialogue.Create());
                Assert.That(view.IsOpen, Is.False);
                Assert.That(dialogue.IsDialogueActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(dialogueObject);
                Object.DestroyImmediate(stage6Object);
            }
        }

        private static Type GetRealityRoomControllerType()
        {
            Type type = Type.GetType("SmallWorld.Flow.RealityRoomController, Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            return type;
        }
    }
}
