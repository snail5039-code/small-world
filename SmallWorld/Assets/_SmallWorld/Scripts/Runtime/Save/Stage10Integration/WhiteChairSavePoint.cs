using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Save.Stage10.Integration
{
    public sealed class WhiteChairSavePoint : InteractableBase
    {
        [SerializeField] private RealityRoomSaveCoordinator coordinator;
        public void ConfigureSavePoint(RealityRoomSaveCoordinator value, params Renderer[] renderers) { coordinator = value; Configure("저장하기", renderers); }
        protected override void BeginInteraction(InteractionContext context)
        {
            bool saved = coordinator != null && coordinator.ReachWhiteChair();
            context.ShowFeedback(saved ? "하얀 의자에서 진행 상황을 저장했습니다." : "저장에 실패했습니다.");
            CompleteInteraction();
        }
    }
}
