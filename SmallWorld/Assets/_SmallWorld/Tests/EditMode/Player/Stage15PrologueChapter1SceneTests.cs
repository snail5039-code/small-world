using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15PrologueChapter1SceneTests
    {
        [Test]
        public void StoryRoute_IntegratesPrologueAndFourthSeatWithAllEntryPoints()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/04_StoryRoute.unity");
            GameObject route = GameObject.Find("Stage 15 Story Route");

            Assert.That(route, Is.Not.Null);
            Component controller = route.GetComponent("StoryRouteController");
            Assert.That(controller, Is.Not.Null);
            Assert.That(route.GetComponent("StoryRouteProgressAdapter"), Is.Not.Null);

            SerializedProperty nodes = new SerializedObject(controller).FindProperty("nodes");
            Assert.That(nodes, Is.Not.Null);
            Assert.That(nodes.arraySize, Is.GreaterThanOrEqualTo(2));
            AssertNode(nodes.GetArrayElementAtIndex(0), "prologue", "Prologue");
            AssertNode(nodes.GetArrayElementAtIndex(1), "chapter-1", "Fourth Place");
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
