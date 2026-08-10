using NUnit.Framework;
using System.Collections.Generic;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Puzzle.Stage9;
using SmallWorld.Puzzle.Stage9.Persistence;
using SmallWorld.Player;
using SmallWorld.UI;
using SmallWorld.UI.Stage8;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Puzzle.Stage9Integration.Tests
{
    public sealed class PhotoPuzzleIntegrationTests
    {
        [Test]
        public void CorrectPhotoSequence_ChangesModelHouseAndAddsStage8PhotoRecord()
        {
            Fixture fixture = CreateFixture(UIState.Gameplay);
            try
            {
                Assert.That(fixture.View.Open(), Is.True);
                Assert.That(fixture.View.SelectPiece(0), Is.EqualTo(PuzzleActionResult.Incorrect));
                Assert.That(fixture.Feedback.text, Does.Contain("힌트"));
                Assert.That(fixture.View.SelectPiece(1), Is.EqualTo(PuzzleActionResult.Accepted));
                Assert.That(fixture.View.SelectPiece(0), Is.EqualTo(PuzzleActionResult.Accepted));
                Assert.That(fixture.View.SelectPiece(2), Is.EqualTo(PuzzleActionResult.Accepted));

                Assert.That(fixture.View.IsCompleted, Is.True);
                Assert.That(fixture.Roof.activeSelf, Is.False);
                Assert.That(fixture.Records.Reader.Contains(PhotoPuzzleView.CompletionRecordId), Is.True);
                Assert.That(fixture.Records.Reader.TryGet(PhotoPuzzleView.CompletionRecordId, out StoredRecord stored), Is.True);
                Assert.That(stored.Record.Kind, Is.EqualTo(RecordKind.Photo));
            }
            finally { fixture.Destroy(); }
        }

        [TestCase(UIState.Paused)]
        [TestCase(UIState.Inspection)]
        [TestCase(UIState.Settings)]
        public void PhotoPuzzle_DoesNotTakeInputFromStage6Overlays(UIState state)
        {
            Fixture fixture = CreateFixture(state);
            try
            {
                Assert.That(fixture.View.Open(), Is.False);
                Assert.That(fixture.View.IsOpen, Is.False);
            }
            finally
            {
                Time.timeScale = 1f;
                fixture.Destroy();
            }
        }

        [Test]
        public void IncorrectChoice_PreservesStepAndReportsKoreanFeedback()
        {
            Fixture fixture = CreateFixture(UIState.Gameplay);
            try
            {
                fixture.View.Open();
                fixture.View.SelectPiece(0);
                Assert.That(fixture.View.CurrentState.CurrentStep, Is.Zero);
                Assert.That(fixture.View.CurrentState.IncorrectAttempts, Is.EqualTo(1));
                Assert.That(fixture.Feedback.text, Does.Contain("힌트"));
            }
            finally { fixture.Destroy(); }
        }

        [Test]
        public void CompletedSnapshot_RestoresInNewInstanceWithoutDuplicateReward()
        {
            var storage = new MemoryPhotoPuzzleStorage();
            Fixture first = CreateFixture(UIState.Gameplay, storage);
            try
            {
                first.View.Open();
                first.View.SelectPiece(1);
                first.View.SelectPiece(0);
                first.View.SelectPiece(2);
                Assert.That(first.Records.Reader.Contains(PhotoPuzzleView.CompletionRecordId), Is.True);
                Assert.That(storage.Contains(PhotoPuzzleView.PersistenceKey), Is.True);
            }
            finally { first.Destroy(); }

            Fixture restored = CreateFixture(UIState.Gameplay);
            int rehydratedRecords = 0;
            restored.Records.NewRecordAdded += _ => rehydratedRecords++;
            try
            {
                restored.View.ConfigurePersistence(storage);
                Assert.That(restored.View.IsCompleted, Is.True);
                Assert.That(restored.View.Open(), Is.False);
                Assert.That(restored.Roof.activeSelf, Is.False);
                Assert.That(restored.Records.Reader.Contains(PhotoPuzzleView.CompletionRecordId), Is.True);
                Assert.That(restored.Records.Reader.GetAll(RecordKind.Photo), Has.Count.EqualTo(1));
                Assert.That(rehydratedRecords, Is.EqualTo(1));

                Assert.That(restored.View.RestoreSavedProgress(), Is.True);
                Assert.That(restored.Records.Reader.GetAll(RecordKind.Photo), Has.Count.EqualTo(1));
                Assert.That(rehydratedRecords, Is.EqualTo(1));
            }
            finally { restored.Destroy(); }
        }

        [Test]
        public void InProgressSnapshot_RestoresStepInNewInstance()
        {
            var storage = new MemoryPhotoPuzzleStorage();
            Fixture first = CreateFixture(UIState.Gameplay, storage);
            try
            {
                first.View.Open();
                first.View.SelectPiece(1);
                Assert.That(first.View.CurrentState.CurrentStep, Is.EqualTo(1));
            }
            finally { first.Destroy(); }

            Fixture restored = CreateFixture(UIState.Gameplay, storage);
            try
            {
                Assert.That(restored.View.CurrentState.Status, Is.EqualTo(PuzzleStatus.InProgress));
                Assert.That(restored.View.CurrentState.CurrentStep, Is.EqualTo(1));
                Assert.That(restored.View.Open(), Is.True);
            }
            finally { restored.Destroy(); }
        }

        [Test]
        public void CorruptStoredSnapshot_IsQuarantinedAndFreshPuzzleCanStart()
        {
            var storage = new MemoryPhotoPuzzleStorage();
            storage.Write(PhotoPuzzleView.PersistenceKey, "not-json");
            Fixture fixture = CreateFixture(UIState.Gameplay, storage);
            try
            {
                Assert.That(storage.Contains(PhotoPuzzleView.PersistenceKey), Is.False);
                Assert.That(storage.Contains(PhotoPuzzleView.PersistenceKey + ".corrupt"), Is.True);
                Assert.That(fixture.View.CurrentState.Status, Is.EqualTo(PuzzleStatus.NotStarted));
                Assert.That(fixture.View.Open(), Is.True);
            }
            finally { fixture.Destroy(); }
        }

        [Test]
        public void ClearSavedProgress_RemovesActiveAndQuarantinedKeys()
        {
            var storage = new MemoryPhotoPuzzleStorage();
            storage.Write(PhotoPuzzleView.PersistenceKey, "active");
            storage.Write(PhotoPuzzleView.PersistenceKey + ".corrupt", "corrupt");

            PhotoPuzzleView.ClearSavedProgress(storage);

            Assert.That(storage.Contains(PhotoPuzzleView.PersistenceKey), Is.False);
            Assert.That(storage.Contains(PhotoPuzzleView.PersistenceKey + ".corrupt"), Is.False);
        }

        [Test]
        public void RealityRoomScene_ContainsPhotoPuzzleAndModelHouseLink()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject root = GameObject.Find("Stage 9 Photo Puzzle UI");
            GameObject frame = GameObject.Find("Empty Frame");
            GameObject roof = GameObject.Find("Model House Roof");
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<PhotoPuzzleView>(), Is.Not.Null);
            Assert.That(frame.GetComponent<PhotoPuzzleInteractable>(), Is.Not.Null);
            Assert.That(roof, Is.Not.Null);
            var serialized = new SerializedObject(root.GetComponent<PhotoPuzzleView>());
            Assert.That(serialized.FindProperty("modelHouseRoof").objectReferenceValue, Is.SameAs(roof));
            Assert.That(serialized.FindProperty("persistenceKey").stringValue, Is.EqualTo(PhotoPuzzleView.PersistenceKey));
            Assert.That(Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None), Has.Length.EqualTo(6));
        }

        private static Fixture CreateFixture(UIState state, IPhotoPuzzleStorage storage = null)
        {
            var stage6Object = new GameObject("Stage6 Test");
            var stage6 = stage6Object.AddComponent<Stage6UIController>();
            stage6.ConfigureInitialState(state);
            var recordObject = new GameObject("Records Test", typeof(CanvasGroup));
            var records = recordObject.AddComponent<Stage8RecordView>();
            records.Configure(recordObject.GetComponent<CanvasGroup>(), null, null, null, null, null, null, null,
                null, stage6, null);
            var root = new GameObject("Photo Puzzle Test", typeof(CanvasGroup));
            var feedbackObject = new GameObject("Feedback", typeof(Text));
            var progressObject = new GameObject("Progress", typeof(Text));
            var instructionObject = new GameObject("Instruction", typeof(Text));
            var roof = new GameObject("Model House Roof Test");
            var buttons = new[] { CreateButton("Door"), CreateButton("Window"), CreateButton("Roof") };
            var view = root.AddComponent<PhotoPuzzleView>();
            view.Configure(root.GetComponent<CanvasGroup>(), instructionObject.GetComponent<Text>(),
                feedbackObject.GetComponent<Text>(), progressObject.GetComponent<Text>(), buttons, null, null,
                stage6, null, records, roof);
            if (storage != null) view.ConfigurePersistence(storage);
            return new Fixture(root, stage6Object, recordObject, feedbackObject, progressObject,
                instructionObject, roof, buttons, view, records);
        }

        private static Button CreateButton(string name) => new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();

        private sealed class MemoryPhotoPuzzleStorage : IPhotoPuzzleStorage
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();
            public bool Contains(string key) => values.ContainsKey(key);
            public bool TryRead(string key, out string value) => values.TryGetValue(key, out value);
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
            public void Quarantine(string key, string value) => values[key + ".corrupt"] = value;
        }

        private sealed class Fixture
        {
            private readonly Object[] objects;
            public Fixture(GameObject root, GameObject stage6, GameObject record, GameObject feedback,
                GameObject progress, GameObject instruction, GameObject roof, Button[] buttons,
                PhotoPuzzleView view, Stage8RecordView records)
            {
                View = view;
                Records = records;
                Feedback = feedback.GetComponent<Text>();
                Roof = roof;
                objects = new Object[] { root, stage6, record, feedback, progress, instruction, roof,
                    buttons[0].gameObject, buttons[1].gameObject, buttons[2].gameObject };
            }
            public PhotoPuzzleView View { get; }
            public Stage8RecordView Records { get; }
            public Text Feedback { get; }
            public GameObject Roof { get; }
            public void Destroy() { foreach (Object item in objects) if (item != null) Object.DestroyImmediate(item); }
        }
    }
}
