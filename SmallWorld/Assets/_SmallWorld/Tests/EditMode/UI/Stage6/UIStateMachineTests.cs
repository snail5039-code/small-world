using NUnit.Framework;

namespace SmallWorld.UI.Tests
{
    public sealed class UIStateMachineTests
    {
        [Test]
        public void Set_NotifiesOnlyWhenStateChanges()
        {
            var machine = new UIStateMachine();
            int count = 0;
            machine.Changed += (_, __) => count++;

            Assert.That(machine.Set(UIState.Title), Is.False);
            Assert.That(machine.Set(UIState.Gameplay), Is.True);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(machine.Previous, Is.EqualTo(UIState.Title));
        }

        [Test]
        public void ReturnToPrevious_RestoresPriorState()
        {
            var machine = new UIStateMachine();
            machine.Set(UIState.Gameplay);
            machine.Set(UIState.Settings);

            machine.ReturnToPrevious();

            Assert.That(machine.Current, Is.EqualTo(UIState.Gameplay));
        }
    }
}

