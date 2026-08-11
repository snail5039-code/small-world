using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class StoryRouteEntryInteractable : InteractableBase
    {
        private const string StorySceneName = "04_StoryRoute";

        public void ConfigureEntry() => Configure("Enter the chapter route", GetComponentsInChildren<Renderer>(true));

        protected override async void BeginInteraction(InteractionContext context)
        {
            if (SceneTransitionService.Instance == null)
            {
                context.ShowFeedback("The scene transition service is unavailable.");
                CompleteInteraction();
                return;
            }

            context.ShowFeedback("Opening the chapter route.");
            await SceneTransitionService.Instance.LoadPlayingSceneAsync(StorySceneName);
            CompleteInteraction();
        }
    }
}

