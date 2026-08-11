using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Character.Stage11.Tests
{
    public sealed class GirlCharacterRuntimeBridgeTests
    {
        [Test]
        public void ReactPublishesRelationshipBehaviorToPresentation()
        {
            var presentation = new RecordingPresentation();
            var bridge = new GirlCharacterStateBridge(new GirlCharacterState("girl", 30), presentation);

            GirlBehavior result = bridge.React(PlayerAction.Help);

            Assert.That(result, Is.EqualTo(GirlBehavior.Approach));
            Assert.That(presentation.Last.Behavior, Is.EqualTo(GirlBehavior.Approach));
            Assert.That(presentation.Last.Relationship, Is.EqualTo(40));
            Assert.That(presentation.ApplyCount, Is.EqualTo(1));
        }

        [Test]
        public void DeathMemoryIsPresentedOnceAndPersistsAcknowledgement()
        {
            var presentation = new RecordingPresentation();
            var bridge = new GirlCharacterStateBridge(new GirlCharacterState("girl"), presentation);

            bridge.RememberDeath(DeathMemoryHandling.Comforted);

            Assert.That(presentation.Last.ReactToDeath, Is.True);
            Assert.That(presentation.Last.DeathCount, Is.EqualTo(1));
            Assert.That(presentation.Last.LastDeathHandling, Is.EqualTo(DeathMemoryHandling.Comforted));
            Assert.That(bridge.State.HasPendingDeathReaction, Is.False);

            bridge.PublishCurrentState();
            Assert.That(presentation.Last.ReactToDeath, Is.False);

            var save = SaveData.CreateNew();
            bridge.Capture(save);
            var restoredPresentation = new RecordingPresentation();
            var restored = new GirlCharacterStateBridge(new GirlCharacterState("girl"), restoredPresentation);
            Assert.That(restored.Restore(save), Is.True);
            Assert.That(restoredPresentation.Last.ReactToDeath, Is.False);
            Assert.That(restored.State.DeathCount, Is.EqualTo(1));
            Assert.That(restored.State.ReactedDeathCount, Is.EqualTo(1));
        }

        [Test]
        public void UnacknowledgedDeathFromSaveReactsOnceAfterRestore()
        {
            var source = new GirlCharacterState("girl");
            source.RememberDeath(DeathMemoryHandling.GivenSpace);
            var save = SaveData.CreateNew();
            GirlSaveBinding.Capture(source, save);
            var presentation = new RecordingPresentation();
            var bridge = new GirlCharacterStateBridge(new GirlCharacterState("girl"), presentation);

            Assert.That(bridge.Restore(save), Is.True);
            Assert.That(presentation.Last.ReactToDeath, Is.True);
            Assert.That(bridge.State.ReactedDeathCount, Is.EqualTo(1));

            bridge.PublishCurrentState();
            Assert.That(presentation.Last.ReactToDeath, Is.False);
        }

        [Test]
        public void DialogueRoundTripIncludesDeathMemoryContract()
        {
            var sourcePresentation = new RecordingPresentation();
            var source = new GirlCharacterStateBridge(new GirlCharacterState("girl", 50), sourcePresentation);
            source.State.RememberDeath(DeathMemoryHandling.Dismissed);
            var dialogue = new DialogueState();
            source.SynchronizeToDialogue(dialogue);
            var targetPresentation = new RecordingPresentation();
            var target = new GirlCharacterStateBridge(new GirlCharacterState("girl"), targetPresentation);

            target.SynchronizeFromDialogue(dialogue);

            Assert.That(target.State.DeathCount, Is.EqualTo(1));
            Assert.That(target.State.LastDeathHandling, Is.EqualTo(DeathMemoryHandling.Dismissed));
            Assert.That(targetPresentation.Last.ReactToDeath, Is.True);
        }

        private sealed class RecordingPresentation : IGirlCharacterPresentation
        {
            public GirlPresentationState Last { get; private set; }
            public int ApplyCount { get; private set; }

            public void ApplyGirlState(GirlPresentationState state)
            {
                Last = state;
                ApplyCount++;
            }
        }
    }
}
