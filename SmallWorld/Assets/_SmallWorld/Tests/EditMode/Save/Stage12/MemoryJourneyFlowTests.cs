using NUnit.Framework;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage12;

namespace SmallWorld.Tests.EditMode.Save.Stage12
{
    public sealed class MemoryJourneyFlowTests
    {
        private static MemoryJourneyFlow CreateFlow() => new MemoryJourneyFlow(
            new MemorySpaceDefinition
            {
                Id = "first-memory",
                EntrySceneId = "FirstMemory",
                ReturnSceneId = "RealityRoom",
                SafeZoneId = "safe-zone"
            },
            "first-memory-sequence",
            new[] { 1, 2, 3 });

        [Test]
        public void Enter_ActivatesMemorySceneAndPersistsVisit()
        {
            SaveData save = SaveData.CreateNew();
            MemorySpaceState state = CreateFlow().Enter(save);
            Assert.That(save.ActiveSceneId, Is.EqualTo("FirstMemory"));
            Assert.That(state.Phase, Is.EqualTo(MemorySpacePhase.Inside));
            Assert.That(state.HasEntered, Is.True);
            Assert.That(state.VisitCount, Is.EqualTo(1));
        }

        [Test]
        public void TryExit_BeforePuzzleCompletion_IsBlockedWithoutMutatingScene()
        {
            SaveData save = SaveData.CreateNew();
            MemoryJourneyFlow flow = CreateFlow();
            flow.Enter(save);
            MemoryExitResult result = flow.TryExit(save);
            Assert.That(result, Is.EqualTo(MemoryExitResult.BlockedByPuzzle));
            Assert.That(save.ActiveSceneId, Is.EqualTo("FirstMemory"));
            Assert.That(flow.RestoreSpace(save).Phase, Is.EqualTo(MemorySpacePhase.Inside));
            Assert.That(flow.RestoreSpace(save).HasExited, Is.False);
        }

        [Test]
        public void SolveThenExit_ReturnsToWhiteRoomAndPersistsCompletion()
        {
            SaveData save = SaveData.CreateNew();
            MemoryJourneyFlow flow = CreateFlow();
            flow.Enter(save);
            flow.SubmitChoice(save, 1);
            flow.SubmitChoice(save, 2);
            flow.SubmitChoice(save, 3);
            MemoryExitResult result = flow.TryExit(save);
            Assert.That(result, Is.EqualTo(MemoryExitResult.ReturnedToWhiteRoom));
            Assert.That(save.ActiveSceneId, Is.EqualTo("RealityRoom"));
            Assert.That(flow.RestorePuzzle(save).Completed, Is.True);
            Assert.That(flow.RestoreSpace(save).HasExited, Is.True);
            Assert.That(flow.RestoreSpace(save).Phase, Is.EqualTo(MemorySpacePhase.WhiteRoom));
        }

        [Test]
        public void Restore_ContinuesPartialPuzzleAcrossFlowInstances()
        {
            SaveData save = SaveData.CreateNew();
            MemoryJourneyFlow first = CreateFlow();
            first.Enter(save);
            first.SubmitChoice(save, 1);
            first.SubmitChoice(save, 2);
            MemoryJourneyFlow restored = CreateFlow();
            Assert.That(restored.RestoreSpace(save).Phase, Is.EqualTo(MemorySpacePhase.Inside));
            Assert.That(restored.RestorePuzzle(save).Progress, Is.EqualTo(2));
            Assert.That(restored.SubmitChoice(save, 3), Is.True);
            Assert.That(restored.RestorePuzzle(save).Completed, Is.True);
        }
    }
}
