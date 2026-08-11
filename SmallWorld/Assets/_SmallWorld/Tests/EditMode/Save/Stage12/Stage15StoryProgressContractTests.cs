using System;
using NUnit.Framework;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage12;

namespace SmallWorld.Tests.EditMode.Save.Stage12
{
    /// <summary>
    /// Expected-contract scaffold for stage 15. Replace ExpectedStoryProgress with the
    /// production story-progress API when that API lands; the assertions are the QA contract.
    /// </summary>
    public sealed class Stage15StoryProgressContractTests
    {
        private static readonly string[] OrderedChapters =
        {
            "prologue", "chapter-1", "chapter-2", "chapter-3",
            "chapter-4", "chapter-5", "chapter-6"
        };

        [Test]
        public void NewGame_IsLockedAndRequiresPrologueThroughChapterSixInOrder()
        {
            SaveData save = SaveData.CreateNew();
            var progress = new ExpectedStoryProgress(save);

            Assert.That(progress.NextChapter, Is.EqualTo("prologue"));
            Assert.That(progress.CanEnterFinalChapter, Is.False);

            for (int i = 0; i < OrderedChapters.Length; i++)
            {
                Assert.That(progress.NextChapter, Is.EqualTo(OrderedChapters[i]));
                progress.CompleteChapter(OrderedChapters[i]);
            }

            Assert.That(progress.NextChapter, Is.Null);
            Assert.That(progress.CanEnterFinalChapter, Is.False,
                "장 완료만으로는 중요 선택과 복선 계약을 우회할 수 없어야 한다.");
        }

        [Test]
        public void ChapterCompletion_RequiresDialoguePuzzleAndMemoryFlags()
        {
            SaveData save = SaveData.CreateNew();
            var progress = new ExpectedStoryProgress(save);

            progress.SetFlag("prologue.dialogue", true);
            progress.SetFlag("prologue.puzzle", true);
            Assert.Throws<InvalidOperationException>(() => progress.CompleteChapter("prologue"));

            progress.SetFlag("prologue.memory", true);
            progress.CompleteChapter("prologue");
            Assert.That(progress.NextChapter, Is.EqualTo("chapter-1"));
        }

        [TestCase(-8, "protect")]
        [TestCase(0, "verify")]
        [TestCase(9, "trust")]
        public void RelationshipBranchAndImportantChoice_DoNotBreakMandatoryProgress(
            int relationship, string branch)
        {
            SaveData save = BuildFinalReadySave();
            save.Relationships.Add(new RelationshipSaveEntry { CharacterId = "girl", Value = relationship });
            var progress = new ExpectedStoryProgress(save);
            progress.SetValue("relationship.branch", branch);

            Assert.That(progress.CanEnterFinalChapter, Is.True);
            Assert.That(progress.GetValue("relationship.branch"), Is.EqualTo(branch));
        }

        [Test]
        public void BinaryRoundTrip_PreservesBranchesChoicesCluesAndFinalUnlock()
        {
            SaveData source = BuildFinalReadySave();
            var progress = new ExpectedStoryProgress(source);
            progress.SetValue("chapter-2.destination", "white-station");
            progress.SetValue("chapter-3.photo", "torn");
            progress.SetValue("chapter-4.record", "original-server");
            progress.SetValue("chapter-5.name", "blank");
            progress.SetValue("chapter-6.connection", "partial-cut");
            progress.SetValue("external-presence", "revealed");
            progress.SetValue("foreshadow.repeat-109", "confirmed");
            source.ActiveSceneId = "FinalChapterEntrance";

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(source), out SaveData restored), Is.True);
            var restoredProgress = new ExpectedStoryProgress(restored);

