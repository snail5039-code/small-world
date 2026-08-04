using System;
using NUnit.Framework;

namespace SmallWorld.Dialogue.Stage7.Tests
{
    public sealed class DialogueSessionTests
    {
        [Test]
        public void Choice_FiltersByPlayerState_AndChangesRelationship()
        {
            var state = new DialogueState();
            state.Set("player.hasKey", 1);
            var choice = new DialogueChoice("trust", "Tell the truth", "end",
                new[] { new DialogueCondition("player.hasKey", 1, DialogueComparison.Equal) },
                new[] { new DialogueEffect("relationship.mita", 2) });
            var hidden = new DialogueChoice("locked", "Impossible", "end",
                new[] { new DialogueCondition("player.hasKey", 0, DialogueComparison.Equal) });
            var definition = new DialogueDefinition("intro", "start", new[]
            {
                new DialogueNode("start", "Mita", "Did you find it?", choices: new[] { choice, hidden }),
                new DialogueNode("end", "Mita", "Thank you.")
            });

            var session = new DialogueSession(definition, state);

            Assert.That(session.Current.SpeakerName, Is.EqualTo("Mita"));
            Assert.That(session.Current.Choices.Count, Is.EqualTo(1));
            session.SelectChoice("trust");
            Assert.That(state.Get("relationship.mita"), Is.EqualTo(2));
            Assert.That(session.Current.NodeId, Is.EqualTo("end"));
            Assert.That(session.History[1].ChoiceId, Is.EqualTo("trust"));
        }

        [Test]
        public void ConditionalNode_UsesFallbackForDifferentPlayerState()
        {
            var definition = new DialogueDefinition("state", "conditional", new[]
            {
                new DialogueNode("conditional", "Mita", "You remember.", conditions: new[]
                {
                    new DialogueCondition("player.remembers", 1, DialogueComparison.Equal)
                }, fallbackNodeId: "fallback"),
                new DialogueNode("fallback", "Mita", "You look confused.")
            });

            var session = new DialogueSession(definition, new DialogueState());

            Assert.That(session.Current.NodeId, Is.EqualTo("fallback"));
            Assert.That(session.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AutoAdvancesOnlyAfterConfiguredDelay()
        {
            var definition = LinearDefinition(1.5f);
            var session = new DialogueSession(definition, new DialogueState());

            Assert.That(session.Tick(1f), Is.False);
            Assert.That(session.Tick(0.5f), Is.True);
            Assert.That(session.Current.NodeId, Is.EqualTo("second"));
        }

        [Test]
        public void Skip_StopsAtChoice_ThenCanFinishAfterSelection()
        {
            var definition = new DialogueDefinition("skip", "first", new[]
            {
                new DialogueNode("first", "A", "One", "choice"),
                new DialogueNode("choice", "A", "Choose", choices: new[] { new DialogueChoice("ok", "OK", "last") }),
                new DialogueNode("last", "A", "Last")
            });
            var session = new DialogueSession(definition, new DialogueState());

            session.Skip();
            Assert.That(session.Current.NodeId, Is.EqualTo("choice"));
            session.SelectChoice("ok");
            session.Skip();

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.History.Count, Is.EqualTo(4));
        }

        [Test]
        public void Skip_ThrowsWhenUnconditionalNodesCycle()
        {
            var definition = new DialogueDefinition("cycle", "a", new[]
            {
                new DialogueNode("a", "A", "One", "b"),
                new DialogueNode("b", "B", "Two", "a")
            });
            var session = new DialogueSession(definition, new DialogueState());

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => session.Skip());

            Assert.That(exception.Message, Does.Contain("skip cycle"));
            Assert.That(exception.Message, Does.Contain("a"));
            Assert.That(session.IsComplete, Is.False);
        }

        [Test]
        public void Tick_OnAutoAdvanceCycle_AdvancesExactlyOneNodePerCall()
        {
            var definition = new DialogueDefinition("auto-cycle", "a", new[]
            {
                new DialogueNode("a", "A", "One", "b", autoAdvanceSeconds: 0.1f),
                new DialogueNode("b", "B", "Two", "a", autoAdvanceSeconds: 0.1f)
            });
            var session = new DialogueSession(definition, new DialogueState());

            Assert.That(session.Tick(10f), Is.True);
            Assert.That(session.Current.NodeId, Is.EqualTo("b"));
            Assert.That(session.History.Count, Is.EqualTo(2));

            Assert.That(session.Tick(10f), Is.True);
            Assert.That(session.Current.NodeId, Is.EqualTo("a"));
            Assert.That(session.History.Count, Is.EqualTo(3));
        }

        [Test]
        public void MenuConditions_UseSharedState()
        {
            var definition = new DialogueDefinition("menu", "only", new[] { new DialogueNode("only", "", "Text") },
                new[] { new DialogueCondition("story.chapter", 3, DialogueComparison.GreaterOrEqual) });
            var state = new DialogueState();

            Assert.That(definition.CanShowInMenu(state), Is.False);
            state.Set("story.chapter", 3);
            Assert.That(definition.CanShowInMenu(state), Is.True);
        }

        [Test]
        public void Definition_RejectsBrokenLinks()
        {
            Assert.Throws<ArgumentException>(() => new DialogueDefinition("bad", "start", new[]
            {
                new DialogueNode("start", "A", "Text", "missing")
            }));
        }

        private static DialogueDefinition LinearDefinition(float autoAdvance)
        {
            return new DialogueDefinition("linear", "first", new[]
            {
                new DialogueNode("first", "A", "One", "second", autoAdvanceSeconds: autoAdvance),
                new DialogueNode("second", "B", "Two")
            });
        }
    }
}
