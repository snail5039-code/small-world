using NUnit.Framework;
using SmallWorld.Save.Stage12;

namespace SmallWorld.Tests.EditMode.Save.Stage12
{
    public sealed class MemoryPuzzleFlowTests
    {
        [Test]
        public void CorrectSequence_CompletesExactlyOnce()
        {
            var flow = new MemoryPuzzleFlow("puzzle", new[] { 1, 2, 3 });
            var state = flow.Start(null);

            Assert.That(flow.Submit(state, 1), Is.True);
            Assert.That(flow.Submit(state, 2), Is.True);
            Assert.That(flow.Submit(state, 3), Is.True);
            Assert.That(state.Completed, Is.True);
            Assert.That(state.Progress, Is.EqualTo(3));
            Assert.That(flow.Submit(state, 1), Is.False);
            Assert.That(state.Progress, Is.EqualTo(3));
        }

        [Test]
        public void WrongChoice_ResetsProgressAndCountsMistake()
        {
            var flow = new MemoryPuzzleFlow("puzzle", new[] { 1, 2, 3 });
            var state = flow.Start(null);
            flow.Submit(state, 1);

            Assert.That(flow.Submit(state, 9), Is.False);
            Assert.That(state.Progress, Is.Zero);
            Assert.That(state.Mistakes, Is.EqualTo(1));
            Assert.That(state.Completed, Is.False);
        }

        [Test]
        public void Normalize_ClampsCorruptSavedState()
        {
            var flow = new MemoryPuzzleFlow("puzzle", new[] { 1, 2, 3 });
            var state = flow.Normalize(new MemoryPuzzleState
            {
                PuzzleId = "puzzle", Progress = 99, Mistakes = -4
            });

            Assert.That(state.Progress, Is.EqualTo(3));
            Assert.That(state.Mistakes, Is.Zero);
            Assert.That(state.Completed, Is.True);
        }
    }
}
