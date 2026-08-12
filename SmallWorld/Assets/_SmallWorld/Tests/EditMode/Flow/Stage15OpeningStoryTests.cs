#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Story;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15OpeningStoryTests
    {
        [TearDown]
        public void ClearRuntimeHandoffs()
        {
            Stage10SaveRuntime.ConsumePendingLoad();
            Stage10SaveRuntime.ConsumeSceneSession();
        }

        [Test]
        public void StoryRouteEntrySession_PreservesUnsavedNewGameDespiteNewerOtherSlot()
        {
            var service = new RouteSessionSaveService();
            SaveData newerOtherSlot = SaveData.CreateNew();
            newerOtherSlot.SavedAtUtcTicks = 200;
            service.Latest = SaveReadResult.Success(newerOtherSlot, "manual-2");
            Stage10SaveRuntime.Configure(service);

            SaveData unsavedNewGame = SaveData.CreateNew();
            unsavedNewGame.SavedAtUtcTicks = 0;
            Stage10SaveRuntime.QueueSceneSession(unsavedNewGame);

            GameObject root = new GameObject("StoryRouteSessionTest");
            try
            {
                StoryRouteProgressAdapter adapter = root.AddComponent<StoryRouteProgressAdapter>();

                Assert.That(adapter.CurrentSave, Is.SameAs(unsavedNewGame));
                Assert.That(adapter.CurrentSave, Is.Not.SameAs(newerOtherSlot));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StoryRouteEntrySession_DoesNotConsumeContinuePendingLoad_AndAutoSavesSameSession()
        {
            var service = new RouteSessionSaveService();
            Stage10SaveRuntime.Configure(service);
            SaveData continuePending = SaveData.CreateNew();
            SaveData selectedSession = SaveData.CreateNew();
            Stage10SaveRuntime.QueueLoad(continuePending);
            Stage10SaveRuntime.QueueSceneSession(selectedSession);

            GameObject root = new GameObject("StoryRouteSessionAutoSaveTest");
            try
            {
                StoryRouteProgressAdapter adapter = root.AddComponent<StoryRouteProgressAdapter>();
                adapter.ReportNodeReached("prologue");

                Assert.That(service.AutoSaved, Is.SameAs(selectedSession));
                Assert.That(service.AutoSaved.ActiveSceneId, Is.EqualTo("04_StoryRoute"));
                Assert.That(Stage10SaveRuntime.PendingLoad, Is.SameAs(continuePending));
                Assert.That(Stage10SaveRuntime.PendingSceneSession, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void StoryRouteEntrySession_PreservesOlderSelectedSaveInsteadOfLatestSlot()
        {
            var service = new RouteSessionSaveService();
            SaveData latestOtherSlot = SaveData.CreateNew();
            latestOtherSlot.SavedAtUtcTicks = 500;
            service.Latest = SaveReadResult.Success(latestOtherSlot, "manual-1");
            Stage10SaveRuntime.Configure(service);
            SaveData olderSelectedSave = SaveData.CreateNew();
            olderSelectedSave.SavedAtUtcTicks = 100;
            Stage10SaveRuntime.QueueSceneSession(olderSelectedSave);

            GameObject root = new GameObject("StoryRouteSelectedSaveTest");
            try
            {
                StoryRouteProgressAdapter adapter = root.AddComponent<StoryRouteProgressAdapter>();

                Assert.That(adapter.CurrentSave, Is.SameAs(olderSelectedSave));
                Assert.That(adapter.CurrentSave.SaveId, Is.Not.EqualTo(latestOtherSlot.SaveId));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Prologue_RequiresGameplaySequenceAndUnlocksOnlyChapterOne()
        {
            var save = SaveData.CreateNew();
            var progress = new StoryProgress();
            var story = new Stage15OpeningStoryService();

            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.PlaceSofa).Accepted, Is.False);
            Perform(story, save, progress, OpeningStoryAction.MeetYuna, OpeningStoryAction.PlaceSofa,
                OpeningStoryAction.FindKey, OpeningStoryAction.FindTeacup, OpeningStoryAction.FindPhotoFragment,
                OpeningStoryAction.ChooseUncertain, OpeningStoryAction.ReadScheduledMail,
                OpeningStoryAction.QuestionMemoryDoor);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter1));
            Assert.That(progress.GetChapter(StoryChapterId.Prologue).IsComplete, Is.True);
            Assert.That(progress.GetChapter(StoryChapterId.Chapter1).IsComplete, Is.False);
            Assert.That(progress.ForeshadowFlags, Contains.Item("repeat-109"));
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "first-memory-door").OutcomeId, Is.EqualTo("question"));
        }

        [Test]
        public void FourthSeat_RejectsSkippedPuzzleAndCompletesEscapeInOrder()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterOne();
            var story = new Stage15OpeningStoryService();

            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.SetClock1942).Accepted, Is.False);
            Perform(story, save, progress, OpeningStoryAction.HearFather, OpeningStoryAction.HearMother,
                OpeningStoryAction.HearChild, OpeningStoryAction.SetClock1942,
                OpeningStoryAction.AddBurntEgg, OpeningStoryAction.AddAppleHalf, OpeningStoryAction.AddEmptyBowl,
                OpeningStoryAction.ArrangePhoto1, OpeningStoryAction.ArrangePhoto2, OpeningStoryAction.ArrangePhoto3,
                OpeningStoryAction.ArrangePhoto4, OpeningStoryAction.ArrangePhoto5, OpeningStoryAction.ArrangePhoto6,
                OpeningStoryAction.OpenSilentDoor, OpeningStoryAction.SeatSeoyun,
                OpeningStoryAction.RotateKitchenDoor, OpeningStoryAction.MoveSofa, OpeningStoryAction.TurnFrame,
                OpeningStoryAction.PullFrontDoor, OpeningStoryAction.ReturnHome);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter2));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter1).IsComplete, Is.True);
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "fourth-seat-name").OutcomeId, Is.EqualTo("seoyun"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("dollhouse-basement-key"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("victim-seoyun-restored"));
        }

        [Test]
        public void FourthSeat_FoodAndNameChoicesPreserveBranchConsequences()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterOne();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, OpeningStoryAction.HearFather, OpeningStoryAction.HearMother,
                OpeningStoryAction.HearChild, OpeningStoryAction.SetClock1942,
                OpeningStoryAction.AddBurntEgg, OpeningStoryAction.AddAppleHalf, OpeningStoryAction.AddColdSoup);

            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.AddEmptyBowl).Accepted, Is.False);
            Perform(story, save, progress, OpeningStoryAction.ArrangePhoto1, OpeningStoryAction.ArrangePhoto2,
                OpeningStoryAction.ArrangePhoto3, OpeningStoryAction.ArrangePhoto4,
                OpeningStoryAction.ArrangePhoto5, OpeningStoryAction.ArrangePhoto6,
                OpeningStoryAction.OpenSilentDoor, OpeningStoryAction.SeatYuna);

            Assert.That(progress.ExternalEntityFlags, Contains.Item("yuna-seoyun-assimilation"));
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "fourth-seat-name").OutcomeId, Is.EqualTo("yuna"));
        }

        [Test]
        public void LastPlatform_RequiresCompletedChapterOneAndUnlocksOnlyChapterThree()
        {
            var save = SaveData.CreateNew();
            var story = new Stage15OpeningStoryService();
            var blocked = new StoryProgress { CurrentChapter = StoryChapterId.Chapter2 };
            Assert.That(story.TryPerform(save, blocked, OpeningStoryAction.HearDohyeon).Accepted, Is.False);

            StoryProgress progress = ReadyChapterTwo();
            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.ConnectLoginTime1).Accepted, Is.False);
            Perform(story, save, progress,
                OpeningStoryAction.HearDohyeon, OpeningStoryAction.ReadPlatformBoard,
                OpeningStoryAction.ConnectLoginTime1, OpeningStoryAction.ConnectLoginTime2,
                OpeningStoryAction.ConnectLoginTime3, OpeningStoryAction.ConnectLoginTime4,
                OpeningStoryAction.ReturnItemToWrongShadow,
                OpeningStoryAction.ReturnEmployeeCard, OpeningStoryAction.ReturnChildShoe,
                OpeningStoryAction.ReturnHospitalBand, OpeningStoryAction.ReturnGameCartridge,
                OpeningStoryAction.ReverseAnnouncement1, OpeningStoryAction.ReverseAnnouncement2,
                OpeningStoryAction.ReverseAnnouncement3, OpeningStoryAction.ChooseRealityHome,
                OpeningStoryAction.CrossSafeZone1, OpeningStoryAction.CrossSafeZone2,
                OpeningStoryAction.CrossSafeZone3, OpeningStoryAction.ReturnFromPlatform);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.True);
            Assert.That(progress.GetChapter(StoryChapterId.Chapter3).IsComplete, Is.False);
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "platform-destination").OutcomeId,
                Is.EqualTo("reality-home"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("victim-restoration-clue-dohyeon"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-wall-clock"));
            Assert.That(progress.ExternalEntityFlags, Contains.Item("passenger-shadow-player-face"));
            Assert.That(progress.ExternalEntityFlags, Contains.Item("yuna-remembers-exact-quit-time"));
        }

        [Test]
        public void LastPlatform_DestinationBranchesPersistChoiceRelationshipAndForeshadowing()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterTwo();
            var story = new Stage15OpeningStoryService();
            PerformUntilDestination(story, save, progress);
            Perform(story, save, progress, OpeningStoryAction.ChooseWhiteStation);

            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "platform-destination").OutcomeId,
                Is.EqualTo("white-station"));
            Assert.That(new StoryRelationshipService().Get(save, "girl"), Is.EqualTo(-5));
            Assert.That(progress.ForeshadowFlags, Contains.Item("autonomy-clue-white-station"));
            Assert.That(progress.ExternalEntityFlags, Contains.Item("first-ai-voice"));

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "platform-destination").OutcomeId,
                Is.EqualTo("white-station"));
            Assert.That(new StoryRelationshipService().Get(restoredSave, "girl"), Is.EqualTo(-5));
            Assert.That(restored.ForeshadowFlags, Contains.Item("autonomy-clue-white-station"));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("first-ai-voice"));
        }

        [Test]
        public void FacelessOffice_RequiresOrderedCoreAndUnlocksChapterFiveAfterHomeReturn()
        {
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyChapterFour();
            var story = new Stage15OpeningStoryService();

            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.EnterFacelessOffice).Accepted, Is.False);
            Perform(story, save, progress,
                OpeningStoryAction.TalkWithYunaBeforeOffice, OpeningStoryAction.EnterFacelessOffice,
                OpeningStoryAction.EquipResearcherBadge, OpeningStoryAction.EquipDeveloperBadge,
                OpeningStoryAction.RecoverInvariantCommand1, OpeningStoryAction.RecoverInvariantCommand2,
                OpeningStoryAction.RecoverInvariantCommand3,
                OpeningStoryAction.AlignMirrorSeat1, OpeningStoryAction.AlignMirrorSeat2,
                OpeningStoryAction.AlignMirrorSeat3,
                OpeningStoryAction.ChooseAlteredDeveloperProtection,
                OpeningStoryAction.EscapeOfficeCheckpoint1, OpeningStoryAction.EscapeOfficeCheckpoint2,
                OpeningStoryAction.EscapeOfficeCheckpoint3, OpeningStoryAction.ReturnFromFacelessOffice);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter5));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.True);
            Assert.That(progress.GetChapter(StoryChapterId.Chapter5).IsComplete, Is.False);
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "office-record").OutcomeId,
                Is.EqualTo("protect-girl"));
            Assert.That(new StoryRelationshipService().Get(save, "girl"), Is.EqualTo(15));
            Assert.That(progress.ExternalEntityFlags, Contains.Item("external-composite-admin-candidate"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("composite-protagonist-revealed"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-study-desk"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-developer-computer"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-locked-file-cabinet"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("chapter-5-unlocked"));
        }

        [Test]
        public void FacelessOffice_OriginalServerRejectsWithoutAutonomyAndRemainsRetryable()
        {
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyChapterFourAtRecordChoice();
            var story = new Stage15OpeningStoryService();

            OpeningStoryResult rejected = story.TryPerform(save, progress, OpeningStoryAction.ChooseInspectOriginalServer);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(progress.ImportantChoices.Exists(x => x.ChoiceId == "office-record"), Is.False);

            new StoryFlowService().SetFlag(progress, "autonomy-clue-white-station", false);
            Assert.That(story.TryPerform(save, progress, OpeningStoryAction.ChooseInspectOriginalServer).Accepted, Is.True);
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "office-record").OutcomeId,
                Is.EqualTo("original-server"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("original-server-confirmed"));
        }

        [Test]
        public void FacelessOffice_SaveRoundTripPreservesChoiceExternalClueFurnitureAndChapterUnlock()
        {
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyChapterFourAtRecordChoice();
            var story = new Stage15OpeningStoryService();
            new StoryFlowService().SetFlag(progress, "autonomy-clue-white-station", false);
            Perform(story, save, progress, OpeningStoryAction.ChooseInspectOriginalServer,
                OpeningStoryAction.EscapeOfficeCheckpoint1, OpeningStoryAction.EscapeOfficeCheckpoint2,
                OpeningStoryAction.EscapeOfficeCheckpoint3, OpeningStoryAction.ReturnFromFacelessOffice);

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter5));
            Assert.That(restored.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.True);
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "office-record").OutcomeId,
                Is.EqualTo("original-server"));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("external-composite-admin-candidate"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-study-desk"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-developer-computer"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-locked-file-cabinet"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("chapter-5-unlocked"));
        }

        private static StoryProgress ReadyChapterOne()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter1 };
            StoryChapterProgress prologue = progress.GetChapter(StoryChapterId.Prologue);
            prologue.ObjectiveCompleted = prologue.DialogueCompleted = prologue.PuzzleCompleted = prologue.MemorySpaceCompleted = true;
            return progress;
        }

        private static StoryProgress ReadyChapterTwo()
        {
            var progress = ReadyChapterOne();
            StoryChapterProgress chapterOne = progress.GetChapter(StoryChapterId.Chapter1);
            chapterOne.ObjectiveCompleted = chapterOne.DialogueCompleted = chapterOne.PuzzleCompleted = chapterOne.MemorySpaceCompleted = true;
            progress.CurrentChapter = StoryChapterId.Chapter2;
            return progress;
        }

        private static StoryProgress ReadyChapterFour()
        {
            var progress = ReadyChapterTwo();
            for (int i = (int)StoryChapterId.Chapter2; i <= (int)StoryChapterId.Chapter3; i++)
            {
                StoryChapterProgress chapter = progress.GetChapter((StoryChapterId)i);
                chapter.ObjectiveCompleted = chapter.DialogueCompleted = chapter.PuzzleCompleted = chapter.MemorySpaceCompleted = true;
            }
            progress.CurrentChapter = StoryChapterId.Chapter4;
            return progress;
        }

        private static StoryProgress ReadyChapterFourAtRecordChoice()
        {
            StoryProgress progress = ReadyChapterFour();
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            Perform(story, save, progress,
                OpeningStoryAction.TalkWithYunaBeforeOffice, OpeningStoryAction.EnterFacelessOffice,
                OpeningStoryAction.EquipResearcherBadge, OpeningStoryAction.EquipDeveloperBadge,
                OpeningStoryAction.RecoverInvariantCommand1, OpeningStoryAction.RecoverInvariantCommand2,
                OpeningStoryAction.RecoverInvariantCommand3,
                OpeningStoryAction.AlignMirrorSeat1, OpeningStoryAction.AlignMirrorSeat2,
                OpeningStoryAction.AlignMirrorSeat3);
            return progress;
        }

        private static void PerformUntilDestination(Stage15OpeningStoryService story, SaveData save, StoryProgress progress)
        {
            Perform(story, save, progress,
                OpeningStoryAction.HearDohyeon, OpeningStoryAction.ReadPlatformBoard,
                OpeningStoryAction.ConnectLoginTime1, OpeningStoryAction.ConnectLoginTime2,
                OpeningStoryAction.ConnectLoginTime3, OpeningStoryAction.ConnectLoginTime4,
                OpeningStoryAction.ReturnEmployeeCard, OpeningStoryAction.ReturnChildShoe,
                OpeningStoryAction.ReturnHospitalBand, OpeningStoryAction.ReturnGameCartridge,
                OpeningStoryAction.ReverseAnnouncement1, OpeningStoryAction.ReverseAnnouncement2,
                OpeningStoryAction.ReverseAnnouncement3);
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            params OpeningStoryAction[] actions)
        {
            foreach (OpeningStoryAction action in actions)
                Assert.That(story.TryPerform(save, progress, action).Accepted, Is.True, action.ToString());
        }

        private sealed class RouteSessionSaveService : IGameSaveService
        {
            public SaveReadResult Latest = SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveData AutoSaved;

            public bool AutoSave(SaveData data) { AutoSaved = data; return true; }
            public bool SaveManual(int slotIndex, SaveData data) => true;
            public SaveReadResult LoadLatestAutoSave() => Latest;
            public SaveReadResult LoadManual(int slotIndex) => SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveData StartNewGame() => SaveData.CreateNew();
        }
    }
}
#endif
