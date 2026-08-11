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
        public void ClosingManualSave_DuringDialogue_PreservesDialogueCursorAndPlayerOwnership()
        {
            Stage10ManualSavePanel savePanel = FindRequired<Stage10ManualSavePanel>();
            Stage7DialogueView dialogue = FindRequired<Stage7DialogueView>();
            FirstPersonPlayerController player = FindRequired<FirstPersonPlayerController>();
            dialogue.StartDialogue(Stage7DemoDialogue.Create());

            savePanel.Open();
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
            savePanel.Close();

            Assert.That(recordView.IsOpen, Is.True);
            Assert.That(player.enabled, Is.False,
                "Closing a nested save menu must not re-enable the player while records own input.");
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);
        }

        private static T FindRequired<T>() where T : Object
        {
            T value = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            Assert.That(value, Is.Not.Null, typeof(T).Name + " must be wired in the Reality Room scene.");
            return value;
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
