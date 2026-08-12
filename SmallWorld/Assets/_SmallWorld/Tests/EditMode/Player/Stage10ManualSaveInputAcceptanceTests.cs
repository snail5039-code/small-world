using System;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Player;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage10ManualSaveInputAcceptanceTests
    {
        private const string RealityRoomScene = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(RealityRoomScene);
            FindRequired<Stage6UIController>().ConfigureInitialState(UIState.Gameplay);
            DialogueCursorMode.RequestGameplay();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            DialogueCursorMode.RequestUi();
        }

        [Test]
        public void OpeningManualSave_ImmediatelyReleasesCursorAndSuspendsPlayerInput()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();

            savePanel.Open();

            AssertPanelVisible(savePanel, true);
            Assert.That(player.enabled, Is.False,
                "The save menu must suspend gameplay input as soon as it opens.");
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None),
                "The save menu must be usable by mouse without requiring Escape first.");
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);
        }

        [Test]
        public void ClosingManualSave_RestoresGameplayInputAndCursorLock()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();

            savePanel.Open();
            savePanel.Close();

            AssertPanelVisible(savePanel, false);
            Assert.That(player.enabled, Is.True);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
        }

        [Test]
        public void EscapeWhileManualSaveIsOpen_ClosesOnlySaveAndRestoresExactRuntimeState()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            Type roomType = GetRealityRoomControllerType();
            Component room = UnityEngine.Object.FindFirstObjectByType(roomType) as Component;
            Assert.That(room, Is.Not.Null, roomType.Name + " must be wired in the Reality Room scene.");
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();
            Stage6UIController stage6 = FindRequired<Stage6UIController>();
            Stage8RecordView records = FindRequired<Stage8RecordView>();
            const float capturedTimeScale = 0.42f;
            Time.timeScale = capturedTimeScale;

            savePanel.Open();
            MethodInfo escape = roomType.GetMethod("HandleEscapePressed",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(escape, Is.Not.Null);
            Assert.That((bool)escape.Invoke(room, null), Is.True);

            AssertPanelVisible(savePanel, false);
            Assert.That(stage6.StateMachine.Current, Is.EqualTo(UIState.Gameplay),
                "The same Escape must not also open the pause menu.");
            Assert.That(records.IsOpen, Is.False);
            Assert.That(player.enabled, Is.True);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(capturedTimeScale));
        }

        [Test]
        public void ClosingManualSave_DuringDialogue_PreservesDialogueCursorAndPlayerOwnership()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            Stage7DialogueView dialogue = FindRequired<Stage7DialogueView>();
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();
            dialogue.StartDialogue(Stage7DemoDialogue.Create());

            savePanel.Open();
            AssertPanelVisible(savePanel, false);
            savePanel.Close();

            Assert.That(dialogue.IsDialogueActive, Is.True);
            Assert.That(player.enabled, Is.False,
                "Closing a nested save menu must not re-enable the player while dialogue owns input.");
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);
        }

        [Test]
        public void ClosingManualSave_DuringRecordView_PreservesRecordCursorAndPlayerOwnership()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            Stage8RecordView recordView = FindRequired<Stage8RecordView>();
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();
            FindRequired<Stage6UIController>().ConfigureInitialState(UIState.Gameplay);
            Assert.That(recordView.Open(), Is.True);

            savePanel.Open();
            AssertPanelVisible(savePanel, false);
            savePanel.Close();

            Assert.That(recordView.IsOpen, Is.True);
            Assert.That(player.enabled, Is.False,
                "Closing a nested save menu must not re-enable the player while records own input.");
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);
        }

        private static T FindRequired<T>() where T : UnityEngine.Object
        {
            T value = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            Assert.That(value, Is.Not.Null, typeof(T).Name + " must be wired in the Reality Room scene.");
            return value;
        }

        private static Type GetRealityRoomControllerType()
        {
            Type type = Type.GetType("SmallWorld.Flow.RealityRoomController, Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            return type;
        }

        private static void AssertPanelVisible(Stage10ManualSavePanel savePanel, bool expected)
        {
            var serialized = new SerializedObject(savePanel);
            var panel = serialized.FindProperty("panel").objectReferenceValue as CanvasGroup;
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.interactable, Is.EqualTo(expected));
            Assert.That(panel.blocksRaycasts, Is.EqualTo(expected));
        }
    }
}
