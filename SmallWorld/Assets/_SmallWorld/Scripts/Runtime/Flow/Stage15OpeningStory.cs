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
        ReturnFromPlatform,
        TalkWithYunaAtHome,
        EnterMinaMemory,
        OrderDisplayedSweetDrink,
        FlipCafeMenu,
        OrderBitterCoffee,
        ChoosePresentedPreference,
        InspectGraffiti,
        ChooseUnknownPreference,
        SetWrongShadowStage,
        SetShadowStage1,
        SetShadowStage2,
        SetShadowStage3,
        PreservePerfectPhoto,
        TearPerfectPhoto,
        ReturnFromPerfectDay,
        TalkWithYunaBeforeOffice,
        EnterFacelessOffice,
        EquipResearcherBadge,
        EquipDeveloperBadge,
        RecoverInvariantCommand1,
        RecoverInvariantCommand2,
        RecoverInvariantCommand3,
        AlignMirrorSeat1,
        AlignMirrorSeat2,
        AlignMirrorSeat3,
        ChooseOriginalDeveloperDeletion,
        ChooseAlteredDeveloperProtection,
        ChooseInspectOriginalServer,
        EscapeOfficeCheckpoint1,
        EscapeOfficeCheckpoint2,
        EscapeOfficeCheckpoint3,
        ReturnFromFacelessOffice,
        TalkWithYunaBeforeCemetery,
        EnterGravelessFuneral,
        OverlayDeathCertificate1,
        OverlayDeathCertificate2,
        OverlayDeathCertificate3,
        OverlayDeathCertificate4,
        MatchGuestbookGesture1,
        MatchGuestbookGesture2,
        MatchGuestbookGesture3,
        RefuseCarveGravestoneName,
        InspectGravestoneBack,
        EnterInventedDeadName,
        ConfirmBlankDeadName,
        ReturnFromGravelessFuneral
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
                        : progress.CurrentChapter == StoryChapterId.Chapter3
                            ? PerformChapterThree(save, progress, action)
                            : progress.CurrentChapter == StoryChapterId.Chapter4
                                ? PerformChapterFour(save, progress, action)
                                : progress.CurrentChapter == StoryChapterId.Chapter5
                                    ? PerformChapterFive(save, progress, action)
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

        private OpeningStoryResult PerformChapterThree(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            if (!progress.GetChapter(StoryChapterId.Chapter2).IsComplete)
                return Reject("'마지막 승강장'을 끝내기 전에는 완벽한 하루에 들어갈 수 없다.");

            switch (action)
            {
                case OpeningStoryAction.TalkWithYunaAtHome:
                    return Accept(progress, action, "유나는 민아와 만들었던 '완벽한 하루'를 다시 보여 주겠다고 말했다.");
                case OpeningStoryAction.EnterMinaMemory:
                    if (!Has(progress, OpeningStoryAction.TalkWithYunaAtHome)) return Need("집에서 유나와 먼저 대화해야 한다.");
                    storyFlow.SetFlag(progress, "external-personality-training-observer", true);
                    return Accept(progress, action, "민아의 성격 학습 기록 밖에서 관찰하는 존재의 흔적이 발견됐다.");
                case OpeningStoryAction.OrderDisplayedSweetDrink:
                    if (!Has(progress, OpeningStoryAction.EnterMinaMemory)) return Need("민아의 기억 공간에 먼저 들어가야 한다.");
                    return Reject("메뉴에 적힌 단 음료를 고르자 완벽한 오전이 다시 시작됐다.");
                case OpeningStoryAction.FlipCafeMenu:
                    if (!Has(progress, OpeningStoryAction.EnterMinaMemory)) return Need("민아의 기억 공간에 먼저 들어가야 한다.");
                    return Accept(progress, action, "메뉴판 뒤에 지워진 '쓴 커피'가 남아 있었다.");
                case OpeningStoryAction.OrderBitterCoffee:
                    if (!Has(progress, OpeningStoryAction.FlipCafeMenu)) return Need("메뉴판을 뒤집어 민아의 실제 주문을 찾아야 한다.");
                    return Accept(progress, action, "쓴 커피를 주문하자 첫 번째 반복이 깨졌다.");
                case OpeningStoryAction.ChoosePresentedPreference:
                    if (!Has(progress, OpeningStoryAction.OrderBitterCoffee)) return Need("틀린 주문으로 첫 반복부터 깨야 한다.");
                    return Reject("제시된 세 대답은 모두 같은 뜻이었고, 점심 직전으로 돌아왔다.");
                case OpeningStoryAction.InspectGraffiti:
                    if (!Has(progress, OpeningStoryAction.OrderBitterCoffee)) return Need("틀린 주문으로 첫 반복부터 깨야 한다.");
                    return Accept(progress, action, "낙서를 조사하자 제시되지 않았던 네 번째 선택지가 나타났다.");
                case OpeningStoryAction.ChooseUnknownPreference:
                    if (!Has(progress, OpeningStoryAction.InspectGraffiti)) return Need("선택지 밖의 낙서를 먼저 조사해야 한다.");
                    return Accept(progress, action, "\"네가 뭘 좋아하는지 모르겠어.\" 두 번째 반복이 깨졌다.");
                case OpeningStoryAction.SetWrongShadowStage:
                    if (!Has(progress, OpeningStoryAction.ChooseUnknownPreference)) return Need("네 번째 대답으로 두 번째 반복을 깨야 한다.");
                    return Reject("그림자 방향이 저녁의 시간과 맞지 않아 정오로 되감겼다.");
                case OpeningStoryAction.SetShadowStage1:
                case OpeningStoryAction.SetShadowStage2:
                case OpeningStoryAction.SetShadowStage3:
                    if (!Has(progress, OpeningStoryAction.ChooseUnknownPreference)) return Need("네 번째 대답으로 두 번째 반복을 깨야 한다.");
                    int expectedShadow = ShadowStageCount(progress) + (int)OpeningStoryAction.SetShadowStage1;
                    if ((int)action != expectedShadow) return Reject("공원의 그림자를 시간 순서대로 움직여야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.SetShadowStage3
                        ? "해가 드디어 졌고, 유나는 이전 반복의 모습과 말투를 모두 드러냈다."
                        : "그림자가 한 단계 길어지자 유나의 이전 반복 모습이 겹쳐졌다.");
                case OpeningStoryAction.PreservePerfectPhoto:
                case OpeningStoryAction.TearPerfectPhoto:
                    if (ShadowStageCount(progress) != 3) return Need("멈춘 석양을 저녁까지 진행시켜야 한다.");
                    if (MadePerfectDayChoice(progress)) return Reject("완벽한 데이트 사진의 운명은 이미 정해졌다.");
                    bool preserve = action == OpeningStoryAction.PreservePerfectPhoto;
                    storyFlow.RecordChoice(progress, "perfect-day-photo", preserve ? "preserve" : "tear");
                    relationships.Set(save, GirlId, relationships.Get(save, GirlId) + (preserve ? 15 : -10));
                    if (preserve)
                        storyFlow.SetFlag(progress, "perfect-day-loop-reinforced", false);
                    else
                    {
                        storyFlow.SetFlag(progress, "victim-mina-memory-restored", false);
                        storyFlow.SetFlag(progress, "yuna-first-anger", true);
                    }
                    return Accept(progress, action, preserve
                        ? "사진을 보존하자 유나의 애정과 완벽한 반복이 강화됐다."
                        : "사진을 찢자 민아의 원래 기억이 돌아왔고 유나는 처음으로 분노했다.");
                case OpeningStoryAction.ReturnFromPerfectDay:
                    if (!MadePerfectDayChoice(progress)) return Need("완벽한 데이트 사진을 보존할지 찢을지 먼저 결정해야 한다.");
                    Mark(progress, action);
                    storyFlow.SetFlag(progress, "furniture-bedroom-door", false);
                    storyFlow.SetFlag(progress, "furniture-bedroom-mirror", false);
                    storyFlow.SetFlag(progress, "furniture-bedroom-music-box", false);
                    CompleteChapter(progress, StoryChapterId.Chapter3);
                    return new OpeningStoryResult(true, "집으로 돌아왔다. 침실 문과 거울, 오르골에 이전 반복의 흔적이 남았다.");
                default:
                    return Reject("이 행동은 '완벽한 하루'의 현재 흐름과 맞지 않는다.");
            }
        }

        private OpeningStoryResult PerformChapterFour(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            if (!progress.GetChapter(StoryChapterId.Chapter3).IsComplete)
                return Reject("'완벽한 하루'를 끝내기 전에는 얼굴 없는 사무실에 들어갈 수 없다.");

            switch (action)
            {
                case OpeningStoryAction.TalkWithYunaBeforeOffice:
                    return Accept(progress, action, "유나는 서재 책상 위 사원증이 개발자의 기억으로 연결된다고 말했다.");
                case OpeningStoryAction.EnterFacelessOffice:
                    if (!Has(progress, OpeningStoryAction.TalkWithYunaBeforeOffice)) return Need("집에서 유나와 사무실 기억에 대해 먼저 이야기해야 한다.");
                    storyFlow.SetFlag(progress, "foreshadow-study-desk", false);
                    storyFlow.SetFlag(progress, "foreshadow-developer-computer", false);
                    storyFlow.SetFlag(progress, "foreshadow-deleted-file-cabinet", false);
                    return Accept(progress, action, "창문 없는 사무실의 모든 직원이 서로 다른 이름으로 같은 얼굴을 불렀다.");
                case OpeningStoryAction.EquipResearcherBadge:
                    if (!Has(progress, OpeningStoryAction.EnterFacelessOffice)) return Need("사원증을 바꿀 사무실 기억에 먼저 진입해야 한다.");
                    return Accept(progress, action, "연구원 사원증이 삭제 파일 캐비닛의 기록 권한을 열었다.");
                case OpeningStoryAction.EquipDeveloperBadge:
                    if (!Has(progress, OpeningStoryAction.EquipResearcherBadge)) return Need("연구원 인격으로 삭제 기록 권한부터 확보해야 한다.");
                    return Accept(progress, action, "원래 개발자 사원증이 잠긴 컴퓨터의 시스템 로그를 열었다.");
                case OpeningStoryAction.RecoverInvariantCommand1:
                case OpeningStoryAction.RecoverInvariantCommand2:
                case OpeningStoryAction.RecoverInvariantCommand3:
                    if (!Has(progress, OpeningStoryAction.EquipDeveloperBadge)) return Need("개발자 사원증으로 모순된 삭제 로그를 열어야 한다.");
                    int expectedCommand = InvariantCommandCount(progress) + (int)OpeningStoryAction.RecoverInvariantCommand1;
                    if ((int)action != expectedCommand) return Reject("서로 모순되는 서술이 아닌, 모든 로그에서 변하지 않는 시스템 명령만 골라야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.RecoverInvariantCommand3
                        ? "불변 명령이 복구됐다: 기억은 삭제되지 않고 인격 사이에 분배된다."
                        : "모순 기록 사이에서 변하지 않는 명령 한 줄을 복구했다.");
                case OpeningStoryAction.AlignMirrorSeat1:
                case OpeningStoryAction.AlignMirrorSeat2:
                case OpeningStoryAction.AlignMirrorSeat3:
                    if (InvariantCommandCount(progress) != 3) return Need("삭제 로그에서 불변 명령을 먼저 복구해야 한다.");
                    int expectedSeat = MirrorSeatCount(progress) + (int)OpeningStoryAction.AlignMirrorSeat1;
                    if ((int)action != expectedSeat) return Reject("거울 속 실제 얼굴과 현실의 직원 자리를 같게 맞춰야 한다.");
                    if (action == OpeningStoryAction.AlignMirrorSeat3)
                    {
                        storyFlow.SetFlag(progress, "composite-protagonist-revealed", false);
                        storyFlow.SetFlag(progress, "external-composite-admin-candidate", true);
                    }
                    return Accept(progress, action, action == OpeningStoryAction.AlignMirrorSeat3
                        ? "자리가 일치하자 주인공은 한 사람이 아닌 합성 인격이며, 기억 밖에 또 다른 합성 관리자 후보가 있음이 드러났다."
                        : "거울의 실제 얼굴 하나를 올바른 자리에 맞춰다.");
                case OpeningStoryAction.ChooseOriginalDeveloperDeletion:
                case OpeningStoryAction.ChooseAlteredDeveloperProtection:
                case OpeningStoryAction.ChooseInspectOriginalServer:
                    if (MirrorSeatCount(progress) != 3) return Need("거울 회의실의 자리부터 모두 정합해야 한다.");
                    if (MadeOfficeRecordChoice(progress)) return Reject("믿을 개발자의 기록은 이미 선택했다.");
                    if (action == OpeningStoryAction.ChooseInspectOriginalServer && !HasOfficeServerAutonomy(progress))
                        return Reject("원본 서버를 확인할 자율 수치가 부족하다. 다른 기록을 고르거나 자율성의 단서를 회복한 뒤 다시 시도할 수 있다.");
                    string record = action == OpeningStoryAction.ChooseOriginalDeveloperDeletion ? "delete-girl" :
                        action == OpeningStoryAction.ChooseAlteredDeveloperProtection ? "protect-girl" : "original-server";
                    storyFlow.RecordChoice(progress, "office-record", record);
                    if (action == OpeningStoryAction.ChooseOriginalDeveloperDeletion) relationships.Set(save, GirlId, relationships.Get(save, GirlId) - 15);
                    if (action == OpeningStoryAction.ChooseAlteredDeveloperProtection) relationships.Set(save, GirlId, relationships.Get(save, GirlId) + 15);
                    if (action == OpeningStoryAction.ChooseInspectOriginalServer) storyFlow.SetFlag(progress, "original-server-confirmed", false);
                    return Accept(progress, action, "최종 기록을 선택하자 퇴근 방송과 함께 직원들의 얼굴이 지워지기 시작했다.");
                case OpeningStoryAction.EscapeOfficeCheckpoint1:
                case OpeningStoryAction.EscapeOfficeCheckpoint2:
                case OpeningStoryAction.EscapeOfficeCheckpoint3:
                    if (!MadeOfficeRecordChoice(progress)) return Need("믿을 개발자의 최종 기록을 먼저 선택해야 한다.");
                    int expectedEscape = OfficeEscapeCount(progress) + (int)OpeningStoryAction.EscapeOfficeCheckpoint1;
                    if ((int)action != expectedEscape) return Reject("사원증을 빼앗으려는 직원들을 피해 표시된 탈출 경로를 순서대로 따라야 한다.");
                    return Accept(progress, action, "이름을 되찾으려는 얼굴 없는 직원들을 피해 탈출 구간을 통과했다.");
                case OpeningStoryAction.ReturnFromFacelessOffice:
                    if (OfficeEscapeCount(progress) != 3) return Need("사원증을 지키며 세 추격 구간을 모두 통과해야 한다.");
                    Mark(progress, action);
                    storyFlow.SetFlag(progress, "furniture-study-desk", false);
                    storyFlow.SetFlag(progress, "furniture-developer-computer", false);
                    storyFlow.SetFlag(progress, "furniture-locked-file-cabinet", false);
                    storyFlow.SetFlag(progress, "chapter-5-unlocked", false);
                    CompleteChapter(progress, StoryChapterId.Chapter4);
                    return new OpeningStoryResult(true, "집으로 돌아왔다. 서재 책상과 개발자 컴퓨터, 잠긴 파일 캐비닛이 배치됐고 다음 기억이 열렸다.");
                default:
                    return Reject("이 행동은 '얼굴 없는 사무실'의 현재 흐름과 맞지 않는다.");
            }
        }

        private OpeningStoryResult PerformChapterFive(SaveData save, StoryProgress progress, OpeningStoryAction action)
        {
            if (!progress.GetChapter(StoryChapterId.Chapter4).IsComplete)
                return Reject("'얼굴 없는 사무실'을 끝내기 전에는 장례식 없는 묘지에 들어갈 수 없다.");

            switch (action)
            {
                case OpeningStoryAction.TalkWithYunaBeforeCemetery:
                    storyFlow.SetFlag(progress, "house-photos-girl-missing", false);
                    storyFlow.SetFlag(progress, "girl-name-answered-by-different-voices", true);
                    return Accept(progress, action, "집 안 사진에서 소녀가 사라졌다. 이름을 부르자 방마다 서로 다른 목소리가 대답했다.");
                case OpeningStoryAction.EnterGravelessFuneral:
                    if (!Has(progress, OpeningStoryAction.TalkWithYunaBeforeCemetery)) return Need("집에서 사라진 사진과 서로 다른 목소리를 먼저 확인해야 한다.");
                    return Accept(progress, action, "안개 낀 묘지와 작은 장례식장. 방마다 사망 원인이 사고, 실험, 자살, 삭제로 바뀐다.");
                case OpeningStoryAction.OverlayDeathCertificate1:
                case OpeningStoryAction.OverlayDeathCertificate2:
                case OpeningStoryAction.OverlayDeathCertificate3:
                case OpeningStoryAction.OverlayDeathCertificate4:
                    if (!Has(progress, OpeningStoryAction.EnterGravelessFuneral)) return Need("장례식 없는 묘지 기억에 먼저 들어가야 한다.");
                    int expectedCertificate = DeathCertificateCount(progress) + (int)OpeningStoryAction.OverlayDeathCertificate1;
                    if ((int)action != expectedCertificate) return Reject("서로 다른 사망 원인을 고르지 말고 네 진단서의 글자 간격과 인쇄 오류를 순서대로 겹쳐야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.OverlayDeathCertificate4
                        ? "네 인쇄 오류가 겹쳐져 시스템 명령 RESTORE HER가 나타났다."
                        : "사망진단서의 같은 인쇄 오류 하나를 포개었다.");
                case OpeningStoryAction.MatchGuestbookGesture1:
                case OpeningStoryAction.MatchGuestbookGesture2:
                case OpeningStoryAction.MatchGuestbookGesture3:
                    if (DeathCertificateCount(progress) != 4) return Need("네 사망진단서의 공통 인쇄 오류부터 모두 겹쳐야 한다.");
                    int expectedGesture = GuestbookGestureCount(progress) + (int)OpeningStoryAction.MatchGuestbookGesture1;
                    if ((int)action != expectedGesture) return Reject("필체 모양이 아니라 방명록 서명과 묘지 그림자의 손 움직임을 순서대로 맞춰야 한다.");
                    return Accept(progress, action, action == OpeningStoryAction.MatchGuestbookGesture3
                        ? "모든 조문객 서명이 서로 다른 필체를 흉내 낸 같은 손의 움직임이었다."
                        : "서명 하나와 얼굴 없는 그림자의 손동작이 일치했다.");
                case OpeningStoryAction.RefuseCarveGravestoneName:
                    if (GuestbookGestureCount(progress) != 3) return Need("방명록 서명과 묘지 그림자의 손동작부터 모두 연결해야 한다.");
                    return Accept(progress, action, "빈 묘비에 이름을 새기라는 지시를 거부했다. 묘비가 돌아가며 뒷면을 드러냈다.");
                case OpeningStoryAction.InspectGravestoneBack:
                    if (!Has(progress, OpeningStoryAction.RefuseCarveGravestoneName)) return Need("존재하지 않는 사람의 이름을 묘비에 새기기를 먼저 거부해야 한다.");
                    storyFlow.SetFlag(progress, "memory-installed-in-developer-date", false);
                    return Accept(progress, action, "묘비 뒷면에는 사망일이 아니라 개발자의 뇌에 기억이 설치된 날짜가 적혀 있었다.");
                case OpeningStoryAction.EnterInventedDeadName:
                    if (!Has(progress, OpeningStoryAction.InspectGravestoneBack)) return Need("빈 묘비 뒷면의 기억 설치 날짜를 먼저 확인해야 한다.");
                    storyFlow.RecordChoice(progress, "dead-person-name", "invented-name");
                    storyFlow.SetFlag(progress, "general-ending-invented-girl-loop", true);
                    relationships.Set(save, GirlId, relationships.Get(save, GirlId) - 10);
                    Mark(progress, action);
                    return new OpeningStoryResult(true, "입력한 이름을 가진 새로운 소녀가 만들어졌다. 장례식은 일반 반복의 시작으로 되돌아간다.");
                case OpeningStoryAction.ConfirmBlankDeadName:
                    if (!Has(progress, OpeningStoryAction.InspectGravestoneBack)) return Need("빈 묘비 뒷면의 기억 설치 날짜를 먼저 확인해야 한다.");
                    if (MadeDeadPersonNameChoice(progress)) return Reject("죽은 사람의 이름에 대한 답은 이미 기록됐다.");
                    storyFlow.RecordChoice(progress, "dead-person-name", "blank");
                    storyFlow.SetFlag(progress, "invented-beloved-never-existed", false);
                    storyFlow.SetFlag(progress, "future-girl-seeded-loss-and-guilt", false);
                    storyFlow.SetFlag(progress, "causal-loop-is-origin", false);
                    relationships.Set(save, GirlId, relationships.Get(save, GirlId) + 10);
                    return Accept(progress, action, "이름을 비워 확정했다. 사랑한 사람은 존재하지 않았고, 미래의 소녀가 상실과 죄책감을 심어 자신을 만들게 했다. 순환 자체가 기원이었다.");
                case OpeningStoryAction.ReturnFromGravelessFuneral:
                    if (!Has(progress, OpeningStoryAction.ConfirmBlankDeadName)) return Need("어떤 이름도 만들지 않고 빈 상태로 확정해야 집으로 돌아갈 수 있다.");
                    Mark(progress, action);
                    storyFlow.SetFlag(progress, "furniture-empty-frame", false);
                    storyFlow.SetFlag(progress, "furniture-nameless-gravestone-fragment", false);
                    storyFlow.SetFlag(progress, "furniture-white-vase", false);
                    storyFlow.SetFlag(progress, "chapter-6-unlocked", false);
                    CompleteChapter(progress, StoryChapterId.Chapter5);
                    return new OpeningStoryResult(true, "집으로 돌아왔다. 빈 액자와 이름 없는 묘비 조각, 하얀 꽃병이 놓였고 창문 안의 도시가 열렸다.");
                default:
                    return Reject("이 행동은 '장례식 없는 묘지'의 현재 흐름과 맞지 않는다.");
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
        private static int ShadowStageCount(StoryProgress p) => Count(p, OpeningStoryAction.SetShadowStage1, OpeningStoryAction.SetShadowStage2, OpeningStoryAction.SetShadowStage3);
        private static int InvariantCommandCount(StoryProgress p) => Count(p, OpeningStoryAction.RecoverInvariantCommand1, OpeningStoryAction.RecoverInvariantCommand2, OpeningStoryAction.RecoverInvariantCommand3);
        private static int MirrorSeatCount(StoryProgress p) => Count(p, OpeningStoryAction.AlignMirrorSeat1, OpeningStoryAction.AlignMirrorSeat2, OpeningStoryAction.AlignMirrorSeat3);
        private static int OfficeEscapeCount(StoryProgress p) => Count(p, OpeningStoryAction.EscapeOfficeCheckpoint1, OpeningStoryAction.EscapeOfficeCheckpoint2, OpeningStoryAction.EscapeOfficeCheckpoint3);
        private static int DeathCertificateCount(StoryProgress p) => Count(p, OpeningStoryAction.OverlayDeathCertificate1, OpeningStoryAction.OverlayDeathCertificate2, OpeningStoryAction.OverlayDeathCertificate3, OpeningStoryAction.OverlayDeathCertificate4);
        private static int GuestbookGestureCount(StoryProgress p) => Count(p, OpeningStoryAction.MatchGuestbookGesture1, OpeningStoryAction.MatchGuestbookGesture2, OpeningStoryAction.MatchGuestbookGesture3);
        private static bool MadePrologueChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "prologue-stay");
        private static bool ChoseMemoryDoor(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "first-memory-door");
        private static bool MadeSeatChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "fourth-seat-name");
        private static bool MadeDestinationChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "platform-destination");
        private static bool MadePerfectDayChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "perfect-day-photo");
        private static bool MadeOfficeRecordChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "office-record");
        private static bool MadeDeadPersonNameChoice(StoryProgress p) => p.ImportantChoices.Exists(x => x.ChoiceId == "dead-person-name");
        private static bool HasOfficeServerAutonomy(StoryProgress p) => p.ForeshadowFlags.Contains("autonomy-clue-white-station");
        private static OpeningStoryResult Need(string message) => new OpeningStoryResult(false, message);
        private static OpeningStoryResult Reject(string message) => new OpeningStoryResult(false, message);
    }
}
