#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15FacelessOfficeAcceptanceTests
    {
        [Test]
        public void FacelessOffice_EnforcesIdentityLogMirrorChoiceChaseAndReturnOrder()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterFour();
            var story = new Stage15OpeningStoryService();

            Reject(story, save, progress, "ReadOriginalDeveloperFile");
            Reject(story, save, progress, "InspectDeletionLog1");
            Reject(story, save, progress, "AlignMirrorSeat1");
            Reject(story, save, progress, "ChooseDeleteRecord");
            Reject(story, save, progress, "StartFacelessChase");
            Reject(story, save, progress, "ReturnFromFacelessOffice");

            Perform(story, save, progress,
                "HearDeveloperRecord", "EnterFacelessOffice",
                "EquipOriginalDeveloperBadge", "ReadOriginalDeveloperFile", "OpenOriginalDeveloperDoor",
                "EquipMemoryResearcherBadge", "ReadMemoryResearcherFile", "OpenMemoryResearcherDoor",
                "InspectDeletionLog1", "InspectDeletionLog2", "InspectDeletionLog3",
                "ChooseImmutableDeleteCommand",
                "AlignMirrorSeat1", "AlignMirrorSeat2", "AlignMirrorSeat3",
                "ChooseDeleteRecord", "StartFacelessChase",
                "EvadeBadgeThief1", "EvadeBadgeThief2", "EvadeBadgeThief3");

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter4));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.False);
            Perform(story, save, progress, "ReturnFromFacelessOffice");
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter5));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter5).IsComplete, Is.False);
        }

        [Test]
        public void FacelessOffice_BadgesExposeOnlyTheirFilesAndDoors()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterFour();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, "HearDeveloperRecord", "EnterFacelessOffice",
                "EquipOriginalDeveloperBadge");

            Reject(story, save, progress, "ReadMemoryResearcherFile");
            Reject(story, save, progress, "OpenMemoryResearcherDoor");
            Perform(story, save, progress, "ReadOriginalDeveloperFile", "OpenOriginalDeveloperDoor",
                "EquipMemoryResearcherBadge");
            Reject(story, save, progress, "ReadOriginalDeveloperFile");
            Reject(story, save, progress, "OpenOriginalDeveloperDoor");
            Perform(story, save, progress, "ReadMemoryResearcherFile", "OpenMemoryResearcherDoor");
        }

        [Test]
        public void FacelessOffice_WrongLogAndMirrorAttemptsDoNotAdvanceAndRemainRetryable()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForLogs(save);
            var story = new Stage15OpeningStoryService();

            Reject(story, save, progress, "ChooseMutableLogCommand");
            Assert.That(Has(progress, "ChooseMutableLogCommand"), Is.False);
            Perform(story, save, progress, "ChooseImmutableDeleteCommand");
            Reject(story, save, progress, "AlignMirrorSeat2");
            Reject(story, save, progress, "AlignWrongMirrorSeat");
            Assert.That(Has(progress, "AlignMirrorSeat2"), Is.False);
            Perform(story, save, progress, "AlignMirrorSeat1", "AlignMirrorSeat2", "AlignMirrorSeat3");
        }

        [TestCase("ChooseDeleteRecord", "delete", "developer-deletion-confirmed")]
        [TestCase("ChooseProtectRecord", "protect", "yuna-memory-protected")]
        public void FacelessOffice_VisibleRecordChoicesAreExclusiveAndPersistDistinctConsequences(
            string action, string outcome, string expectedFlag)
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForRecordChoice(save);
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, action);
            AssertChoice(progress, outcome);
            Assert.That(progress.ForeshadowFlags, Contains.Item(expectedFlag));
            Reject(story, save, progress, "ChooseDeleteRecord");
            Reject(story, save, progress, "ChooseProtectRecord");
            Reject(story, save, progress, "ChooseOriginalServerRecord");
        }

        [Test]
        public void FacelessOffice_OriginalServerChoiceRequiresAutonomyClue()
        {
            var story = new Stage15OpeningStoryService();
            var lowSave = SaveData.CreateNew();
            var low = ReadyForRecordChoice(lowSave);
            Reject(story, lowSave, low, "ChooseOriginalServerRecord");
            Assert.That(low.ImportantChoices.Exists(x => x.ChoiceId == "faceless-office-record"), Is.False);

            var highSave = SaveData.CreateNew();
            var high = ReadyForRecordChoice(highSave);
            high.ForeshadowFlags.Add("autonomy-clue-white-station");
            Perform(story, highSave, high, "ChooseOriginalServerRecord");
            AssertChoice(high, "original-server");
            Assert.That(high.ExternalEntityFlags, Contains.Item("original-server-verified"));
        }

        [Test]
        public void FacelessOffice_ChaseReturnPersistsRelationshipExternalCluesFurnitureAndChapterFiveUnlock()
        {
            var save = SaveData.CreateNew();
            new StoryRelationshipService().Set(save, "girl", 20);
            var progress = ReadyForRecordChoice(save);
            progress.ForeshadowFlags.Add("autonomy-clue-white-station");
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, "ChooseOriginalServerRecord", "StartFacelessChase",
                "EvadeBadgeThief1", "EvadeBadgeThief2", "EvadeBadgeThief3", "ReturnFromFacelessOffice");

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            save.ActiveSceneId = "FacelessOfficeReturn";
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter5));
            Assert.That(restored.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.True);
            AssertChoice(restored, "original-server");
            Assert.That(new StoryRelationshipService().Get(restoredSave, "girl"), Is.EqualTo(20));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("original-server-verified"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("composite-admin-candidate"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-study-desk"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-development-computer"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-locked-file-cabinet"));
            Assert.That(restoredSave.ActiveSceneId, Is.EqualTo("FacelessOfficeReturn"));
        }

        [Test]
        public void FacelessOffice_DoesNotRegressPrologueThroughChapterThreeSequenceContracts()
        {
            Assert.That(StoryCatalog.Get(StoryChapterId.Prologue).SummaryId, Is.EqualTo("already-running"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter1).SummaryId, Is.EqualTo("fourth-seat"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter2).SummaryId, Is.EqualTo("last-platform"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter3).SummaryId, Is.EqualTo("perfect-day"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter4).SummaryId, Is.EqualTo("faceless-office"));

            var save = SaveData.CreateNew();
            var incomplete = ReadyChapterFour();
            incomplete.GetChapter(StoryChapterId.Chapter3).MemorySpaceCompleted = false;
            Reject(new Stage15OpeningStoryService(), save, incomplete, "HearDeveloperRecord");
        }

        private static StoryProgress ReadyChapterFour()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter4 };
            Complete(progress, StoryChapterId.Prologue);
            Complete(progress, StoryChapterId.Chapter1);
            Complete(progress, StoryChapterId.Chapter2);
            Complete(progress, StoryChapterId.Chapter3);
            return progress;
        }

        private static StoryProgress ReadyForLogs(SaveData save)
        {
            StoryProgress progress = ReadyChapterFour();
            Perform(new Stage15OpeningStoryService(), save, progress,
                "HearDeveloperRecord", "EnterFacelessOffice",
                "EquipOriginalDeveloperBadge", "ReadOriginalDeveloperFile", "OpenOriginalDeveloperDoor",
                "EquipMemoryResearcherBadge", "ReadMemoryResearcherFile", "OpenMemoryResearcherDoor",
                "InspectDeletionLog1", "InspectDeletionLog2", "InspectDeletionLog3");
            return progress;
        }

        private static StoryProgress ReadyForRecordChoice(SaveData save)
        {
            StoryProgress progress = ReadyForLogs(save);
            Perform(new Stage15OpeningStoryService(), save, progress, "ChooseImmutableDeleteCommand",
                "AlignMirrorSeat1", "AlignMirrorSeat2", "AlignMirrorSeat3");
            return progress;
        }

        private static void Complete(StoryProgress progress, StoryChapterId chapter)
        {
            StoryChapterProgress state = progress.GetChapter(chapter);
            state.ObjectiveCompleted = state.DialogueCompleted = state.PuzzleCompleted = state.MemorySpaceCompleted = true;
        }

        private static OpeningStoryAction Action(string name)
        {
            Assert.That(Enum.TryParse(name, out OpeningStoryAction action), Is.True,
                $"Stage 15 chapter 4 runtime contract is missing OpeningStoryAction.{name}.");
            return action;
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            params string[] actions)
        {
            foreach (string name in actions)
                Assert.That(story.TryPerform(save, progress, Action(name)).Accepted, Is.True, name);
        }

        private static void Reject(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            string action) => Assert.That(story.TryPerform(save, progress, Action(action)).Accepted, Is.False, action);

        private static bool Has(StoryProgress progress, string action) =>
            progress.ForeshadowFlags.Contains("s15-opening:" + action);

        private static void AssertChoice(StoryProgress progress, string outcome) =>
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "faceless-office-record")?.OutcomeId,
                Is.EqualTo(outcome));
    }
}
#endif
