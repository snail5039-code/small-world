using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class MemoryExitInteractable : InteractableBase
    {
        [SerializeField] private Stage12MemorySpaceController memorySpaceController;
        [SerializeField] private string blockedFeedback = "아직 나갈 수 없습니다. 기억 표식을 1 → 2 → 3 순서로 조사하세요.";

        public void ConfigureExit(Stage12MemorySpaceController controller,
            string interactionPrompt = "하얀 방으로 돌아가기")
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
                context.ShowFeedback("기억 공간 출구가 연결되지 않았습니다.");
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
