using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI.Stage7;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15PrologueChapter1SceneTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
            Time.timeScale = 1f;
            DialogueCursorMode.RequestGameplay();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            DialogueCursorMode.RequestUi();
        }

        [Test]
        public void StoryRoute_IntegratesLastPlatformLandmarksBetweenChaptersOneAndThree()
        {
            GameObject route = GameObject.Find("Stage 15 Story Route");

            Assert.That(route, Is.Not.Null);
            Component controller = route.GetComponent("StoryRouteController");
            Assert.That(controller, Is.Not.Null);
            Assert.That(route.GetComponent("StoryRouteProgressAdapter"), Is.Not.Null);

            SerializedProperty nodes = new SerializedObject(controller).FindProperty("nodes");
            Assert.That(nodes, Is.Not.Null);
            Assert.That(nodes.arraySize, Is.GreaterThanOrEqualTo(4));
            AssertNode(nodes.GetArrayElementAtIndex(0), "prologue", "Prologue");
            AssertNode(nodes.GetArrayElementAtIndex(1), "chapter-1", "Fourth Place");
            AssertNode(nodes.GetArrayElementAtIndex(2), "chapter-2", "Last Platform");
            AssertNode(nodes.GetArrayElementAtIndex(3), "chapter-3", "Perfect Day");
        }

        [Test]
        public void StoryRoute_TabOwnsAndRestoresPlayerInputAndCursor()
        {
            StoryRouteController route = Object.FindFirstObjectByType<StoryRouteController>();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(route.HandleTabPressed(), Is.True);
            Assert.That(route.IsRuntimeOverlayOpen, Is.True);
            Assert.That(player.enabled, Is.False);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);

            Assert.That(route.HandleTabPressed(), Is.True);
            Assert.That(route.IsRuntimeOverlayOpen, Is.False);
            Assert.That(player.enabled, Is.True);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
        }

        [Test]
        public void StoryRoute_EscapePausesAndRestoresRuntimeState()
        {
            StoryRouteController route = Object.FindFirstObjectByType<StoryRouteController>();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(route.HandleEscapePressed(), Is.True);
            Assert.That(route.IsRuntimePaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.enabled, Is.False);

            Assert.That(route.HandleEscapePressed(), Is.True);
            Assert.That(route.IsRuntimeOverlayOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(player.enabled, Is.True);
        }

        [Test]
        public void StoryRoute_DoesNotStealInputWhileSavePanelOwnsIt()
        {
            StoryRouteController route = Object.FindFirstObjectByType<StoryRouteController>();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();
            var saveObject = new GameObject("Story Route Save Input Owner");
            CanvasGroup panel = saveObject.AddComponent<CanvasGroup>();
            Stage10ManualSavePanel savePanel = saveObject.AddComponent<Stage10ManualSavePanel>();
            savePanel.Configure(panel, null, null, null);
            savePanel.Configure(player);
            savePanel.Open();

            try
            {
                Assert.That(savePanel.IsOpen, Is.True);
                Assert.That(route.HandleTabPressed(), Is.False);
                Assert.That(route.HandleEscapePressed(), Is.False);
                Assert.That(route.IsRuntimeOverlayOpen, Is.False);
                Assert.That(player.enabled, Is.False);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            }
            finally
            {
                Object.DestroyImmediate(saveObject);
            }
        }

        private static void AssertNode(SerializedProperty node, string id, string displayFragment)
        {
            Assert.That(node.FindPropertyRelative("Id").stringValue, Is.EqualTo(id));
            Assert.That(node.FindPropertyRelative("DisplayName").stringValue, Does.Contain(displayFragment));
            Assert.That(node.FindPropertyRelative("Arrival").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("DialogueEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("PuzzleEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("MemoryEntry").objectReferenceValue, Is.Not.Null);
        }
    }
}
