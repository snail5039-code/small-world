using NUnit.Framework;
using SmallWorld.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage6SceneIntegrationTests
    {
        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity", "Stage 6 Main Menu UI")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity", "Stage 6 Reality Room UI")]
        public void IntegratedScene_HasOneResponsiveStage6Root(string scenePath, string rootName)
        {
            EditorSceneManager.OpenScene(scenePath);
            GameObject root = GameObject.Find(rootName);

            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetComponent<Stage6UIController>(), Is.Not.Null);
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [Test]
        public void RealityRoom_PreservesStageFiveAndAddsOverlayViews()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject root = GameObject.Find("Stage 6 Reality Room UI");

            Assert.That(root, Is.Not.Null);
            InteractableBase[] interactables = Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            Assert.That(System.Array.FindAll(interactables,
                item => item.GetType().FullName != "SmallWorld.Flow.FirstMemoryEntryInteractable" &&
                    item.GetType().FullName != "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(7));
            Assert.That(System.Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.FirstMemoryEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(System.Array.FindAll(interactables,
                item => item.GetType().FullName == "SmallWorld.Flow.StoryRouteEntryInteractable"), Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("Stage 10 Save Integration"), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<InteractionPromptView>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<InspectionView>(true), Has.Length.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<NotificationQueueView>(true), Has.Length.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Stage6LoadingView>(true), Has.Length.EqualTo(1));
        }
    }
}
