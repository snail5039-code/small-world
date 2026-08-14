using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class StoryRouteRealityReturnInteractable : InteractableBase
    {
        [SerializeField] private StoryRouteController route;

        public void ConfigureReturn(StoryRouteController controller, string prompt = "현실방으로 돌아가기")
        {
            route = controller;
            Configure(prompt, GetComponentsInChildren<Renderer>(true));
        }

        protected override async void BeginInteraction(InteractionContext context)
        {
            if (route == null)
                context.ShowFeedback("현실방 복귀 장치가 연결되지 않았습니다.");
            else
            {
                StoryRouteReturnResult result = await route.ReturnToRealityRoomAsync();
                context.ShowFeedback(result.Feedback);
            }
            CompleteInteraction();
        }
    }
}
