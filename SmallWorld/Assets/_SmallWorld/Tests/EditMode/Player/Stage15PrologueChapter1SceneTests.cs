using System.Reflection;
using NUnit.Framework;
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
        public void StoryRoute_IntegratesPerfectDayVillagePuzzlesAndFinalChoice()
        {
            string[] requiredObjects =
            {
                "Warm Village Cafe", "Sunny Village Park", "Perfect Day Arcade", "Riverside Walk",
                "Repeated Person And Dialogue Mark 1", "Menu Showing Her Favorites", "Flipped Menu Bitter Coffee",
                "Mina Bitter Coffee Cup", "Choice Graffiti", "Fourth Choice I Do Not Know What You Like",
                "Movable Park Shadow Stage 1", "Movable Park Shadow Stage 2", "Movable Park Shadow Stage 3",
                "Sunset Stage Light 3", "Yuna Previous Loop Appearance 3", "Evening Unlocked",
                "Perfect Date Photo", "Preserve Photo Choice", "Tear Photo Choice", "Mina Original Memory",
                "Return Home Door", "Mina Perfect Day Loop", "Break The Perfect Day Rules",
                "Preserve Or Tear The Photo And Return Home"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from chapter 3.");

            Assert.That(GameObject.Find("Mina Perfect Day Loop").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Break The Perfect Day Rules").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Preserve Or Tear The Photo And Return Home").GetComponent("StoryRouteInteractable"), Is.Not.Null);
        }

        [Test]
        public void StoryRoute_TabOwnsAndRestoresPlayerInputAndCursor()
        {
            Component route = FindStoryRouteController();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.True);
            Assert.That(player.enabled, Is.False);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);

            Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
            Assert.That(player.enabled, Is.True);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
        }

        [Test]
        public void StoryRoute_EscapePausesAndRestoresRuntimeState()
        {
            Component route = FindStoryRouteController();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimePaused"), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.enabled, Is.False);

            Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(player.enabled, Is.True);
        }

        [Test]
        public void StoryRoute_DoesNotStealInputWhileSavePanelOwnsIt()
        {
            Component route = FindStoryRouteController();
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
                Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.False);
                Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.False);
                Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
                Assert.That(player.enabled, Is.False);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            }
            finally
            {
                Object.DestroyImmediate(saveObject);
            }
        }

        private static Component FindStoryRouteController()
        {
            GameObject route = GameObject.Find("Stage 15 Story Route");
            Assert.That(route, Is.Not.Null);
            Component controller = route.GetComponent("StoryRouteController");
            Assert.That(controller, Is.Not.Null);
            return controller;
        }

        private static T Invoke<T>(Component target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " method is missing.");
            return (T)method.Invoke(target, null);
        }

        private static T ReadProperty<T>(Component target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName + " property is missing.");
            return (T)property.GetValue(target);
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
