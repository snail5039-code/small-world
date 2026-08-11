using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class MemoryPuzzleChoiceInteractable : InteractableBase
    {
        [SerializeField] private Stage13MemoryPuzzleController puzzleController;
        [SerializeField, Range(1, 3)] private int choice = 1;

        public int Choice => choice;

        public void ConfigureChoice(Stage13MemoryPuzzleController controller, int value,
            string interactionPrompt = null)
        {
            puzzleController = controller;
            choice = Mathf.Clamp(value, 1, 3);
            Configure(string.IsNullOrWhiteSpace(interactionPrompt) ? $"Select memory {choice}" : interactionPrompt,
                GetComponentsInChildren<Renderer>(true));
        }

        private void Awake()
        {
            if (puzzleController == null)
                puzzleController = GetComponentInParent<Stage13MemoryPuzzleController>();
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            if (puzzleController == null)
            {
                context.ShowFeedback("The memory puzzle is unavailable.");
                CompleteInteraction();
                return;
            }

            bool accepted = puzzleController.SubmitChoice(choice);
            if (puzzleController.IsCompleted)
                context.ShowFeedback("The sequence is complete. The exit is open.");
            else
                context.ShowFeedback(accepted
                    ? $"Memory {choice} was added to the sequence."
                    : "That order was incorrect. The sequence has restarted.");
            CompleteInteraction();
        }
    }
}

