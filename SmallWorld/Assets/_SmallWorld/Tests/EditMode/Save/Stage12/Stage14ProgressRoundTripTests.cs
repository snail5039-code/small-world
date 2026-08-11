using NUnit.Framework;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage12;

namespace SmallWorld.Tests.EditMode.Save.Stage12
{
    public sealed class Stage14ProgressRoundTripTests
    {
        private const string MemorySpaceId = "first-memory";

        [Test]
        public void BinarySaveRoundTrip_PreservesMemoryReturnAndExistingCoreProgress()
        {
            SaveData source = SaveData.CreateNew();
            source.ActiveSceneId = "RealityRoom";
            source.Puzzles.Add(new PuzzleSaveEntry
            {
                PuzzleId = "photo",
                Status = 1,
                CurrentStep = 2,
                IncorrectAttempts = 1,
                Snapshot = "{photo:2}"
            });
            source.Relationships.Add(new RelationshipSaveEntry { CharacterId = "girl", Value = 4 });
            source.Inventory.Add(new InventorySaveEntry { ItemId = "memory-record", Quantity = 1 });
            source.Memories.Add(new MemorySaveEntry
            {
                MemoryId = "first-memory-record",
                IsUnlocked = true,
                IsRead = true
            });

            var progress = new MemorySpaceProgress();
            progress.Set(source, new MemorySpaceState
            {
                SpaceId = MemorySpaceId,
                Phase = MemorySpacePhase.WhiteRoom,
                HasEntered = true,
                HasExited = true,
                SafeZoneReached = true,
                VisitCount = 1,
                PuzzleProgress = 3
            });

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(source), out SaveData restored), Is.True);

            MemorySpaceState restoredMemory = progress.Get(restored, MemorySpaceId);
            Assert.That(restored.ActiveSceneId, Is.EqualTo("RealityRoom"));
            Assert.That(restoredMemory.Phase, Is.EqualTo(MemorySpacePhase.WhiteRoom));
            Assert.That(restoredMemory.HasEntered, Is.True);
            Assert.That(restoredMemory.HasExited, Is.True);
            Assert.That(restoredMemory.SafeZoneReached, Is.True);
            Assert.That(restoredMemory.VisitCount, Is.EqualTo(1));
            Assert.That(restoredMemory.PuzzleProgress, Is.EqualTo(3));
            Assert.That(restored.Puzzles[0].CurrentStep, Is.EqualTo(2));
            Assert.That(restored.Relationships[0].Value, Is.EqualTo(4));
            Assert.That(restored.Inventory[0].ItemId, Is.EqualTo("memory-record"));
            Assert.That(restored.Memories[0].IsRead, Is.True);
        }

        [Test]
        public void PartialPuzzleProgress_RoundTripsWithoutMarkingMemoryExited()
        {
            SaveData source = SaveData.CreateNew();
            source.ActiveSceneId = "FirstMemory";
            var progress = new MemorySpaceProgress();
            progress.Set(source, new MemorySpaceState
            {
                SpaceId = MemorySpaceId,
                Phase = MemorySpacePhase.Inside,
                HasEntered = true,
                HasExited = false,
                VisitCount = 1,
                PuzzleProgress = 2
            });

            var serializer = new BinarySaveDataSerializer();
            Assert.That(serializer.TryDeserialize(serializer.Serialize(source), out SaveData restored), Is.True);

            MemorySpaceState restoredMemory = progress.Get(restored, MemorySpaceId);
            Assert.That(restored.ActiveSceneId, Is.EqualTo("FirstMemory"));
            Assert.That(restoredMemory.Phase, Is.EqualTo(MemorySpacePhase.Inside));
            Assert.That(restoredMemory.HasEntered, Is.True);
            Assert.That(restoredMemory.HasExited, Is.False);
            Assert.That(restoredMemory.PuzzleProgress, Is.EqualTo(2));
        }
    }
}

