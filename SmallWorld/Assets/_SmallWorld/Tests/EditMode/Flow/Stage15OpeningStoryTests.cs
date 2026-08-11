#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15OpeningStoryTests
    {
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

        private static StoryProgress ReadyChapterOne()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter1 };
            StoryChapterProgress prologue = progress.GetChapter(StoryChapterId.Prologue);
            prologue.ObjectiveCompleted = prologue.DialogueCompleted = prologue.PuzzleCompleted = prologue.MemorySpaceCompleted = true;
            return progress;
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save, StoryProgress progress,
            params OpeningStoryAction[] actions)
        {
            foreach (OpeningStoryAction action in actions)
                Assert.That(story.TryPerform(save, progress, action).Accepted, Is.True, action.ToString());
        }
    }
}
#endif
