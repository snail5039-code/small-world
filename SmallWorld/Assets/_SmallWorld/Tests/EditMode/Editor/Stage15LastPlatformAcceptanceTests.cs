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

            Reject(story, save, progress, "ReturnEmployeeCard");
            Reject(story, save, progress, "ReverseAnnouncement1");
            Reject(story, save, progress, "ChooseWhiteStation");
            Reject(story, save, progress, "ReturnFromPlatform");

            Perform(story, save, progress,
                "HearDohyeon", "ReadPlatformBoard",
                "ConnectLoginTime1", "ConnectLoginTime2", "ConnectLoginTime3", "ConnectLoginTime4",
                "ReturnEmployeeCard", "ReturnChildShoe", "ReturnHospitalBand", "ReturnGameCartridge",
                "ReverseAnnouncement1", "ReverseAnnouncement2", "ReverseAnnouncement3",
                "ChooseWhiteStation", "CrossSafeZone1", "CrossSafeZone2", "CrossSafeZone3",
                "ReturnFromPlatform");

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.True);
        }

        [Test]
        public void LastPlatform_WrongAnswersDoNotAdvanceAndRemainRetryable()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyChapterTwo();
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, "HearDohyeon", "ReadPlatformBoard");
            Reject(story, save, progress, "ConnectLoginTime2");
            Perform(story, save, progress, "ConnectLoginTime1");
            Assert.That(Has(progress, "ConnectLoginTime2"), Is.False);

            Perform(story, save, progress, "ConnectLoginTime2", "ConnectLoginTime3", "ConnectLoginTime4");
            Reject(story, save, progress, "ReverseAnnouncement1");
            Perform(story, save, progress, "ReturnEmployeeCard");
            Assert.That(Has(progress, "ReverseAnnouncement1"), Is.False);

            Perform(story, save, progress, "ReturnChildShoe", "ReturnHospitalBand", "ReturnGameCartridge");
            Reject(story, save, progress, "ReverseAnnouncement2");
            Perform(story, save, progress, "ReverseAnnouncement1");
            Assert.That(Has(progress, "ReverseAnnouncement2"), Is.False);
        }

        [TestCase("ChooseRealityHome", "reality-home", "victim-restoration-clue-dohyeon")]
        [TestCase("ChooseGameHouse", "game-house", "yuna-affection-memory-dohyeon")]
        [TestCase("ChooseWhiteStation", "white-station", "autonomy-clue-white-station")]
        public void LastPlatform_DestinationBranchesPersistDistinctConsequences(
            string action, string outcome, string expectedFlag)
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, action);

            StoryChoiceState choice = progress.ImportantChoices.Find(x => x.ChoiceId == "platform-destination");
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.OutcomeId, Is.EqualTo(outcome));
            Assert.That(progress.ForeshadowFlags, Contains.Item(expectedFlag));
            Reject(story, save, progress, "ChooseRealityHome");
            Reject(story, save, progress, "ChooseGameHouse");
            Reject(story, save, progress, "ChooseWhiteStation");
        }

        [Test]
        public void LastPlatform_SaveRoundTripPreservesChoiceAndCompletion()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, "ChooseWhiteStation",
                "CrossSafeZone1", "CrossSafeZone2", "CrossSafeZone3", "ReturnFromPlatform");

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            StoryProgress restored = store.Load(save);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(restored.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.True);
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "platform-destination").OutcomeId,
                Is.EqualTo("white-station"));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("first-ai-voice"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-wall-clock"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-entry-shoe-cabinet"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("furniture-small-radio"));
        }

        [Test]
        public void LastPlatform_UnlocksOnlyChapterThreeAfterReturnHome()
        {
            var save = SaveData.CreateNew();
            var progress = ReadyForDestination();
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, "ChooseRealityHome",
                "CrossSafeZone1", "CrossSafeZone2", "CrossSafeZone3");
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter2));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter2).IsComplete, Is.False);

            Perform(story, save, progress, "ReturnFromPlatform");
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
                "HearDohyeon", "ReadPlatformBoard",
                "ConnectLoginTime1", "ConnectLoginTime2", "ConnectLoginTime3", "ConnectLoginTime4",
                "ReturnEmployeeCard", "ReturnChildShoe", "ReturnHospitalBand", "ReturnGameCartridge",
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
