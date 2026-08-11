using NUnit.Framework;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Save.Story.Tests
{
    public sealed class StoryProgressTests
    {
        [Test]
        public void Catalog_DefinesCanonicalSequenceThroughFinalChapter()
        {
            Assert.That(StoryCatalog.All.Count, Is.EqualTo(8));
            Assert.That(StoryCatalog.Get(StoryChapterId.Prologue).SummaryId, Is.EqualTo("already-running"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter1).MemorySpaceId, Is.EqualTo("fourth-seat"));
            Assert.That(StoryCatalog.Get(StoryChapterId.Chapter6).MemorySpaceId, Is.EqualTo("window-city"));
            Assert.That(StoryCatalog.Get(StoryChapterId.FinalChapter).SummaryId, Is.EqualTo("white-room"));
        }

        [Test]
        public void Advance_RequiresEveryChapterCompletionFlag()
        {
            var progress = new StoryProgress();
            var flow = new StoryFlowService();
            StoryChapterProgress prologue = progress.GetChapter(StoryChapterId.Prologue);
            prologue.ObjectiveCompleted = true;
            prologue.DialogueCompleted = true;
            prologue.PuzzleCompleted = true;

            Assert.That(flow.TryAdvance(progress), Is.False);
            prologue.MemorySpaceCompleted = true;
            Assert.That(flow.TryAdvance(progress), Is.True);
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter1));
        }

        [Test]
        public void PrologueAndFourthSeat_StaySequentialAndRequireEveryStoryBeat()
        {
            var progress = new StoryProgress();
            var flow = new StoryFlowService();

            Assert.That(flow.Current(progress).Id, Is.EqualTo(StoryChapterId.Prologue));
            Assert.That(flow.TryAdvance(progress), Is.False,
                "A new game must not skip the prologue.");

            Complete(progress.GetChapter(StoryChapterId.Prologue));
            Assert.That(flow.TryAdvance(progress), Is.True);
            Assert.That(flow.Current(progress).Id, Is.EqualTo(StoryChapterId.Chapter1));

            StoryChapterProgress fourthSeat = progress.GetChapter(StoryChapterId.Chapter1);
            fourthSeat.ObjectiveCompleted = true;
            fourthSeat.DialogueCompleted = true;
            fourthSeat.PuzzleCompleted = true;
            Assert.That(flow.TryAdvance(progress), Is.False,
                "Chapter 1 must remain locked until the fourth-seat memory is complete.");

            fourthSeat.MemorySpaceCompleted = true;
            Assert.That(flow.TryAdvance(progress), Is.True);
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter2));
        }

        [Test]
        public void PrologueAndFourthSeat_RoundTripPreservesSceneChoiceClueAndRelationship()
        {
            var source = SaveData.CreateNew();
            source.ActiveSceneId = "04_StoryRoute";
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter1 };
            Complete(progress.GetChapter(StoryChapterId.Prologue));
            StoryChapterProgress fourthSeat = progress.GetChapter(StoryChapterId.Chapter1);
            fourthSeat.ObjectiveCompleted = true;
            fourthSeat.DialogueCompleted = true;
            fourthSeat.PuzzleCompleted = true;

            var flow = new StoryFlowService();
            flow.RecordChoice(progress, "fourth-seat-name", "seoyun");
            flow.SetFlag(progress, "repeat-109", false);
            flow.SetFlag(progress, "first-memory-door-open", false);
            new StoryRelationshipService().Set(source, "girl", 7);
            var store = new SaveDataStoryProgressStore();
            store.Save(source, progress);

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(source), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restoredSave.ActiveSceneId, Is.EqualTo("04_StoryRoute"));
            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter1));
            Assert.That(restored.GetChapter(StoryChapterId.Prologue).IsComplete, Is.True);
            Assert.That(restored.GetChapter(StoryChapterId.Chapter1).IsComplete, Is.False);
            Assert.That(restored.ImportantChoices.Find(x => x.ChoiceId == "fourth-seat-name").OutcomeId,
                Is.EqualTo("seoyun"));
            Assert.That(restored.ForeshadowFlags, Does.Contain("repeat-109"));
            Assert.That(restored.ForeshadowFlags, Does.Contain("first-memory-door-open"));
            Assert.That(new StoryRelationshipService().Get(restoredSave, "girl"), Is.EqualTo(7));
        }

        [Test]
        public void FinalChapter_RequiresPrologueAndAllSixChapters()
        {
            var progress = new StoryProgress();
            var flow = new StoryFlowService();
            for (int i = 0; i <= (int)StoryChapterId.Chapter6; i++)
            {
                StoryChapterProgress chapter = progress.GetChapter((StoryChapterId)i);
                chapter.ObjectiveCompleted = chapter.DialogueCompleted = chapter.PuzzleCompleted = chapter.MemorySpaceCompleted = true;
                progress.CurrentChapter = (StoryChapterId)i;
                Assert.That(flow.TryAdvance(progress), Is.True);
            }
            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.FinalChapter));
            Assert.That(flow.CanEnterFinalChapter(progress), Is.True);
        }

        [Test]
        public void StoryState_RoundTripsInsideExistingSaveExtension()
        {
            var save = SaveData.CreateNew();
            save.Relationships.Add(new RelationshipSaveEntry { CharacterId = "girl", Value = 72 });
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter3 };
            progress.GetChapter(StoryChapterId.Chapter2).MemorySpaceCompleted = true;
            var flow = new StoryFlowService();
            flow.RecordChoice(progress, "platform-destination", "white-station");
            flow.SetFlag(progress, "choice-text-overwritten", true);
            flow.SetFlag(progress, "repeat-109", false);
            var store = new SaveDataStoryProgressStore();
            store.Save(save, progress);

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restoredSave), Is.True);
            StoryProgress restored = store.Load(restoredSave);

            Assert.That(restored.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(restored.GetChapter(StoryChapterId.Chapter2).MemorySpaceCompleted, Is.True);
            Assert.That(restored.ImportantChoices[0].OutcomeId, Is.EqualTo("white-station"));
            Assert.That(restored.ExternalEntityFlags, Contains.Item("choice-text-overwritten"));
            Assert.That(restored.ForeshadowFlags, Contains.Item("repeat-109"));
            Assert.That(restoredSave.Relationships[0].Value, Is.EqualTo(72));
        }

        [Test]
        public void LegacySaveWithoutStoryExtension_LoadsSafeDefaults()
        {
            var legacy = SaveData.CreateNew();
            legacy.CheckpointId = "stage14-checkpoint";

            StoryProgress progress = new SaveDataStoryProgressStore().Load(legacy);

            Assert.That(progress.CurrentChapter, Is.EqualTo(StoryChapterId.Prologue));
            Assert.That(progress.Chapters, Is.Empty);
            Assert.That(legacy.CheckpointId, Is.EqualTo("stage14-checkpoint"));
        }

        [Test]
        public void RecordingChoice_UpdatesOutcomeWithoutDuplicatingKey()
        {
            var progress = new StoryProgress();
            var flow = new StoryFlowService();
            flow.RecordChoice(progress, "office-record", "protect-girl");
            flow.RecordChoice(progress, "office-record", "inspect-server");

            Assert.That(progress.ImportantChoices.Count, Is.EqualTo(1));
            Assert.That(progress.ImportantChoices[0].OutcomeId, Is.EqualTo("inspect-server"));
        }

        [Test]
        public void Relationship_UsesExistingSaveContractAndStage11Range()
        {
            var save = SaveData.CreateNew();
            var relationships = new StoryRelationshipService();

            Assert.That(relationships.Get(save, "girl"), Is.Zero);
            Assert.That(relationships.Set(save, "girl", 140), Is.EqualTo(100));
            Assert.That(relationships.Set(save, "girl", -120), Is.EqualTo(-100));
            Assert.That(save.Relationships.Count, Is.EqualTo(1));
        }

        private static void Complete(StoryChapterProgress chapter)
        {
            chapter.ObjectiveCompleted = true;
            chapter.DialogueCompleted = true;
            chapter.PuzzleCompleted = true;
            chapter.MemorySpaceCompleted = true;
        }
    }
}

