#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15FinalWhiteRoomAcceptanceTests
    {
        private static readonly OpeningStoryAction[] PreserveAll =
        {
            OpeningStoryAction.PreserveChapter1Furniture, OpeningStoryAction.PreserveChapter2Furniture,
            OpeningStoryAction.PreserveChapter3Furniture, OpeningStoryAction.PreserveChapter4Furniture,
            OpeningStoryAction.PreserveChapter5Furniture, OpeningStoryAction.PreserveChapter6Furniture
        };

        private static readonly OpeningStoryAction[] DestroyAll =
        {
            OpeningStoryAction.DestroyChapter1Furniture, OpeningStoryAction.DestroyChapter2Furniture,
            OpeningStoryAction.DestroyChapter3Furniture, OpeningStoryAction.DestroyChapter4Furniture,
            OpeningStoryAction.DestroyChapter5Furniture, OpeningStoryAction.DestroyChapter6Furniture
        };

        [Test]
        public void FinalChapter_GatesEntryAndRejectsOutOfOrderOrDuplicateDecisionsWithoutMutation()
        {
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            StoryProgress incomplete = ReadyFinal("body-connected", true);
            incomplete.GetChapter(StoryChapterId.Chapter6).MemorySpaceCompleted = false;
            Reject(story, save, incomplete, OpeningStoryAction.EnterLivingHouse);

            StoryProgress locked = ReadyFinal("body-connected", true);
            locked.ForeshadowFlags.Remove("final-chapter-unlocked");
            Reject(story, save, locked, OpeningStoryAction.EnterLivingHouse);
            Assert.That(locked.ForeshadowFlags, Does.Not.Contain(Mark(OpeningStoryAction.EnterLivingHouse)));

            StoryProgress progress = ReadyFinal("body-connected", true);
            Reject(story, save, progress, OpeningStoryAction.PreserveChapter1Furniture);
            Perform(story, save, progress, OpeningStoryAction.EnterLivingHouse);
            Reject(story, save, progress, OpeningStoryAction.PreserveChapter2Furniture);
            Perform(story, save, progress, OpeningStoryAction.DestroyChapter1Furniture);
            Reject(story, save, progress, OpeningStoryAction.PreserveChapter1Furniture);
            Reject(story, save, progress, OpeningStoryAction.DestroyChapter1Furniture);
            Reject(story, save, progress, OpeningStoryAction.DestroyManagementCore1);
            Reject(story, save, progress, OpeningStoryAction.EnterWhiteRoom);

            AssertChoice(progress, "final-memory-furniture-1", "destroy");
            Assert.That(progress.ImportantChoices.Exists(x => x.ChoiceId == "final-memory-furniture-2"), Is.False);
            Assert.That(progress.ImportantChoices.Exists(x => x.ChoiceId.StartsWith("final-core-result-", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void FinalChapter_PreserveDestroyAndCoreOrderProduceExclusiveVictimAndGirlResults()
        {
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyFinal("body-connected", true);
            Perform(story, save, progress, OpeningStoryAction.EnterLivingHouse,
                OpeningStoryAction.PreserveChapter1Furniture, OpeningStoryAction.DestroyChapter2Furniture,
                OpeningStoryAction.PreserveChapter3Furniture, OpeningStoryAction.DestroyChapter4Furniture,
                OpeningStoryAction.PreserveChapter5Furniture, OpeningStoryAction.DestroyChapter6Furniture);

            Reject(story, save, progress, OpeningStoryAction.DestroyManagementCore2);
            for (int i = 0; i < 6; i++)
                Perform(story, save, progress, (OpeningStoryAction)((int)OpeningStoryAction.DestroyManagementCore1 + i));

            for (int chapter = 1; chapter <= 6; chapter++)
            {
                bool retained = chapter % 2 == 1;
                AssertChoice(progress, "final-core-result-" + chapter, retained ? "victim-retained" : "girl-assimilated");
                Assert.That(progress.ForeshadowFlags,
                    Contains.Item((retained ? "final-victim-retained-" : "final-girl-assimilated-") + chapter));
                Assert.That(progress.ForeshadowFlags,
                    Does.Not.Contain((retained ? "final-girl-assimilated-" : "final-victim-retained-") + chapter));
            }
        }

        [Test]
        public void FinalChapter_RealityLinkBranchesAndWhiteRoomDialogueTransformInStrictOrder()
        {
            string[] links = { "body-connected", "partial-cut", "city-power-cut" };
            string[] personalities = { "developer", "composite", "protagonist" };
            for (int branch = 0; branch < links.Length; branch++)
            {
                var story = new Stage15OpeningStoryService();
                var save = SaveData.CreateNew();
                StoryProgress progress = ReadyFinal(links[branch], false);
                PerformThroughCores(story, save, progress, DestroyAll);

                Reject(story, save, progress, OpeningStoryAction.HearGirlAsDeveloper1);
                Perform(story, save, progress, OpeningStoryAction.EnterWhiteRoom);
                AssertChoice(progress, "reality-link", links[branch]);
                AssertChoice(progress, "reality-body-remaining-personality", personalities[branch]);
                Assert.That(progress.ForeshadowFlags, Contains.Item("reality-body-personality-" + personalities[branch]));

                Reject(story, save, progress, OpeningStoryAction.SitInSecondChair);
                Perform(story, save, progress, OpeningStoryAction.SitInFirstChair,
                    OpeningStoryAction.SitInSecondChair, OpeningStoryAction.ActivateOldComputer);
                Reject(story, save, progress, OpeningStoryAction.HearGirlAsDeveloper2);
                Perform(story, save, progress, OpeningStoryAction.HearGirlAsDeveloper1,
                    OpeningStoryAction.HearGirlAsDeveloper2, OpeningStoryAction.HearGirlAsDeveloper3);
                Reject(story, save, progress, OpeningStoryAction.HearGirlAsDeveloper3);
                Assert.That(progress.ForeshadowFlags, Does.Not.Contain("final-choice-ready"));
            }
        }

        [Test]
        public void FinalChapter_CalculatesSixChoiceFlagsAndTrueEndingAvailabilityWithoutExecutingEnding()
        {
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            StoryProgress ideal = ReadyFinal("body-connected", true);
            PerformUntilReady(story, save, ideal, PreserveAll);

            AssertAvailability(ideal, "program-termination", true);
            AssertAvailability(ideal, "connect-model-house", true);
            AssertAvailability(ideal, "stay-with-girl", false);
            AssertAvailability(ideal, "become-new-manager", true);
            AssertAvailability(ideal, "send-girl-to-reality", false);
            AssertAvailability(ideal, "restore-victims-distribute-memories", true);
            Assert.That(ideal.ForeshadowFlags, Contains.Item("true-ending-available"));
            AssertReadyWithoutEnding(ideal);

            StoryProgress destroyed = ReadyFinal("partial-cut", false);
            PerformUntilReady(story, SaveData.CreateNew(), destroyed, DestroyAll);
            AssertAvailability(destroyed, "program-termination", true);
            AssertAvailability(destroyed, "connect-model-house", false);
            AssertAvailability(destroyed, "stay-with-girl", true);
            AssertAvailability(destroyed, "become-new-manager", false);
            AssertAvailability(destroyed, "send-girl-to-reality", false);
            AssertAvailability(destroyed, "restore-victims-distribute-memories", false);
            Assert.That(destroyed.ForeshadowFlags, Contains.Item("true-ending-unavailable"));
            AssertReadyWithoutEnding(destroyed);
        }

        [Test]
        public void FinalChapter_FinalChoiceReadyRoundTripPreservesStateAndEarlierChapterRegressionContract()
        {
            var story = new Stage15OpeningStoryService();
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyFinal("body-connected", true);
            PerformUntilReady(story, save, progress, PreserveAll);

            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.FinalChapter));
            for (int chapter = (int)StoryChapterId.Prologue; chapter <= (int)StoryChapterId.Chapter6; chapter++)
                Assert.That(restored.GetChapter((StoryChapterId)chapter).IsComplete, Is.True, ((StoryChapterId)chapter).ToString());
            for (int chapter = 1; chapter <= 6; chapter++)
            {
                AssertChoice(restored, "final-memory-furniture-" + chapter, "preserve");
                AssertChoice(restored, "final-core-result-" + chapter, "victim-retained");
            }
            AssertChoice(restored, "reality-link", "body-connected");
            AssertChoice(restored, "reality-body-remaining-personality", "developer");
            Assert.That(restored.ForeshadowFlags, Contains.Item("true-ending-available"));
            AssertReadyWithoutEnding(restored);
            Reject(story, restoredSave, restored, OpeningStoryAction.PrepareFinalChoice);
            AssertReadyWithoutEnding(restored);
        }

        private static StoryProgress ReadyFinal(string realityLink, bool idealVictimSources)
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.FinalChapter };
            for (int chapter = (int)StoryChapterId.Prologue; chapter <= (int)StoryChapterId.Chapter6; chapter++)
            {
                StoryChapterProgress state = progress.GetChapter((StoryChapterId)chapter);
                state.ObjectiveCompleted = state.DialogueCompleted = state.PuzzleCompleted = state.MemorySpaceCompleted = true;
            }
            progress.ForeshadowFlags.Add("final-chapter-unlocked");
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "fourth-seat-name", OutcomeId = idealVictimSources ? "seoyun" : "yuna" });
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "platform-destination", OutcomeId = idealVictimSources ? "reality-home" : "game-house" });
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "perfect-day-photo", OutcomeId = idealVictimSources ? "tear" : "preserve" });
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "office-record", OutcomeId = idealVictimSources ? "original-server" : "protect-girl" });
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "dead-person-name", OutcomeId = idealVictimSources ? "blank" : "invented-name" });
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "reality-link", OutcomeId = realityLink });
            if (idealVictimSources) progress.ForeshadowFlags.Add("autonomy-clue-white-station");
            return progress;
        }

        private static void PerformThroughCores(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, OpeningStoryAction[] furniture)
        {
            Perform(story, save, progress, OpeningStoryAction.EnterLivingHouse);
            Perform(story, save, progress, furniture);
            for (int i = 0; i < 6; i++)
                Perform(story, save, progress, (OpeningStoryAction)((int)OpeningStoryAction.DestroyManagementCore1 + i));
        }

        private static void PerformUntilReady(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, OpeningStoryAction[] furniture)
        {
            PerformThroughCores(story, save, progress, furniture);
            Perform(story, save, progress, OpeningStoryAction.EnterWhiteRoom,
                OpeningStoryAction.SitInFirstChair, OpeningStoryAction.SitInSecondChair,
                OpeningStoryAction.ActivateOldComputer, OpeningStoryAction.HearGirlAsDeveloper1,
                OpeningStoryAction.HearGirlAsDeveloper2, OpeningStoryAction.HearGirlAsDeveloper3,
                OpeningStoryAction.PrepareFinalChoice);
        }

        private static void AssertAvailability(StoryProgress progress, string id, bool available)
        {
            string expected = "final-choice-" + (available ? "available-" : "unavailable-") + id;
            string opposite = "final-choice-" + (available ? "unavailable-" : "available-") + id;
            Assert.That(progress.ForeshadowFlags, Contains.Item(expected));
            Assert.That(progress.ForeshadowFlags, Does.Not.Contain(opposite));
        }

        private static void AssertReadyWithoutEnding(StoryProgress progress)
        {
            Assert.That(progress.ForeshadowFlags, Contains.Item("final-choice-ready"));
            Assert.That(progress.GetChapter(StoryChapterId.FinalChapter).IsComplete, Is.False);
            Assert.That(progress.ImportantChoices.Exists(x => x.ChoiceId.StartsWith("final-ending", StringComparison.Ordinal)), Is.False);
        }

        private static void AssertChoice(StoryProgress progress, string id, string outcome)
        {
            StoryChoiceState choice = progress.ImportantChoices.Find(x => x.ChoiceId == id);
            Assert.That(choice, Is.Not.Null, id);
            Assert.That(choice.OutcomeId, Is.EqualTo(outcome), id);
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, params OpeningStoryAction[] actions)
        {
            foreach (OpeningStoryAction action in actions)
                Assert.That(story.TryPerform(save, progress, action).Accepted, Is.True, action.ToString());
        }

        private static void Reject(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, OpeningStoryAction action) =>
            Assert.That(story.TryPerform(save, progress, action).Accepted, Is.False, action.ToString());

        private static string Mark(OpeningStoryAction action) => "s15-opening:" + action;
    }
}
#endif
