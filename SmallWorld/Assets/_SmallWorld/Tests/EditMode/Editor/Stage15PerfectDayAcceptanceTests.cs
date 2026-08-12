#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15PerfectDayAcceptanceTests
    {
        [Test]
        public void PerfectDay_EnforcesHomeMemoryPuzzleChoiceAndReturnOrder()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterThree();
            var story = new Stage15OpeningStoryService();

            Reject(story, save, progress, OpeningStoryAction.EnterMinaMemory);
            Reject(story, save, progress, OpeningStoryAction.FlipCafeMenu);
            Perform(story, save, progress, OpeningStoryAction.TalkWithYunaAtHome,
                OpeningStoryAction.EnterMinaMemory, OpeningStoryAction.FlipCafeMenu,
                OpeningStoryAction.OrderBitterCoffee, OpeningStoryAction.InspectGraffiti,
                OpeningStoryAction.ChooseUnknownPreference, OpeningStoryAction.SetShadowStage1,
                OpeningStoryAction.SetShadowStage2, OpeningStoryAction.SetShadowStage3,
                OpeningStoryAction.TearPerfectPhoto);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter3).IsComplete, Is.False);
            Perform(story, save, progress, OpeningStoryAction.ReturnFromPerfectDay);
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter4));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.False);
        }

        [Test]
        public void PerfectDay_WrongAnswersDoNotAdvanceAndRemainRetryable()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterThree();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, OpeningStoryAction.TalkWithYunaAtHome,
                OpeningStoryAction.EnterMinaMemory);

            Reject(story, save, progress, OpeningStoryAction.OrderDisplayedSweetDrink);
            Assert.That(Has(progress, OpeningStoryAction.OrderDisplayedSweetDrink), Is.False);
            Perform(story, save, progress, OpeningStoryAction.FlipCafeMenu,
                OpeningStoryAction.OrderBitterCoffee);
            Reject(story, save, progress, OpeningStoryAction.ChoosePresentedPreference);
            Assert.That(Has(progress, OpeningStoryAction.ChoosePresentedPreference), Is.False);
            Perform(story, save, progress, OpeningStoryAction.InspectGraffiti,
                OpeningStoryAction.ChooseUnknownPreference);
            Reject(story, save, progress, OpeningStoryAction.SetShadowStage2);
            Reject(story, save, progress, OpeningStoryAction.SetWrongShadowStage);
            Assert.That(Has(progress, OpeningStoryAction.SetShadowStage2), Is.False);
            Perform(story, save, progress, OpeningStoryAction.SetShadowStage1,
                OpeningStoryAction.SetShadowStage2, OpeningStoryAction.SetShadowStage3);
        }

        [TestCase(true, "preserve", 15, "perfect-day-loop-reinforced")]
        [TestCase(false, "tear", -10, "victim-mina-memory-restored")]
        public void PerfectDay_PhotoBranchIsSingleUseAndPersists(bool preserve, string outcome,
            int relationship, string branchFlag)
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForPhotoChoice(save);
            var story = new Stage15OpeningStoryService();
            OpeningStoryAction choice = preserve
                ? OpeningStoryAction.PreservePerfectPhoto
                : OpeningStoryAction.TearPerfectPhoto;
            Perform(story, save, progress, choice);
            Reject(story, save, progress, preserve
                ? OpeningStoryAction.TearPerfectPhoto
                : OpeningStoryAction.PreservePerfectPhoto);
            Perform(story, save, progress, OpeningStoryAction.ReturnFromPerfectDay);

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter4));
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "perfect-day-photo").OutcomeId,
                Is.EqualTo(outcome));
            Assert.That(new StoryRelationshipService().Get(restoredSave, "girl"), Is.EqualTo(relationship));
            Assert.That(restored.ForeshadowFlags, Contains.Item(branchFlag));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("external-personality-training-observer"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-bedroom-door"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-bedroom-mirror"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-bedroom-music-box"));
            if (!preserve) Assert.That(restored.ExternalEntityFlags, Contains.Item("yuna-first-anger"));
        }

        [Test]
        public void PerfectDay_CannotStartOrUnlockChapterFourOutOfSequence()
        {
            var save = SaveData.CreateNew();
            var story = new Stage15OpeningStoryService();
            var blocked = new StoryProgress { CurrentChapter = StoryChapterId.Chapter3 };
            Reject(story, save, blocked, OpeningStoryAction.TalkWithYunaAtHome);

            StoryProgress progress = ReadyForPhotoChoice(save);
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter4).IsComplete, Is.False);
            Reject(story, save, progress, OpeningStoryAction.ReturnFromPerfectDay);
        }

        private static StoryProgress ReadyChapterThree()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter3 };
            Complete(progress, StoryChapterId.Prologue);
            Complete(progress, StoryChapterId.Chapter1);
            Complete(progress, StoryChapterId.Chapter2);
            return progress;
        }

        private static StoryProgress ReadyForPhotoChoice(SaveData save)
        {
            var progress = ReadyChapterThree();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, OpeningStoryAction.TalkWithYunaAtHome,
                OpeningStoryAction.EnterMinaMemory, OpeningStoryAction.FlipCafeMenu,
                OpeningStoryAction.OrderBitterCoffee, OpeningStoryAction.InspectGraffiti,
                OpeningStoryAction.ChooseUnknownPreference, OpeningStoryAction.SetShadowStage1,
                OpeningStoryAction.SetShadowStage2, OpeningStoryAction.SetShadowStage3);
            return progress;
        }

        private static void Complete(StoryProgress progress, StoryChapterId chapter)
        {
            StoryChapterProgress state = progress.GetChapter(chapter);
            state.ObjectiveCompleted = state.DialogueCompleted = state.PuzzleCompleted = state.MemorySpaceCompleted = true;
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            params OpeningStoryAction[] actions)
        {
            foreach (OpeningStoryAction action in actions)
                Assert.That(story.TryPerform(save, progress, action).Accepted, Is.True, action.ToString());
        }

        private static void Reject(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            OpeningStoryAction action) =>
            Assert.That(story.TryPerform(save, progress, action).Accepted, Is.False, action.ToString());

        private static bool Has(StoryProgress progress, OpeningStoryAction action) =>
            progress.ForeshadowFlags.Contains("s15-opening:" + action);
    }
}
#endif
