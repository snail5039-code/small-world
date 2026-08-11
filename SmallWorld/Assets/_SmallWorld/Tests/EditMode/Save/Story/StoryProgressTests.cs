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
    }
}

