using NUnit.Framework;
using SmallWorld.Flow;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15PrologueChapterOneSceneTests
    {
        private const string StoryScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private const string RealityScenePath = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";

        [Test]
        public void StoryScene_HasPlayablePrologueAndFourthPlaceLandmarks()
        {
            EditorSceneManager.OpenScene(StoryScenePath);

            string[] landmarks =
            {
                "Prologue Living Room - Our House", "Prologue Sofa", "Model House First Memory Door",
                "Reserved Email Monitor", "Loop 109 Display", "Chapter 1 Apartment - The Fourth Place",
                "Dining Table", "The Empty Fourth Chair", "Fourth Plate", "Stopped Clock 4",
                "Manipulated Family Photo", "Locked Seoyun Room", "Repeating Corridor", "Nonexistent Room",
                "Basement Key Foreshadow"
            };

            foreach (string landmark in landmarks)
                Assert.That(GameObject.Find(landmark), Is.Not.Null, $"Missing Stage 15 landmark: {landmark}");

            Assert.That(Object.FindObjectsByType<StoryRouteInteractable>(FindObjectsSortMode.None).Length,
                Is.GreaterThanOrEqualTo(7));
            Assert.That(Object.FindObjectsByType<FirstPersonPlayerController>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length,
                Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void RealityRoom_HasStoryRouteEntryForPrologueFlow()
        {
            EditorSceneManager.OpenScene(RealityScenePath);

            GameObject entry = GameObject.Find("Stage 15 Story Route Entry");
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.GetComponent<StoryRouteEntryInteractable>(), Is.Not.Null);
            Assert.That(entry.GetComponent<Collider>(), Is.Not.Null);
        }
    }
}
