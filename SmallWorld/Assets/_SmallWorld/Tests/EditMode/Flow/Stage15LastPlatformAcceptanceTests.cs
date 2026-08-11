#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15LastPlatformAcceptanceTests
    {
        [Test]
        public void LastPlatform_EnforcesPuzzleAndEscapeOrder()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterTwo();
            var story = new Stage15OpeningStoryService();

            Reject(story, save, progress, "ReturnEmployeeId");
            Reject(story, save, progress, "ReverseAnnouncement1");
            Reject(story, save, progress, "ChooseWhiteStation");
            Reject(story, save, progress, "EscapeLastPlatform");

            Perform(story, save, progress,
                "ConnectRoute1", "ConnectRoute2", "ConnectRoute3", "ConnectRoute4",
                "ReturnEmployeeId", "ReturnChildShoe", "ReturnHospitalWristband", "ReturnGameCartridge",
                "ReverseAnnouncement1", "ReverseAnnouncement2", "ReverseAnnouncement3",
                "ChooseWhiteStation", "EscapeLastPlatform", "ReturnFromLastPlatform");

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.True);
        }

        [Test]
        public void LastPlatform_WrongAnswersDoNotAdvanceAndRemainRetryable()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterTwo();
            var story = new Stage15OpeningStoryService();

            Reject(story, save, progress, "ConnectWrongRoute");
            Perform(story, save, progress, "ConnectRoute1");
            Assert.That(Has(progress, "ConnectWrongRoute"), Is.False);

            Perform(story, save, progress, "ConnectRoute2", "ConnectRoute3", "ConnectRoute4");
            Reject(story, save, progress, "ReturnWrongLostItem");
            Perform(story, save, progress, "ReturnEmployeeId");
            Assert.That(Has(progress, "ReturnWrongLostItem"), Is.False);

            Perform(story, save, progress, "ReturnChildShoe", "ReturnHospitalWristband", "ReturnGameCartridge");
            Reject(story, save, progress, "ReverseWrongAnnouncement");
            Perform(story, save, progress, "ReverseAnnouncement1");
            Assert.That(Has(progress, "ReverseWrongAnnouncement"), Is.False);
        }

        [TestCase("ChooseDohyeonHome", "dohyeon-home", "victim-dohyeon-restoration-clue")]
        [TestCase("ChooseGameHome", "game-home", "yuna-affection-memory")]
        [TestCase("ChooseWhiteStation", "white-station", "first-ai-voice")]
        public void LastPlatform_DestinationBranchesPersistDistinctConsequences(
            string action, string outcome, string expectedFlag)
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, action);

            StoryChoiceState choice = progress.ImportantChoices.Find(x => x.ChoiceId == "chapter-2-destination");
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.OutcomeId, Is.EqualTo(outcome));
            Assert.That(progress.ForeshadowFlags, Contains.Item(expectedFlag));
            Reject(story, save, progress, "ChooseDohyeonHome");
            Reject(story, save, progress, "ChooseGameHome");
            Reject(story, save, progress, "ChooseWhiteStation");
        }

        [Test]
        public void LastPlatform_SaveRoundTripPreservesChoiceAndCompletion()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, "ChooseWhiteStation", "EscapeLastPlatform", "ReturnFromLastPlatform");

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            StoryProgress restored = store.Load(save);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(restored.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.True);
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "chapter-2-destination").OutcomeId,
                Is.EqualTo("white-station"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("first-ai-voice"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("chapter-2-wall-clock"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("chapter-2-shoe-cabinet"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("chapter-2-small-radio"));
        }

        [Test]
        public void LastPlatform_UnlocksOnlyChapterThreeAfterReturnHome()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, "ChooseDohyeonHome", "EscapeLastPlatform");
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter2));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.False);

            Perform(story, save, progress, "ReturnFromLastPlatform");
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter3).IsComplete, Is.False);
        }

        private static StoryProgress ReadyChapterTwo()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter2 };
            Complete(progress, StoryChapterId.Prologue);
            Complete(progress, StoryChapterId.Chapter1);
            return progress;
        }

        private static StoryProgress ReadyForDestination()
        {
            StoryProgress progress = ReadyChapterTwo();
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            Perform(story, save, progress,
                "ConnectRoute1", "ConnectRoute2", "ConnectRoute3", "ConnectRoute4",
                "ReturnEmployeeId", "ReturnChildShoe", "ReturnHospitalWristband", "ReturnGameCartridge",
                "ReverseAnnouncement1", "ReverseAnnouncement2", "ReverseAnnouncement3");
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
                $"Stage 15 chapter 2 runtime contract is missing OpeningStoryAction.{name}.");
            return action;
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            params string[] actions)
        {
            foreach (string name in actions)
                Assert.That(story.TryPerform(save, progress, Action(name)).Accepted, Is.True, name);
        }

        private static void Reject(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            string action)
        {
            Assert.That(story.TryPerform(save, progress, Action(action)).Accepted, Is.False, action);
        }

        private static bool Has(StoryProgress progress, string action) =>
            progress.ForeshadowFlags.Contains("s15-opening:" + action);
    }
}
#endif
