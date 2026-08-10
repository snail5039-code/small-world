using SmallWorld.Player;

namespace SmallWorld.Puzzle.Stage9Integration
{
    public sealed class PhotoPuzzleInteractable : InteractableBase
    {
        private PhotoPuzzleView puzzleView;

        public void ConfigurePuzzle(PhotoPuzzleView view, params UnityEngine.Renderer[] renderers)
        {
            puzzleView = view;
            Configure("사진 조각 맞추기", renderers);
        }

        public override bool CanInteract => base.CanInteract && puzzleView != null && !puzzleView.IsCompleted;

        protected override void BeginInteraction(InteractionContext context)
        {
            if (!puzzleView.Open()) context.ShowFeedback("지금은 사진 퍼즐을 열 수 없습니다.");
            CompleteInteraction();
        }
    }
}