            Assert.That(restoredProgress.CanEnterFinalChapter, Is.True);
            Assert.That(restored.ActiveSceneId, Is.EqualTo("FinalChapterEntrance"));
            Assert.That(restoredProgress.GetValue("chapter-2.destination"), Is.EqualTo("white-station"));
            Assert.That(restoredProgress.GetValue("chapter-3.photo"), Is.EqualTo("torn"));
            Assert.That(restoredProgress.GetValue("chapter-4.record"), Is.EqualTo("original-server"));
            Assert.That(restoredProgress.GetValue("chapter-5.name"), Is.EqualTo("blank"));
            Assert.That(restoredProgress.GetValue("chapter-6.connection"), Is.EqualTo("partial-cut"));
            Assert.That(restoredProgress.GetValue("external-presence"), Is.EqualTo("revealed"));
            Assert.That(restoredProgress.GetValue("foreshadow.repeat-109"), Is.EqualTo("confirmed"));
        }

        [Test]
        public void Stage14FirstMemoryRegression_PartialSaveStaysLockedAndSolvedSaveReturns()
        {
            var definition = new MemorySpaceDefinition
            {
                Id = "first-memory",
                EntrySceneId = "FirstMemory",
                ReturnSceneId = "RealityRoom",
                SafeZoneId = "safe-zone"
            };
            SaveData save = SaveData.CreateNew();
            var firstRun = new MemoryJourneyFlow(definition, "first-memory-sequence", new[] { 1, 2, 3 });
            firstRun.Enter(save);
            firstRun.SubmitChoice(save, 1);
            firstRun.SubmitChoice(save, 2);

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(save), out SaveData restored), Is.True);
            var resumed = new MemoryJourneyFlow(definition, "first-memory-sequence", new[] { 1, 2, 3 });
            Assert.That(resumed.TryExit(restored), Is.EqualTo(MemoryExitResult.BlockedByPuzzle));
            Assert.That(resumed.SubmitChoice(restored, 3), Is.True);
            Assert.That(resumed.TryExit(restored), Is.EqualTo(MemoryExitResult.ReturnedToWhiteRoom));
            Assert.That(restored.ActiveSceneId, Is.EqualTo("RealityRoom"));
        }

        private static SaveData BuildFinalReadySave()
        {
            SaveData save = SaveData.CreateNew();
            var progress = new ExpectedStoryProgress(save);
            for (int i = 0; i < OrderedChapters.Length; i++) progress.CompleteChapter(OrderedChapters[i]);
            progress.SetFlag("important-choices.recorded", true);
            progress.SetFlag("external-presence.witnessed", true);
            progress.SetFlag("foreshadow.required", true);
            return save;
        }

        private sealed class ExpectedStoryProgress
        {
            private const string SceneId = "stage-15/story";
            private readonly SaveData save;

            public ExpectedStoryProgress(SaveData saveData)
            {
                save = saveData ?? throw new ArgumentNullException(nameof(saveData));
            }

            public string NextChapter
            {
                get
                {
                    for (int i = 0; i < OrderedChapters.Length; i++)
                        if (!GetFlag(OrderedChapters[i] + ".complete")) return OrderedChapters[i];
                    return null;
                }
            }

            public bool CanEnterFinalChapter => NextChapter == null
                && GetFlag("important-choices.recorded")
                && GetFlag("external-presence.witnessed")
                && GetFlag("foreshadow.required");

            public void CompleteChapter(string chapter)
            {
                if (chapter != NextChapter) throw new InvalidOperationException("장 순서를 건너뛸 수 없습니다.");
                bool hasAnyComponentFlag = HasKey(chapter + ".dialogue")
                    || HasKey(chapter + ".puzzle") || HasKey(chapter + ".memory");
                if (hasAnyComponentFlag && (!GetFlag(chapter + ".dialogue")
                    || !GetFlag(chapter + ".puzzle") || !GetFlag(chapter + ".memory")))
                    throw new InvalidOperationException("장 필수 플래그가 완료되지 않았습니다.");
                SetFlag(chapter + ".complete", true);
            }

            public void SetFlag(string key, bool value) => SetValue(key, value ? "1" : "0");
            public bool GetFlag(string key) => GetValue(key) == "1";

            public void SetValue(string key, string value)
            {
                SceneStateSaveEntry entry = Find(key);
                if (entry == null)
                {
                    entry = new SceneStateSaveEntry { SceneId = SceneId, StateKey = key };
                    save.SceneStates.Add(entry);
                }
                entry.Value = value ?? string.Empty;
            }

            public string GetValue(string key)
            {
                SceneStateSaveEntry entry = Find(key);
                return entry == null ? string.Empty : entry.Value;
            }

            private bool HasKey(string key) => Find(key) != null;

            private SceneStateSaveEntry Find(string key) => save.SceneStates.Find(
                entry => entry.SceneId == SceneId && entry.StateKey == key);
        }
    }
}

