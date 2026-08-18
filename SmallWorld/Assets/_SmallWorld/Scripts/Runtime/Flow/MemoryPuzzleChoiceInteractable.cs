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
            Configure(string.IsNullOrWhiteSpace(interactionPrompt) ? $"기억 {choice}번 선택" : interactionPrompt,
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
                context.ShowFeedback("기억 순서 장치가 연결되지 않았습니다.");
                CompleteInteraction();
                return;
            }

            bool accepted = puzzleController.SubmitChoice(choice);
            if (puzzleController.IsCompleted)
                context.ShowFeedback("기억 순서 1 → 2 → 3을 완성했습니다. 빛나는 출구로 돌아가세요.");
            else
                context.ShowFeedback(accepted
                    ? $"기억 {choice}번 확인 완료. 다음은 기억 {puzzleController.Progress + 1}번입니다."
                    : "순서가 다릅니다. 진행이 초기화되었습니다. 기억 1번부터 다시 조사하세요.");
            CompleteInteraction();
        }
    }
}
