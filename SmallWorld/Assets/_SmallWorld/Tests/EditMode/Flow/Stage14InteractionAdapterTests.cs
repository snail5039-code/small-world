#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage14InteractionAdapterTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("stage13.memory-puzzle.first-memory-sequence.progress");
            PlayerPrefs.DeleteKey("stage13.memory-puzzle.first-memory-sequence.completed");
            PlayerPrefs.DeleteKey("stage13.memory-puzzle.first-memory-sequence.mistakes");
        }

        [Test]
        public void PuzzleChoice_ForwardsConfiguredChoice()
        {
            GameObject root = new GameObject("MemoryPuzzle");
            try
            {
                Stage13MemoryPuzzleController puzzle = root.AddComponent<Stage13MemoryPuzzleController>();
                MemoryPuzzleChoiceInteractable choice = root.AddComponent<MemoryPuzzleChoiceInteractable>();
                choice.ConfigureChoice(puzzle, 1);

                Assert.That(choice.TryInteract(default), Is.True);
                Assert.That(choice.Choice, Is.EqualTo(1));
                Assert.That(puzzle.Progress, Is.EqualTo(1));
                Assert.That(choice.IsBusy, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PuzzleChoices_CompleteOneTwoThreeSequence()
        {
            GameObject root = new GameObject("MemoryPuzzle");
            try
            {
                Stage13MemoryPuzzleController puzzle = root.AddComponent<Stage13MemoryPuzzleController>();
                for (int value = 1; value <= 3; value++)
                {
                    MemoryPuzzleChoiceInteractable choice = root.AddComponent<MemoryPuzzleChoiceInteractable>();
                    choice.ConfigureChoice(puzzle, value);
                    Assert.That(choice.TryInteract(new InteractionContext(null, null)), Is.True);
                }

                Assert.That(puzzle.IsCompleted, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
