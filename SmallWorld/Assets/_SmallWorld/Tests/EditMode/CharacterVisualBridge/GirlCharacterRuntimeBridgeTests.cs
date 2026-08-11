using NUnit.Framework;
using SmallWorld.Character.Stage11;
using SmallWorld.UI.Stage7;
using UnityEngine;

namespace SmallWorld.Character.VisualBridge.Tests
{
    public sealed class GirlCharacterRuntimeBridgeTests
    {
        private GameObject root;
        private GameObject dialogueObject;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (dialogueObject != null) Object.DestroyImmediate(dialogueObject);
        }

        [Test]
        public void RestoredWarmRelationshipImmediatelyChangesVisualBehavior()
        {
            GirlCharacterRuntimeBridge bridge = CreateBridge(out GirlCharacterController controller, out Stage7DialogueView dialogue);
            var savedState = new GirlCharacterState("girl", 75);
            GirlDialogueBinding.WriteTo(savedState, dialogue.State);

            bridge.RestoreNow();

            Assert.That(bridge.State.Relationship, Is.EqualTo(75));
            Assert.That(controller.Behavior, Is.EqualTo(GirlBehavior.Approach));
            Assert.That(controller.Expression, Is.EqualTo(GirlExpression.Happy));
        }

        [Test]
        public void PlayerActionUpdatesCoreStateAndPublishedDialogueState()
        {
            GirlCharacterRuntimeBridge bridge = CreateBridge(out GirlCharacterController controller, out Stage7DialogueView dialogue);

            bridge.React(PlayerAction.Help);

            Assert.That(bridge.State.Relationship, Is.EqualTo(10));
            Assert.That(bridge.State.LastPlayerAction, Is.EqualTo(PlayerAction.Help));
            Assert.That(controller.Behavior, Is.EqualTo(GirlBehavior.Observe));
            Assert.That(dialogue.State.Get(GirlCharacterKeys.Relationship("girl")), Is.EqualTo(10));
        }

        private GirlCharacterRuntimeBridge CreateBridge(out GirlCharacterController controller, out Stage7DialogueView dialogue)
        {
            root = new GameObject("Girl Character Test");
            controller = root.AddComponent<GirlCharacterController>();
            GirlCharacterRuntimeBridge bridge = root.AddComponent<GirlCharacterRuntimeBridge>();
            dialogueObject = new GameObject("Dialogue Test");
            dialogue = dialogueObject.AddComponent<Stage7DialogueView>();
            bridge.Configure(controller, dialogue);
            return bridge;
        }
    }
}
