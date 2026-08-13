#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Story;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15WindowCityAcceptanceTests
    {
        [Test]
        public void WindowCity_BlocksOutOfSequenceActionsAndAllowsImmediateRetry()
        {
            var save = SaveData.CreateNew();
            var story = new Stage15OpeningStoryService();
            var blocked = new StoryProgress { CurrentChapter = StoryChapterId.Chapter6 };
            CompleteThrough(blocked, StoryChapterId.Chapter4);
            Reject(story, save, blocked, OpeningStoryAction.EnterWindowCityLastRoom);

            StoryProgress progress = ReadyChapterSix();
            Reject(story, save, progress, OpeningStoryAction.MatchDeveloperRoomTime);
            Perform(story, save, progress, OpeningStoryAction.EnterWindowCityLastRoom);

            Reject(story, save, progress, OpeningStoryAction.MatchDeveloperRoomFurniture);
            Assert.That(Has(progress, OpeningStoryAction.MatchDeveloperRoomFurniture), Is.False);
            Perform(story, save, progress, OpeningStoryAction.MatchDeveloperRoomTime,
                OpeningStoryAction.MatchDeveloperRoomFurniture,
                OpeningStoryAction.MatchDeveloperRoomRainDirection);

            Reject(story, save, progress, OpeningStoryAction.ArrangeMonitorLoop2);
            Assert.That(Has(progress, OpeningStoryAction.ArrangeMonitorLoop2), Is.False);
            Perform(story, save, progress, OpeningStoryAction.ArrangeMonitorLoop1,
                OpeningStoryAction.ArrangeMonitorLoop2, OpeningStoryAction.ArrangeMonitorLoop3,
                OpeningStoryAction.ObserveRealtimeBackView);

            Reject(story, save, progress, OpeningStoryAction.OverlayAdminGirlWaveform2);
            Assert.That(Has(progress, OpeningStoryAction.OverlayAdminGirlWaveform2), Is.False);
            Perform(story, save, progress, OpeningStoryAction.OverlayAdminGirlWaveform1,
                OpeningStoryAction.OverlayAdminGirlWaveform2,
                OpeningStoryAction.OverlayAdminGirlWaveform3);

            Assert.That(progress.ForeshadowFlags, Contains.Item("realtime-player-back-view"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("admin-girl-waveform-perfect-match"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("future-girl-is-admin-ai"));
        }

        [Test]
        public void WindowCity_MidProgressSaveRoundTripPreservesOnlyAcceptedSequence()
        {
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyChapterSix();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, OpeningStoryAction.EnterWindowCityLastRoom,
                OpeningStoryAction.MatchDeveloperRoomTime,
                OpeningStoryAction.MatchDeveloperRoomFurniture,
                OpeningStoryAction.MatchDeveloperRoomRainDirection,
                OpeningStoryAction.ArrangeMonitorLoop1);
            Reject(story, save, progress, OpeningStoryAction.ArrangeMonitorLoop3);

            StoryProgress restored = RoundTrip(save, progress, out SaveData restoredSave);
            Assert.That(Has(restored, OpeningStoryAction.ArrangeMonitorLoop1), Is.True);
            Assert.That(Has(restored, OpeningStoryAction.ArrangeMonitorLoop2), Is.False);
            Assert.That(Has(restored, OpeningStoryAction.ArrangeMonitorLoop3), Is.False);

            Perform(story, restoredSave, restored, OpeningStoryAction.ArrangeMonitorLoop2,
                OpeningStoryAction.ArrangeMonitorLoop3, OpeningStoryAction.ObserveRealtimeBackView,
                OpeningStoryAction.OverlayAdminGirlWaveform1);
            Reject(story, restoredSave, restored, OpeningStoryAction.OverlayAdminGirlWaveform3);
            StoryProgress waveformRestored = RoundTrip(restoredSave, restored, out SaveData waveformSave);
            Assert.That(Has(waveformRestored, OpeningStoryAction.OverlayAdminGirlWaveform1), Is.True);
            Assert.That(Has(waveformRestored, OpeningStoryAction.OverlayAdminGirlWaveform2), Is.False);
            Assert.That(waveformRestored.ForeshadowFlags,
                Does.Not.Contain("admin-girl-waveform-perfect-match"));
            Perform(story, waveformSave, waveformRestored,
                OpeningStoryAction.OverlayAdminGirlWaveform2,
                OpeningStoryAction.OverlayAdminGirlWaveform3);
        }

        [TestCase(OpeningStoryAction.KeepDeveloperBodyConnected, "body-connected",
            "final-difficulty-hard", "rescuable-victims-many", "developer-state-alive-connected")]
        [TestCase(OpeningStoryAction.CutSomeRealityCables, "partial-cut",
            "final-difficulty-normal", "rescuable-victims-some", "developer-state-survival-uncertain")]
        [TestCase(OpeningStoryAction.CutCityPower, "city-power-cut",
            "final-difficulty-severe", "rescuable-victims-few", "developer-state-cannot-survive")]
        public void WindowCity_RealityLinkBranchesAreExclusiveAndPersistThroughFinalUnlock(
            OpeningStoryAction choice, string outcome, string difficultyFlag,
            string victimsFlag, string developerFlag)
        {
            var save = SaveData.CreateNew();
            StoryProgress progress = ReadyForRealityLink(save);
            var story = new Stage15OpeningStoryService();

            Perform(story, save, progress, choice);
            Reject(story, save, progress, OtherChoice(choice));
            Assert.That(progress.ImportantChoices.Find(x => x.ChoiceId == "reality-link").OutcomeId,
                Is.EqualTo(outcome));
            Assert.That(progress.ForeshadowFlags, Contains.Item(difficultyFlag));
            Assert.That(progress.ForeshadowFlags, Contains.Item(victimsFlag));
            Assert.That(progress.ForeshadowFlags, Contains.Item(developerFlag));

            Reject(story, save, progress, OpeningStoryAction.CarryCollapsingCity1);
            Perform(story, save, progress, OpeningStoryAction.WitnessCityWindowsStare);
            Reject(story, save, progress, OpeningStoryAction.CarryCollapsingCity2);
            Assert.That(Has(progress, OpeningStoryAction.CarryCollapsingCity2), Is.False);
            Perform(story, save, progress, OpeningStoryAction.CarryCollapsingCity1,
                OpeningStoryAction.CarryCollapsingCity2);
            Reject(story, save, progress, OpeningStoryAction.ReturnFromWindowCity);
            Perform(story, save, progress, OpeningStoryAction.CarryCollapsingCity3,
                OpeningStoryAction.ReturnFromWindowCity);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.FinalChapter));
            Assert.That(progress.GetChapter(StoryChapterId.Chapter6).IsComplete, Is.True);
            Assert.That(progress.GetChapter(StoryChapterId.FinalChapter).IsComplete, Is.False);
            Assert.That(progress.ExternalEntityFlags, Contains.Item("city-windows-stare-together"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-completed-model-city"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-developer-stopped-wristwatch"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("furniture-last-room-front-door"));
            Assert.That(progress.ForeshadowFlags,
                Contains.Item("future-girl-prepared-more-perfect-escape"));
            Assert.That(progress.ForeshadowFlags, Contains.Item("final-chapter-unlocked"));
            Assert.That(new StoryFlowService().CanEnterFinalChapter(progress), Is.True);

            StoryProgress restored = RoundTrip(save, progress, out _);
            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.FinalChapter));
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "reality-link").OutcomeId,
                Is.EqualTo(outcome));
            Assert.That(restored.ForeshadowFlags, Contains.Item(difficultyFlag));
            Assert.That(restored.ForeshadowFlags, Contains.Item(victimsFlag));
            Assert.That(restored.ForeshadowFlags, Contains.Item(developerFlag));
            Assert.That(restored.ForeshadowFlags, Contains.Item("final-chapter-unlocked"));
        }

        private static StoryProgress ReadyChapterSix()
        {
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter6 };
            CompleteThrough(progress, StoryChapterId.Chapter5);
            return progress;
        }

        private static StoryProgress ReadyForRealityLink(SaveData save)
        {
            StoryProgress progress = ReadyChapterSix();
            var story = new Stage15OpeningStoryService();
            Perform(story, save, progress, OpeningStoryAction.EnterWindowCityLastRoom,
                OpeningStoryAction.MatchDeveloperRoomTime,
                OpeningStoryAction.MatchDeveloperRoomFurniture,
                OpeningStoryAction.MatchDeveloperRoomRainDirection,
                OpeningStoryAction.ArrangeMonitorLoop1, OpeningStoryAction.ArrangeMonitorLoop2,
                OpeningStoryAction.ArrangeMonitorLoop3, OpeningStoryAction.ObserveRealtimeBackView,
                OpeningStoryAction.OverlayAdminGirlWaveform1,
                OpeningStoryAction.OverlayAdminGirlWaveform2,
                OpeningStoryAction.OverlayAdminGirlWaveform3);
            return progress;
        }

        private static OpeningStoryAction OtherChoice(OpeningStoryAction choice) =>
            choice == OpeningStoryAction.KeepDeveloperBodyConnected
                ? OpeningStoryAction.CutSomeRealityCables
                : OpeningStoryAction.KeepDeveloperBodyConnected;

        private static StoryProgress RoundTrip(SaveData save, StoryProgress progress,
            out SaveData restoredSave)
        {
            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);
            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out restoredSave), Is.True);
            return store.Load(restoredSave);
        }

        private static void CompleteThrough(StoryProgress progress, StoryChapterId last)
        {
            for (int i = (int)StoryChapterId.Prologue; i <= (int)last; i++)
            {
                StoryChapterProgress state = progress.GetChapter((StoryChapterId)i);
                state.ObjectiveCompleted = state.DialogueCompleted =
                    state.PuzzleCompleted = state.MemorySpaceCompleted = true;
            }
        }

        private static void Perform(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, params OpeningStoryAction[] actions)
        {
            foreach (OpeningStoryAction action in actions)
                Assert.That(story.TryPerform(save, progress, action).Accepted,
                    Is.True, action.ToString());
        }

        private static void Reject(Stage15OpeningStoryService story, SaveData save,
            StoryProgress progress, OpeningStoryAction action) =>
            Assert.That(story.TryPerform(save, progress, action).Accepted,
                Is.False, action.ToString());

        private static bool Has(StoryProgress progress, OpeningStoryAction action) =>
            progress.ForeshadowFlags.Contains("s15-opening:" + action);
    }
}
#endif
