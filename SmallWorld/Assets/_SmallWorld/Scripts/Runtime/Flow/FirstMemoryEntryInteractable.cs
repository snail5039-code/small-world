using SmallWorld.Core;
using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class FirstMemoryEntryInteractable : InteractableBase
    {
        public void ConfigureEntry(string interactionPrompt = "Enter the first memory")
        {
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
        }

        protected override async void BeginInteraction(InteractionContext context)
        {
            if (SceneTransitionService.Instance == null)
            {
                context.ShowFeedback("The scene transition service is unavailable.");
                CompleteInteraction();
                return;
            }

            context.ShowFeedback("Entering the first memory.");
            await SceneTransitionService.Instance.LoadSceneAsync(SceneId.FirstMemory);
            CompleteInteraction();
        }
    }
}

