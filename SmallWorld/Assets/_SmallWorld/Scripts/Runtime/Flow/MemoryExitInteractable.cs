using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class MemoryExitInteractable : InteractableBase
    {
        [SerializeField] private Stage12MemorySpaceController memorySpaceController;
        [SerializeField] private string blockedFeedback = "Complete the memory sequence before leaving.";

        public void ConfigureExit(Stage12MemorySpaceController controller,
            string interactionPrompt = "Return to the white room")
        {
            memorySpaceController = controller;
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
        }

        private void Awake()
        {
            if (memorySpaceController == null)
                memorySpaceController = GetComponentInParent<Stage12MemorySpaceController>();
        }

        protected override async void BeginInteraction(InteractionContext context)
        {
            if (memorySpaceController == null)
            {
                context.ShowFeedback("The memory-space controller is unavailable.");
                CompleteInteraction();
                return;
            }

            await memorySpaceController.ReturnToWhiteRoom();
            if (memorySpaceController != null && memorySpaceController.IsExitBlocked)
                context.ShowFeedback(blockedFeedback);
            CompleteInteraction();
        }
    }
}

