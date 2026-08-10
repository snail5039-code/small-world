using System;
using System.IO;
using NUnit.Framework;

namespace SmallWorld.Save.Stage10.Tests
{
    public sealed class SaveCoreTests
    {
        private string directory;
        private AtomicFileSaveStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SmallWorld-SaveTests-" + Guid.NewGuid().ToString("N"));
            store = new AtomicFileSaveStore(directory, new BinarySaveDataSerializer());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void BinaryRoundTrip_PreservesExtensibleProgressContract()
        {
            var data = CompleteData("checkpoint-white-chair");
            var serializer = new BinarySaveDataSerializer();

            SaveData restored;
            Assert.That(serializer.TryDeserialize(serializer.Serialize(data), out restored), Is.True);
            Assert.That(restored.CheckpointId, Is.EqualTo("checkpoint-white-chair"));
            Assert.That(restored.ActiveSceneId, Is.EqualTo("reality-room"));
            Assert.That(restored.Puzzles[0].Snapshot, Is.EqualTo("{photo:2}"));
            Assert.That(restored.Relationships[0].Value, Is.EqualTo(7));
            Assert.That(restored.Inventory[0].Quantity, Is.EqualTo(2));
            Assert.That(restored.Memories[0].IsRead, Is.True);
            Assert.That(restored.SceneStates[0].Value, Is.EqualTo("open"));
            Assert.That(restored.Extensions[0].Payload, Is.EqualTo("future-data"));
        }

        [Test]
        public void ManualSlots_AreExactlyThree_AndReadAtomically()
        {
            for (var i = 0; i < 3; i++)
            {
                var data = CompleteData("manual-" + i);
                Assert.That(store.Write(new SaveSlot(SaveSlotKind.Manual, i), data), Is.True);
                Assert.That(store.Read(new SaveSlot(SaveSlotKind.Manual, i)).Data.CheckpointId, Is.EqualTo("manual-" + i));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new SaveSlot(SaveSlotKind.Manual, 3));
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
        }

        [Test]
        public void AutoSave_RotatesTwoFiles_AndLoadsNewest()
        {
            var now = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var service = new GameSaveService(store, () => now = now.AddMinutes(1));

            Assert.That(service.AutoSave(CompleteData("first")), Is.True);
            Assert.That(service.AutoSave(CompleteData("second")), Is.True);
            Assert.That(service.AutoSave(CompleteData("third")), Is.True);

            var restored = service.LoadLatestAutoSave();
            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Data.CheckpointId, Is.EqualTo("third"));
            Assert.That(Directory.GetFiles(directory, "auto-*.sav").Length, Is.EqualTo(2));
        }

        [Test]
        public void Read_CorruptPrimary_RecoversFromAtomicBackup()
        {
            var slot = new SaveSlot(SaveSlotKind.Manual, 0);
            Assert.That(store.Write(slot, CompleteData("backup")), Is.True);
            Assert.That(store.Write(slot, CompleteData("primary")), Is.True);
            File.WriteAllText(Path.Combine(directory, "manual-0.sav"), "damaged");

            var restored = store.Read(slot);

            Assert.That(restored.IsSuccess, Is.True);
            Assert.That(restored.Data.CheckpointId, Is.EqualTo("backup"));
            Assert.That(restored.Source, Does.EndWith(".bak"));
            Assert.That(store.Read(slot).IsSuccess, Is.True, "The primary file should be self-healed.");
        }

        [Test]
        public void Read_DetectsUnsupportedSaveVersion()
        {
            var data = CompleteData("future");
            data.Version = SaveData.CurrentVersion + 1;
            var slot = new SaveSlot(SaveSlotKind.Manual, 0);
            Assert.That(store.Write(slot, data), Is.True);

            Assert.That(store.Read(slot).Status, Is.EqualTo(SaveReadStatus.UnsupportedVersion));
        }

        [Test]
        public void StartNewGame_RemovesAllProgress_ButReturnsCleanState()
        {
            store.Write(new SaveSlot(SaveSlotKind.Auto, 0), CompleteData("auto"));
            store.Write(new SaveSlot(SaveSlotKind.Manual, 2), CompleteData("manual"));
            var service = new GameSaveService(store);

            var fresh = service.StartNewGame();

            Assert.That(fresh.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(fresh.SaveId, Is.Not.Empty);
            Assert.That(store.Read(new SaveSlot(SaveSlotKind.Auto, 0)).Status, Is.EqualTo(SaveReadStatus.Missing));
            Assert.That(store.Read(new SaveSlot(SaveSlotKind.Manual, 2)).Status, Is.EqualTo(SaveReadStatus.Missing));
        }

        private static SaveData CompleteData(string checkpoint)
        {
            var data = SaveData.CreateNew();
            data.CheckpointId = checkpoint;
            data.ActiveSceneId = "reality-room";
            data.Puzzles.Add(new PuzzleSaveEntry { PuzzleId = "photo", Status = 1, CurrentStep = 2, IncorrectAttempts = 3, Snapshot = "{photo:2}" });
            data.Relationships.Add(new RelationshipSaveEntry { CharacterId = "companion", Value = 7 });
            data.Inventory.Add(new InventorySaveEntry { ItemId = "photo-piece", Quantity = 2 });
            data.Memories.Add(new MemorySaveEntry { MemoryId = "memory-1", IsUnlocked = true, IsRead = true });
            data.SceneStates.Add(new SceneStateSaveEntry { SceneId = "reality-room", StateKey = "door", Value = "open" });
            data.Extensions.Add(new ExtensionSaveEntry { Key = "future-system", Version = 1, Payload = "future-data" });
            return data;
        }
    }
}
