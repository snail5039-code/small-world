using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage7DialogueSceneIntegrationTests
    {
        [Test]
        public void RealityRoom_PreservesStage6AndAddsResponsiveDialogueOverlay()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            GameObject stage6 = GameObject.Find("Stage 6 Reality Room UI");
            GameObject stage7 = GameObject.Find("Stage 7 Dialogue UI");

            Assert.That(stage6, Is.Not.Null);
            Assert.That(stage6.GetComponent<Stage6UIController>(), Is.Not.Null);
            Assert.That(stage6.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(stage7, Is.Not.Null);
            Assert.That(stage7.GetComponent<Stage7DialogueView>(), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<InteractableBase>(FindObjectsSortMode.None), Has.Length.EqualTo(6));
        }

        [Test]
        public void DemoDialogue_ChoicesBranchAndChangeRelationship()
        {
            DialogueDefinition definition = Stage7DemoDialogue.Create();
            var trustingState = new DialogueState();
            var trusting = new DialogueSession(definition, trustingState);
            trusting.Advance();
            trusting.SelectChoice("trust");

            var doubtfulState = new DialogueState();
            var doubtful = new DialogueSession(definition, doubtfulState);
            doubtful.Advance();
            doubtful.SelectChoice("doubt");

            Assert.That(trusting.Current.NodeId, Is.EqualTo("warm"));
            Assert.That(doubtful.Current.NodeId, Is.EqualTo("cold"));
            Assert.That(trustingState.Get(Stage7DemoDialogue.RelationshipKey), Is.EqualTo(2));
            Assert.That(doubtfulState.Get(Stage7DemoDialogue.RelationshipKey), Is.EqualTo(-1));
            Assert.That(trusting.History, Has.Count.EqualTo(4));
        }
    }
}
