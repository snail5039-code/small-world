using SmallWorld.Save.Story;

namespace SmallWorld.Flow
{
    public static class StoryRouteGuidance
    {
        public static string Location(StoryChapterId chapter)
        {
            switch (chapter)
            {
                case StoryChapterId.Prologue: return "프롤로그 · 이미 실행된 게임";
                case StoryChapterId.Chapter1: return "1장 · 네 번째 자리";
                case StoryChapterId.Chapter2: return "2장 · 마지막 승강장";
                case StoryChapterId.Chapter3: return "3장 · 완벽한 하루";
                case StoryChapterId.Chapter4: return "4장 · 얼굴 없는 사무실";
                case StoryChapterId.Chapter5: return "5장 · 장례식 없는 묘지";
                case StoryChapterId.Chapter6: return "6장 · 창문 안의 도시";
                default: return "최종장 · 아무것도 남지 않은 하얀 방";
            }
        }

        public static string ArrivalObjective(StoryChapterId chapter)
        {
            switch (chapter)
            {
                case StoryChapterId.Prologue: return "유나와 대화하고 방 안의 이상한 물건을 조사한다.";
                case StoryChapterId.Chapter1: return "가족의 목소리를 듣고 멈춘 저녁 식사의 시각을 복원한다.";
                case StoryChapterId.Chapter2: return "도현의 기억과 전광판을 조사해 존재하지 않는 노선을 잇는다.";
                case StoryChapterId.Chapter3: return "유나와 대화한 뒤 완벽한 하루의 반복 규칙을 깨뜨린다.";
                case StoryChapterId.Chapter4: return "사원증 권한을 바꾸며 변하지 않는 삭제 명령을 복구한다.";
                case StoryChapterId.Chapter5: return "서로 모순되는 죽음의 기록이 모두 거짓임을 증명한다.";
                case StoryChapterId.Chapter6: return "시간·가구·비의 방향으로 현실 개발자의 창문을 찾는다.";
                default: return "살아 있는 집으로 들어가되, 최종 선택은 실행하지 않는다.";
            }
        }

        public static string ArrivalDialogue(StoryProgress progress, int relationship)
        {
            StoryChoiceState promise = progress?.ImportantChoices?.Find(x => x.ChoiceId == "prologue-stay");
            if (relationship >= 10 || promise?.OutcomeId == "stay")
                return "유나: “기억해 줬네. 이번에도 내가 다음 표식을 알려 줄게.”";
            if (relationship < 0 || promise?.OutcomeId == "escape")
                return "유나: “나갈 길을 찾는 건 이해해. 그래도 혼자 먼저 가지는 마.”";
            return "유나: “빛나는 조사 표식을 따라와. 막히면 방금 본 단서를 다시 확인해.”";
        }

        public static string NextObjective(StoryChapterId chapter, OpeningStoryAction action, bool accepted)
        {
            if (!accepted) return "잠김 사유를 확인하고, 직전의 빛나는 조사 표식부터 살핀다.";
            switch (action)
            {
                case OpeningStoryAction.MeetYuna: return "소파를 배치한 뒤 열쇠·찻잔·사진 조각을 조사한다.";
                case OpeningStoryAction.PlaceSofa: return "방 안의 세 조사 표식에서 기억 조각을 찾는다.";
                case OpeningStoryAction.ReadScheduledMail: return "모형 집의 첫 기억 문으로 돌아간다.";
                case OpeningStoryAction.SetClock1942: return "탄 달걀·반쪽 사과·식은 수프·빈 그릇으로 네 번째 식사를 완성한다.";
                case OpeningStoryAction.OpenSilentDoor: return "네 번째 자리의 이름표를 선택한다.";
                case OpeningStoryAction.PullFrontDoor: return "현관문 표식으로 돌아가 집에 귀환한다.";
                case OpeningStoryAction.ReadPlatformBoard: return "삭제된 이름의 접속 시간 네 개를 순서대로 연결한다.";
                case OpeningStoryAction.ReverseAnnouncement3: return "열차의 목적지를 고른 뒤 조명이 켜진 안전 구역으로 이동한다.";
                case OpeningStoryAction.CrossSafeZone3: return "귀환 표식을 따라 게임 속 집으로 돌아간다.";
                case OpeningStoryAction.EnterMinaMemory: return "카페 메뉴판을 뒤집어 민아가 원한 쓴 커피를 찾는다.";
                case OpeningStoryAction.ChooseUnknownPreference: return "공원의 그림자를 움직여 멈춘 석양을 진행시킨다.";
                case OpeningStoryAction.TearPerfectPhoto:
                case OpeningStoryAction.PreservePerfectPhoto: return "출구 표식으로 돌아가 완벽한 하루를 끝낸다.";
                case OpeningStoryAction.EnterFacelessOffice: return "연구원과 개발자 사원증을 번갈아 장착한다.";
                case OpeningStoryAction.AlignMirrorSeat3: return "어떤 개발자 기록을 믿을지 정한 뒤 퇴근 방송의 안전 지점을 따른다.";
                case OpeningStoryAction.EscapeOfficeCheckpoint3: return "귀환 표식을 따라 얼굴 없는 사무실을 나간다.";
                case OpeningStoryAction.EnterGravelessFuneral: return "네 장의 사망진단서를 겹쳐 같은 인쇄 오류를 찾는다.";
                case OpeningStoryAction.InspectGravestoneBack: return "이름을 만들지, 빈 이름으로 확정할지 결정한다.";
                case OpeningStoryAction.ConfirmBlankDeadName:
                case OpeningStoryAction.EnterInventedDeadName: return "귀환 표식을 따라 이름 없는 묘지를 나간다.";
                case OpeningStoryAction.EnterWindowCityLastRoom: return "시간·가구·비의 방향이 일치하는 현실의 창문을 찾는다.";
                case OpeningStoryAction.OverlayAdminGirlWaveform3: return "현실 연결 상태를 결정한 뒤 동시에 열린 창문을 관찰한다.";
                case OpeningStoryAction.CarryCollapsingCity3: return "마지막 귀환 표식으로 도시를 집에 가져간다.";
                case OpeningStoryAction.EnterLivingHouse: return "기억 가구를 보존하거나 파괴하며 관리 핵심으로 향한다.";
                case OpeningStoryAction.EnterWhiteRoom: return "두 의자와 낡은 컴퓨터를 차례로 조사한다.";
                case OpeningStoryAction.PrepareFinalChoice: return "최종 선택 준비 완료. 여기서 멈춘다 — 엔딩 선택은 실행하지 않는다.";
                default: return ArrivalObjective(chapter);
            }
        }
    }
}
