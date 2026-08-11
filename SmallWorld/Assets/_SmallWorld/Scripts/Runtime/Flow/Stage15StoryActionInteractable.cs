using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class Stage15StoryActionInteractable : InteractableBase
    {
        [SerializeField] private StoryRouteProgressAdapter progress;
        [SerializeField] private OpeningStoryAction action;

        public void ConfigureAction(StoryRouteProgressAdapter adapter, OpeningStoryAction storyAction, string prompt)
        {
            progress = adapter;
            action = storyAction;
            Configure(prompt, GetComponentsInChildren<Renderer>(true));
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            context.ShowFeedback(progress == null ? "이야기 진행 장치가 연결되지 않았다." : progress.PerformOpeningAction(action).Message);
            CompleteInteraction();
        }
    }
}
