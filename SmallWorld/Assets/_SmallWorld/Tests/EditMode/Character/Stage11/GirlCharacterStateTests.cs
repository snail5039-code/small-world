using NUnit.Framework;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Character.Stage11.Tests
{
    public sealed class GirlCharacterStateTests
    {
        [Test]
        public void React_SameInputsProduceSameObservableState()
        {
            var first = new GirlCharacterState("girl");
            var second = new GirlCharacterState("girl");
            PlayerAction[] actions = { PlayerAction.Greet, PlayerAction.Listen, PlayerAction.Help, PlayerAction.Ignore };

            foreach (PlayerAction action in actions)
                Assert.That(first.React(action), Is.EqualTo(second.React(action)));

            Assert.That(first.Relationship, Is.EqualTo(second.Relationship));
            Assert.That(first.Mood, Is.EqualTo(second.Mood));
            Assert.That(first.LastPlayerAction, Is.EqualTo(second.LastPlayerAction));
            Assert.That(first.InteractionCount, Is.EqualTo(second.InteractionCount));
            Assert.That(first.SharedPrivateMemory, Is.EqualTo(second.SharedPrivateMemory));
        }

        [Test]
        public void React_ClampsRelationshipAtBothBounds()
        {
            var high = new GirlCharacterState("girl", 99);
            var low = new GirlCharacterState("girl", -99);

            high.React(PlayerAction.Help);
            low.React(PlayerAction.BreakPromise);

            Assert.That(high.Relationship, Is.EqualTo(GirlCharacterState.MaximumRelationship));
            Assert.That(low.Relationship, Is.EqualTo(GirlCharacterState.MinimumRelationship));
        }

        [TestCase(9, PlayerAction.Greet, GirlBehavior.Observe)]
        [TestCase(34, PlayerAction.Greet, GirlBehavior.Approach)]
        [TestCase(60, PlayerAction.Help, GirlBehavior.ShareMemory)]
        [TestCase(-19, PlayerAction.Ignore, GirlBehavior.Withdraw)]
        public void React_SelectsBehaviorAcrossRelationshipBoundaries(
            int initialRelationship, PlayerAction action, GirlBehavior expected)
        {
            var state = new GirlCharacterState("girl", initialRelationship);

            Assert.That(state.React(action), Is.EqualTo(expected));
        }

        [Test]
        public void ShareMemory_IsEmittedOnlyOnce()
        {
            var state = new GirlCharacterState("girl", 65);

            Assert.That(state.React(PlayerAction.Help), Is.EqualTo(GirlBehavior.ShareMemory));
            Assert.That(state.React(PlayerAction.Listen), Is.EqualTo(GirlBehavior.Approach));
            Assert.That(state.SharedPrivateMemory, Is.True);
        }

        [Test]
        public void DialogueBinding_RoundTripsAllCharacterState()
        {
            var source = new GirlCharacterState("girl", 65);
            source.React(PlayerAction.Help);
            var dialogue = new DialogueState();
            GirlDialogueBinding.WriteTo(source, dialogue);
            var restored = new GirlCharacterState("girl", -50);

            GirlDialogueBinding.ReadFrom(dialogue, restored);

            AssertEquivalent(source, restored);
        }

        [Test]
        public void SaveBinding_RoundTripsAndReplacesExistingEntriesForSameCharacter()
        {
            var save = new SaveData();
            var old = new GirlCharacterState("girl", -50);
            old.React(PlayerAction.Ignore);
            GirlSaveBinding.Capture(old, save);
            var source = new GirlCharacterState("girl", 65);
            source.React(PlayerAction.Help);

            GirlSaveBinding.Capture(source, save);
            var restored = new GirlCharacterState("girl", -100);

            Assert.That(GirlSaveBinding.Restore(save, restored), Is.True);
            AssertEquivalent(source, restored);
            Assert.That(save.Relationships.Count, Is.EqualTo(5));
        }

        [Test]
        public void SaveBinding_PreservesOtherCharactersEntries()
        {
            var save = new SaveData();
            GirlSaveBinding.Capture(new GirlCharacterState("other", 12), save);
            GirlSaveBinding.Capture(new GirlCharacterState("girl", 42), save);

            var other = new GirlCharacterState("other");

            Assert.That(GirlSaveBinding.Restore(save, other), Is.True);
            Assert.That(other.Relationship, Is.EqualTo(12));
            Assert.That(save.Relationships.Count, Is.EqualTo(10));
        }

        [Test]
        public void SaveBinding_MissingCharacterDoesNotMutateDestination()
        {
            var destination = new GirlCharacterState("girl", 25);

            Assert.That(GirlSaveBinding.Restore(new SaveData(), destination), Is.False);
            Assert.That(destination.Relationship, Is.EqualTo(25));
            Assert.That(destination.InteractionCount, Is.Zero);
        }

        private static void AssertEquivalent(GirlCharacterState expected, GirlCharacterState actual)
        {
            Assert.That(actual.CharacterId, Is.EqualTo(expected.CharacterId));
            Assert.That(actual.Relationship, Is.EqualTo(expected.Relationship));
            Assert.That(actual.Mood, Is.EqualTo(expected.Mood));
            Assert.That(actual.LastPlayerAction, Is.EqualTo(expected.LastPlayerAction));
            Assert.That(actual.InteractionCount, Is.EqualTo(expected.InteractionCount));
            Assert.That(actual.SharedPrivateMemory, Is.EqualTo(expected.SharedPrivateMemory));
        }
    }
}
