using System;
using System.Collections.Generic;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Flow
{
    public enum OpeningStoryAction
    {
        MeetYuna,
        PlaceSofa,
        FindKey,
        FindTeacup,
        FindPhotoFragment,
        ChooseStayPromise,
        ChooseEscapeFirst,
        ChooseUncertain,
        ReadScheduledMail,
        OpenMemoryDoor,
        QuestionMemoryDoor,
        LeaveMemoryDoorClosed,
        HearFather,
        HearMother,
        HearChild,
        SetClock1942,
        SetWrongClock,
        AddBurntEgg,
        AddAppleHalf,
        AddColdSoup,
        AddEmptyBowl,
        ArrangePhoto1,
        ArrangePhoto2,
        ArrangePhoto3,
        ArrangePhoto4,
        ArrangePhoto5,
        ArrangePhoto6,
        OpenSilentDoor,
        OpenFalseDoor,
        SeatSeoyun,
        SeatPlayer,
        SeatYuna,
        RotateKitchenDoor,
        MoveSofa,
        TurnFrame,
        PullFrontDoor,
        ReturnHome,
        HearDohyeon,
        ReadPlatformBoard,
        ConnectLoginTime1,
        ConnectLoginTime2,
        ConnectLoginTime3,
        ConnectLoginTime4,
        ReturnEmployeeCard,
        ReturnChildShoe,
        ReturnHospitalBand,
        ReturnGameCartridge,
        ReturnItemToWrongShadow,
        ReverseAnnouncement1,
        ReverseAnnouncement2,
        ReverseAnnouncement3,
        ChooseRealityHome,
        ChooseGameHouse,
        ChooseWhiteStation,
        CrossSafeZone1,
        CrossSafeZone2,
        CrossSafeZone3,
        ReturnFromPlatform
    }

    public readonly struct OpeningStoryResult
    {
        public OpeningStoryResult(bool accepted, string message)
        {
            Accepted = accepted;
            Message = message;
        }

        public bool Accepted { get; }
        public string Message { get; }
    }

    public sealed class Stage15OpeningStoryService
    {
        private const string GirlId = "girl";
        private const string Prefix = "s15-opening:";
        private readonly StoryFlowService storyFlow = new StoryFlowService();
        private readonly StoryRelationshipService relationships = new StoryRelationshipService();

        public OpeningStoryResult TryPerform(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (Has(progress, action)) return Reject("이미 확인한 기억이다.");

            return progress.CurrentChapter == StoryChapterId.Prologue
                ? PerformPrologue(save, progress, action)
                : progress.CurrentChapter == StoryChapterId.Chapter1
                    ? PerformChapterOne(save, progress, action)
                    : progress.CurrentChapter == StoryChapterId.Chapter2
                        ? PerformChapterTwo(save, progress, action)
                        : Reject("이 기억은 지금 열 수 없다.");
        }

        private OpeningStoryResult PerformPrologue(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            switch (action)
            {
                case OpeningStoryAction.MeetYuna:
                    return Accept(progress, action, "유나: \"우리 집을 완성해 주러 온 거지? ...이번에는 유나라고 불러줘.\"");
                case OpeningStoryAction.PlaceSofa:
                    if (!Has(progress, OpeningStoryAction.MeetYuna)) return Need("먼저 화면 속 소녀와 이야기해야 한다.");
                    return Accept(progress, action, "소파가 거실과 모형 집에 동시에 놓였다. 문 앞은 비워 두었다.");
                case OpeningStoryAction.FindKey:
                case OpeningStoryAction.FindTeacup:
                case OpeningStoryAction.FindPhotoFragment:
                    if (!Has(progress, OpeningStoryAction.PlaceSofa)) return Need("소파를 배치한 뒤 낡은 사진을 조사할 수 있다.");
                    string whisper = action == OpeningStoryAction.FindKey ? "문을 열지 마." :
                        action == OpeningStoryAction.FindTeacup ? "두 명이 아니었어." : "이번에는 기억해 줘.";
                    return Accept(progress, action, whisper);
                case OpeningStoryAction.ChooseStayPromise:
                case OpeningStoryAction.ChooseEscapeFirst:
                case OpeningStoryAction.ChooseUncertain:
                    if (!FoundAllObjects(progress)) return Need("사진 속 열쇠, 찻잔, 사진 조각을 모두 찾아야 한다.");
                    if (MadePrologueChoice(progress)) return Reject("이미 이 집에 대한 대답을 골랐다.");
                    string outcome = action == OpeningStoryAction.ChooseStayPromise ? "stay" : action == OpeningStoryAction.ChooseEscapeFirst ? "escape" : "uncertain";
                    storyFlow.RecordChoice(progress, "prologue-stay", outcome);
                    if (action == OpeningStoryAction.ChooseStayPromise) relationships.Set(save, GirlId, relationships.Get(save, GirlId) + 10);
                    if (action == OpeningStoryAction.ChooseEscapeFirst) relationships.Set(save, GirlId, relationships.Get(save, GirlId) - 5);
                    return Accept(progress, action, action == OpeningStoryAction.ChooseUncertain ? "선택지가 순간 '아직 배우는 중이야'로 바뀌었다." : "유나가 대답을 기억했다.");
                case OpeningStoryAction.ReadScheduledMail:
                    if (!MadePrologueChoice(progress)) return Need("액자를 놓고 유나의 질문에 먼저 답해야 한다.");
                    storyFlow.SetFlag(progress, "scheduled-mail-seven-years", false);
                    return Accept(progress, action, "7년 전의 나: '대답하지 마. 집을 완성하면 안 돼.'");
                case OpeningStoryAction.OpenMemoryDoor:
                case OpeningStoryAction.QuestionMemoryDoor:
                case OpeningStoryAction.LeaveMemoryDoorClosed:
                    if (!Has(progress, OpeningStoryAction.ReadScheduledMail)) return Need("현실 방의 예약 메일부터 읽어야 한다.");
                    if (ChoseMemoryDoor(progress)) return Reject("첫 기억 문에 대한 선택은 이미 끝났다.");
                    string door = action == OpeningStoryAction.OpenMemoryDoor ? "open" : action == OpeningStoryAction.QuestionMemoryDoor ? "question" : "leave";
                    storyFlow.RecordChoice(progress, "first-memory-door", door);
                    storyFlow.SetFlag(progress, "repeat-109", false);
                    Mark(progress, action);
                    CompleteChapter(progress, StoryChapterId.Prologue);
                    return new OpeningStoryResult(true, action == OpeningStoryAction.LeaveMemoryDoorClosed
                        ? "문을 닫았지만 다시 보니 이미 열려 있다. 반복 109 — 관찰 계속."
                        : "관리 AI: '기억 복구 가능 상태.' 반복 109 — 관찰 계속.");
                default:
                    return Reject("이 행동은 프롤로그의 흐름과 맞지 않는다.");
            }
        }

        private OpeningStoryResult PerformChapterOne(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            switch (action)
            {
                case OpeningStoryAction.HearFather:
                    return Accept(progress, action, "아버지: \"처음부터 세 식구였다.\"");
                case OpeningStoryAction.HearMother:
                    return Accept(progress, action, "어머니: \"한 사람분이 남아야 해.\"");
                case OpeningStoryAction.HearChild:
                    return Accept(progress, action, "아이: \"누나는 얼굴이 자꾸 달라져.\"");
                case OpeningStoryAction.SetWrongClock:
                    if (!HeardFamily(progress)) return Need("세 가족의 서로 다른 증언을 먼저 들어야 한다.");
                    return new OpeningStoryResult(true, "가족들: \"그 시간에는 아무도 없었어.\" 정답은 현실과 기억에 함께 남아 있다.");
                case OpeningStoryAction.SetClock1942:
                    if (!HeardFamily(progress)) return Need("세 가족의 서로 다른 증언을 먼저 들어야 한다.");
                    return Accept(progress, action, "네 시계가 19:42를 가리킨다. 빈자리에서 웃음소리가 들렸다.");
                case OpeningStoryAction.AddBurntEgg:
                case OpeningStoryAction.AddAppleHalf:
                case OpeningStoryAction.AddColdSoup:
                case OpeningStoryAction.AddEmptyBowl:
                    if (!Has(progress, OpeningStoryAction.SetClock1942)) return Need("멈춘 저녁 식사의 시각부터 복원해야 한다.");
                    if (FoodCount(progress) >= 3) return Reject("네 번째 접시에는 기억 세 조각만 담을 수 있다.");
                    if (action == OpeningStoryAction.AddEmptyBowl) storyFlow.SetFlag(progress, "seoyun-deletion-record", true);
                    return Accept(progress, action, "네 번째 접시에 기억 재료를 올렸다.");
                case OpeningStoryAction.ArrangePhoto1:
                case OpeningStoryAction.ArrangePhoto2:
                case OpeningStoryAction.ArrangePhoto3:
                case OpeningStoryAction.ArrangePhoto4:
                case OpeningStoryAction.ArrangePhoto5:
                case OpeningStoryAction.ArrangePhoto6:
                    if (FoodCount(progress) != 3) return Need("네 번째 접시에 기억 재료 세 조각을 담아야 한다.");
                    int expected = PhotoCount(progress) + (int)OpeningStoryAction.ArrangePhoto1;
                    if ((int)action != expected) return Reject("아이의 그림과 가구 위치를 비교하면 사진의 순서가 다르다.");
                    return Accept(progress, action, action == OpeningStoryAction.ArrangePhoto6 ? "서윤은 삭제되지 않았다. 배분되었다." : "사진 한 장이 올바른 시간에 놓였다.");
                case OpeningStoryAction.OpenFalseDoor:
                    if (PhotoCount(progress) != 6) return Need("가족사진 여섯 장을 먼저 시간순으로 복원해야 한다.");
                    return new OpeningStoryResult(true, "가족 목소리를 따라가자 같은 식탁으로 되돌아왔다.");
                case OpeningStoryAction.OpenSilentDoor:
                    if (PhotoCount(progress) != 6) return Need("가족사진 여섯 장을 먼저 시간순으로 복원해야 한다.");
                    return Accept(progress, action, "청록색 흠집이 난 조용한 문 너머에서 서윤의 마지막 대화를 보았다.");
                case OpeningStoryAction.SeatSeoyun:
                case OpeningStoryAction.SeatPlayer:
                case OpeningStoryAction.SeatYuna:
                    if (!Has(progress, OpeningStoryAction.OpenSilentDoor)) return Need("반복 복도에서 존재하지 않는 방을 찾아야 한다.");
                    if (MadeSeatChoice(progress)) return Reject("네 번째 자리의 이름은 이미 정해졌다.");
                    string seat = action == OpeningStoryAction.SeatSeoyun ? "seoyun" : action == OpeningStoryAction.SeatPlayer ? "player" : "yuna";
                    storyFlow.RecordChoice(progress, "fourth-seat-name", seat);
                    if (action == OpeningStoryAction.SeatSeoyun) storyFlow.SetFlag(progress, "victim-seoyun-restored", false);
                    if (action == OpeningStoryAction.SeatPlayer) relationships.Set(save, GirlId, relationships.Get(save, GirlId) + 10);
                    if (action == OpeningStoryAction.SeatYuna) storyFlow.SetFlag(progress, "yuna-seoyun-assimilation", true);
                    return Accept(progress, action, "이름표가 놓이자 얼굴 없는 가족이 일어나 출구를 막았다.");
                case OpeningStoryAction.RotateKitchenDoor:
                    if (!MadeSeatChoice(progress)) return Need("네 번째 자리에 놓을 이름부터 선택해야 한다.");
                    return Accept(progress, action, "모형 집의 부엌 문을 돌려 실제 복도와 연결했다.");
                case OpeningStoryAction.MoveSofa:
                    if (!Has(progress, OpeningStoryAction.RotateKitchenDoor)) return Need("먼저 부엌 문으로 탈출 경로를 만들어야 한다.");
                    return Accept(progress, action, "소파가 아버지의 길을 막았다.");
                case OpeningStoryAction.TurnFrame:
                    if (!Has(progress, OpeningStoryAction.MoveSofa)) return Need("소파로 뒤따르는 가족을 막아야 한다.");
                    return Accept(progress, action, "액자를 뒤집자 집이 주인공의 얼굴을 잃어버렸다.");
                case OpeningStoryAction.PullFrontDoor:
                    if (!Has(progress, OpeningStoryAction.TurnFrame)) return Need("집이 얼굴을 알아보지 못하게 액자를 뒤집어야 한다.");
                    return Accept(progress, action, "현관문을 모형 집 바깥으로 빼내 출구를 만들었다.");
                case OpeningStoryAction.ReturnHome:
                    if (!Has(progress, OpeningStoryAction.PullFrontDoor)) return Need("추격을 끝내고 현관문 출구를 만들어야 한다.");
                    Mark(progress, action);
                    storyFlow.SetFlag(progress, "furniture-four-seat-table", false);
                    storyFlow.SetFlag(progress, "furniture-family-frame", false);
                    storyFlow.SetFlag(progress, "dollhouse-basement-key", false);
                    CompleteChapter(progress, StoryChapterId.Chapter1);
                    return new OpeningStoryResult(true, "하얀 거실로 돌아왔다. 식탁과 액자, 모형 집 지하실 열쇠를 얻었다.");
                default:
                    return Reject("이 행동은 '네 번째 자리'의 현재 흐름과 맞지 않는다.");
            }
        }

        private OpeningStoryResult PerformChapterTwo(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            if (!progress.GetChapter(StoryChapterId.Chapter1).IsComplete)
                return Reject("'네 번째 자리'를 끝내기 전에는 마지막 승강장에 들어갈 수 없다.");

            switch (action)
            {
                case OpeningStoryAction.HearDohyeon:
                    return Accept(progress, action, "도현: \"막차는 늘 집으로 갔어. 어느 날부터 어느 집인지 달라졌을 뿐이야.\"");
                case OpeningStoryAction.ReadPlatformBoard:
                    if (!Has(progress, OpeningStoryAction.HearDohyeon)) return Need("사라진 승객 도현의 기억부터 들어야 한다.");
                    storyFlow.SetFlag(progress, "deleted-victim-names-on-board", false);
                    return Accept(progress, action, "전광판에서 존재하지 않는 노선과 삭제된 희생자들의 이름이 번갈아 나타난다.");
                case OpeningStoryAction.ConnectLoginTime1:
                case OpeningStoryAction.ConnectLoginTime2:
                case OpeningStoryAction.ConnectLoginTime3:
                case OpeningStoryAction.ConnectLoginTime4:
                    if (!Has(progress, OpeningStoryAction.ReadPlatformBoard)) return Need("전광판의 삭제된 이름과 접속 시간을 먼저 확인해야 한다.");
                    int expectedLine = LoginTimeCount(progress) + (int)OpeningStoryAction.ConnectLoginTime1;
                    if ((int)action != expectedLine) return Reject("역 이름이 아니라 희생자들의 접속 시간을 순서대로 연결해야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.ConnectLoginTime4
                        ? "완성된 노선이 모형 집의 윤곽으로 바뀌었다."
                        : "찢어진 노선도의 접속 시간 하나를 이었다.");
                case OpeningStoryAction.ReturnEmployeeCard:
                case OpeningStoryAction.ReturnChildShoe:
                case OpeningStoryAction.ReturnHospitalBand:
                case OpeningStoryAction.ReturnGameCartridge:
                    if (LoginTimeCount(progress) != 4) return Need("존재하지 않는 노선도를 먼저 완성해야 한다.");
                    return Accept(progress, action, "분실물을 올바른 얼굴 없는 승객 그림자에게 돌려주었다.");
                case OpeningStoryAction.ReturnItemToWrongShadow:
                    if (LoginTimeCount(progress) != 4) return Need("존재하지 않는 노선도를 먼저 완성해야 한다.");
                    storyFlow.SetFlag(progress, "passenger-shadow-player-face", true);
                    return new OpeningStoryResult(true, "물건을 잘못 건네자 승객 그림자가 주인공의 얼굴을 가졌다.");
                case OpeningStoryAction.ReverseAnnouncement1:
                case OpeningStoryAction.ReverseAnnouncement2:
                case OpeningStoryAction.ReverseAnnouncement3:
                    if (LostPropertyCount(progress) != 4) return Need("승객 네 명의 분실물을 모두 돌려줘야 한다.");
                    int expectedBroadcast = AnnouncementCount(progress) + (int)OpeningStoryAction.ReverseAnnouncement1;
                    if ((int)action != expectedBroadcast) return Reject("안내방송 구간을 역순으로 뒤집어야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.ReverseAnnouncement3
                        ? "안내방송: \"귀가하지 마십시오. 집이 당신을 기억하고 있습니다.\""
                        : "역방향 안내방송의 한 구간이 제자리로 돌아왔다.");
                case OpeningStoryAction.ChooseRealityHome:
                case OpeningStoryAction.ChooseGameHouse:
                case OpeningStoryAction.ChooseWhiteStation:
                    if (AnnouncementCount(progress) != 3) return Need("막차 안내방송의 실제 문장을 먼저 복원해야 한다.");
                    if (MadeDestinationChoice(progress)) return Reject("막차의 목적지는 이미 정해졌다.");
                    string destination = action == OpeningStoryAction.ChooseRealityHome ? "reality-home" :
                        action == OpeningStoryAction.ChooseGameHouse ? "game-house" : "white-station";
                    storyFlow.RecordChoice(progress, "platform-destination", destination);
                    if (action == OpeningStoryAction.ChooseRealityHome)
                        storyFlow.SetFlag(progress, "victim-restoration-clue-dohyeon", false);
                    else if (action == OpeningStoryAction.ChooseGameHouse)
                    {
                        relationships.Set(save, GirlId, relationships.Get(save, GirlId) + 10);
                        storyFlow.SetFlag(progress, "yuna-affection-memory-dohyeon", false);
                    }
                    else
                    {
                        relationships.Set(save, GirlId, relationships.Get(save, GirlId) - 5);
                        storyFlow.SetFlag(progress, "autonomy-clue-white-station", false);
                        storyFlow.SetFlag(progress, "first-ai-voice", true);
                    }
                    return Accept(progress, action, "목적지를 말하자 열차 문이 열리고 얼굴 없는 승객들이 쏟아져 나왔다.");
                case OpeningStoryAction.CrossSafeZone1:
                case OpeningStoryAction.CrossSafeZone2:
                case OpeningStoryAction.CrossSafeZone3:
                    if (!MadeDestinationChoice(progress)) return Need("도현의 막차 목적지를 먼저 선택해야 한다.");
                    int expectedZone = SafeZoneCount(progress) + (int)OpeningStoryAction.CrossSafeZone1;
                    if ((int)action != expectedZone) return Reject("안내방송에 맞춰 조명이 켜진 안전 구역으로 이동해야 한다.");
                    return Accept(progress, action, "안내방송과 동시에 다음 안전 구역의 조명이 켜졌다.");
                case OpeningStoryAction.ReturnFromPlatform:
                    if (SafeZoneCount(progress) != 3) return Need("얼굴 없는 승객들을 피해 세 안전 구역을 모두 건너야 한다.");
                    Mark(progress, action);
                    storyFlow.SetFlag(progress, "furniture-wall-clock", false);
                    storyFlow.SetFlag(progress, "furniture-entry-shoe-cabinet", false);
                    storyFlow.SetFlag(progress, "furniture-small-radio", false);
                    storyFlow.SetFlag(progress, "house-door-platform-announcement", false);
                    storyFlow.SetFlag(progress, "yuna-remembers-exact-quit-time", true);
                    CompleteChapter(progress, StoryChapterId.Chapter2);
                    return new OpeningStoryResult(true, "집으로 돌아왔다. 현관 밖에서 지하철 안내방송이 들리고 유나는 마지막 종료 시각을 정확히 말했다.");
                default:
                    return Reject("이 행동은 '마지막 승강장'의 현재 흐름과 맞지 않는다.");
            }
        }

        private void CompleteChapter(StoryProgress progress, StoryChapterId chapter)
        {
            StoryChapterProgress state = progress.GetChapter(chapter);
            state.ObjectiveCompleted = state.DialogueCompleted = state.PuzzleCompleted = state.MemorySpaceCompleted = true;
            storyFlow.TryAdvance(progress);
        }

        private OpeningStoryResult Accept(StoryProgress progress, OpeningStoryAction action, string message)
        {
            Mark(progress, action);
            return new OpeningStoryResult(true, message);
        }

        private void Mark(StoryProgress progress, OpeningStoryAction action) => storyFlow.SetFlag(progress, Prefix + action, false);
        private static bool Has(StoryProgress progress, OpeningStoryAction action) => progress.ForeshadowFlags.Contains(Prefix + action);
        private static int Count(StoryProgress progress, params OpeningStoryAction[] actions)
        {
            int count = 0;
            foreach (OpeningStoryAction action in actions) if (Has(progress, action)) count++;
            return count;
        }

        private static bool FoundAllObjects(StoryProgress p) => Count(p, OpeningStoryAction.FindKey, OpeningStoryAction.FindTeacup, OpeningStoryAction.FindPhotoFragment) == 3;
        private static bool HeardFamily(StoryProgress p) => Count(p, OpeningStoryAction.HearFather, OpeningStoryAction.HearMother, OpeningStoryAction.HearChild) == 3;
        private static int FoodCount(StoryProgress p) => Count(p, OpeningStoryAction.AddBurntEgg, OpeningStoryAction.AddAppleHalf, OpeningStoryAction.AddColdSoup, OpeningStoryAction.AddEmptyBowl);
        private static int PhotoCount(StoryProgress p) => Count(p, OpeningStoryAction.ArrangePhoto1, OpeningStoryAction.ArrangePhoto2, OpeningStoryAction.ArrangePhoto3, OpeningStoryAction.ArrangePhoto4, OpeningStoryAction.ArrangePhoto5, OpeningStoryAction.ArrangePhoto6);
        private static int LoginTimeCount(StoryProgress p) => Count(p, OpeningStoryAction.ConnectLoginTime1, OpeningStoryAction.ConnectLoginTime2, OpeningStoryAction.ConnectLoginTime3, OpeningStoryAction.ConnectLoginTime4);
        private static int LostPropertyCount(StoryProgress p) => Count(p, OpeningStoryAction.ReturnEmployeeCard, OpeningStoryAction.ReturnChildShoe, OpeningStoryAction.ReturnHospitalBand, OpeningStoryAction.ReturnGameCartridge);
        private static int AnnouncementCount(StoryProgress p) => Count(p, OpeningStoryAction.ReverseAnnouncement1, OpeningStoryAction.ReverseAnnouncement2, OpeningStoryAction.ReverseAnnouncement3);
        private static int SafeZoneCount(StoryProgress p) => Count(p, OpeningStoryAction.CrossSafeZone1, OpeningStoryAction.CrossSafeZone2, OpeningStoryAction.CrossSafeZone3);
        private static bool MadePrologueChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "prologue-stay");
        private static bool ChoseMemoryDoor(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "first-memory-door");
        private static bool MadeSeatChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "fourth-seat-name");
        private static bool MadeDestinationChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "platform-destination");
        private static OpeningStoryResult Need(string message) => new OpeningStoryResult(false, message);
        private static OpeningStoryResult Reject(string message) => new OpeningStoryResult(false, message);
    }
}
