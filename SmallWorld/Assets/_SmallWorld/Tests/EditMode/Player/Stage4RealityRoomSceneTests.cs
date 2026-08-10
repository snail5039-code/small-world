using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage4RealityRoomSceneTests
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";

        [Test]
        public void RealityRoom_HasCompleteStage4Layout()
        {
            EditorSceneManager.OpenScene(ScenePath);

            string[] requiredNames =
            {
                "Stage 4 Reality Room", "Floor", "Ceiling", "Door", "Window Glass", "Bed",
                "Computer Desk", "Monitor Screen", "Wardrobe", "Bookshelf", "Model House Table",
                "Door Corridor", "Empty Frame", "Old Telephone", "Midnight Clock",
                "Reality Room Audio Zone", "First Person Player"
            };
            foreach (string name in requiredNames)
                Assert.That(GameObject.Find(name), Is.Not.Null, $"Missing required object: {name}");

            Assert.That(Object.FindObjectsByType<FirstPersonPlayerController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<AudioReverbZone>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [Test]
        public void RealityRoom_HasCollisionLightingAndMaterials()
        {
            EditorSceneManager.OpenScene(ScenePath);
            GameObject root = GameObject.Find("Stage 4 Reality Room");
            Assert.That(root, Is.Not.Null);

            Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.GreaterThanOrEqualTo(25));
            Assert.That(root.GetComponentsInChildren<Light>(true).Length, Is.GreaterThanOrEqualTo(3));

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, $"Missing material on {renderer.name}");
                Assert.That(renderer.sharedMaterial.shader, Is.Not.Null, $"Missing shader on {renderer.name}");
                Assert.That(renderer.sharedMaterial.shader.name, Does.Not.Contain("Hidden/InternalErrorShader"));
            }
        }

        [Test]
        public void RealityRoom_HasStage5InteractionConnections()
        {
            EditorSceneManager.OpenScene(ScenePath);

            Assert.That(Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None), Has.Length.EqualTo(7));
            Assert.That(GameObject.Find("Stage 10 Save Integration"), Is.Not.Null);
            InteractionPromptView[] views = Object.FindObjectsByType<InteractionPromptView>(FindObjectsSortMode.None);
            Assert.That(views, Has.Length.EqualTo(1));
            Assert.That(GameObject.Find("Door Hinge"), Is.Not.Null);
            Text[] labels = views[0].GetComponentsInChildren<Text>(true);
            Assert.That(labels, Has.Exactly(1).Matches<Text>(text => text.name == "Interaction Prompt"));
            Assert.That(labels, Has.Exactly(1).Matches<Text>(text => text.name == "Interaction Feedback"));
        }
    }
}
