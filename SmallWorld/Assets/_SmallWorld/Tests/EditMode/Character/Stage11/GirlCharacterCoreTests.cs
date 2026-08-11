using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Character.Stage11.Tests
{
    public sealed class GirlCharacterCoreTests
    {
        [Test]
        public void HelpfulActionsRaiseRelationshipAndChangeBehavior()
        {
            var state = new GirlCharacterState("girl", 30);

            Assert.That(state.React(PlayerAction.Help), Is.EqualTo(GirlBehavior.Approach));
            Assert.That(state.Relationship, Is.EqualTo(40));
            Assert.That(state.Mood, Is.EqualTo(GirlMood.Calm));
            Assert.That(state.InteractionCount, Is.EqualTo(1));
        }

        [Test]
        public void TrustedGirlSharesMemoryOnlyOnce()
        {
            var state = new GirlCharacterState("girl", 65);

            Assert.That(state.React(PlayerAction.Listen), Is.EqualTo(GirlBehavior.ShareMemory));
            Assert.That(state.SharedPrivateMemory, Is.True);
            Assert.That(state.React(PlayerAction.Listen), Is.EqualTo(GirlBehavior.Approach));
        }

        [Test]
        public void BrokenPromiseForcesHurtWithdrawal()
        {
            var state = new GirlCharacterState("girl", 80);

            Assert.That(state.React(PlayerAction.BreakPromise), Is.EqualTo(GirlBehavior.Withdraw));
            Assert.That(state.Mood, Is.EqualTo(GirlMood.Hurt));
            Assert.That(state.Relationship, Is.EqualTo(60));
        }

        [Test]
        public void DialogueBindingExposesStateForStage7ConditionsAndRestoresEffects()
        {
            var girl = new GirlCharacterState("girl", 42);
            girl.React(PlayerAction.Listen);
            var dialogue = new DialogueState();

            GirlDialogueBinding.WriteTo(girl, dialogue);
            Assert.That(dialogue.Get(GirlCharacterKeys.Relationship("girl")), Is.EqualTo(47));

            dialogue.Set(GirlCharacterKeys.Relationship("girl"), 75);
            dialogue.Set(GirlCharacterKeys.SharedMemory("girl"), 1);
            GirlDialogueBinding.ReadFrom(dialogue, girl);

            Assert.That(girl.Relationship, Is.EqualTo(75));
            Assert.That(girl.SharedPrivateMemory, Is.True);
        }

        [Test]
        public void SaveBindingRoundTripsWithoutRemovingOtherRelationships()
        {
            var source = new GirlCharacterState("girl", 68);
            source.React(PlayerAction.Help);
            var save = SaveData.CreateNew();
            save.Relationships.Add(new RelationshipSaveEntry { CharacterId = "other", Value = 9 });

            GirlSaveBinding.Capture(source, save);
            var restored = new GirlCharacterState("girl");

            Assert.That(GirlSaveBinding.Restore(save, restored), Is.True);
            Assert.That(restored.Relationship, Is.EqualTo(78));
            Assert.That(restored.LastPlayerAction, Is.EqualTo(PlayerAction.Help));
            Assert.That(restored.SharedPrivateMemory, Is.True);
            Assert.That(save.Relationships.Exists(entry => entry.CharacterId == "other" && entry.Value == 9), Is.True);
        }

        [Test]
        public void MissingCharacterSaveLeavesDefaultsUntouched()
        {
            var state = new GirlCharacterState("girl", 12);

            Assert.That(GirlSaveBinding.Restore(SaveData.CreateNew(), state), Is.False);
            Assert.That(state.Relationship, Is.EqualTo(12));
        }
    }
}
