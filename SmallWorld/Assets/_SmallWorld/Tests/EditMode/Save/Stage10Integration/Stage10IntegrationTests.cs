using System;
using System.Collections.Generic;
using NUnit.Framework;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Puzzle.Stage9;
using SmallWorld.Puzzle.Stage9Integration;
using SmallWorld.Save.Stage10;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEngine;

namespace SmallWorld.Save.Stage10.Integration.Tests
{
    public sealed class Stage10IntegrationTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void CaptureAndRestore_UsesRealPuzzleRelationshipAndInventoryState()
        {
            RealityRoomSaveCoordinator source = CreateCoordinator(out Stage7DialogueView sourceDialogue,
                out Stage8RecordView sourceRecords, out PhotoPuzzleView sourcePuzzle);
            sourceDialogue.State.Set("mira.relationship", 8);
            sourceRecords.AddRecord(new InventoryRecord("memory.test", RecordKind.MemoryFragment, "기억", "설명", 17));
            sourcePuzzle.RestoreSnapshot(new PuzzleSnapshot(new[] {
                new PuzzleSnapshotEntry(PhotoPuzzleView.PuzzleId, PuzzleStatus.InProgress, 2, 1) }));

            SaveData saved = source.Capture(RealityRoomSaveCoordinator.WhiteChairCheckpoint);

            Assert.That(saved.Puzzles[0].CurrentStep, Is.EqualTo(2));
            Assert.That(saved.Relationships[0].Value, Is.EqualTo(8));
            Assert.That(saved.Inventory[0].ItemId, Is.EqualTo("memory.test"));
            RealityRoomSaveCoordinator target = CreateCoordinator(out Stage7DialogueView targetDialogue,
                out Stage8RecordView targetRecords, out PhotoPuzzleView targetPuzzle);
            targetDialogue.State.Set("stale.relationship", 99);
            target.Restore(saved);
            Assert.That(targetDialogue.State.Get("mira.relationship"), Is.EqualTo(8));
            Assert.That(targetDialogue.State.Get("stale.relationship"), Is.Zero,
                "Relationships absent from the save must return to their default value.");
            Assert.That(targetRecords.Reader.Contains("memory.test"), Is.True);
            Assert.That(targetPuzzle.CurrentState.CurrentStep, Is.EqualTo(2));
        }

        [Test]
        public void ManualSavePanelContract_UsesExactlyThreeCoreSlots()
        {
            var fake = new MemorySaveService();
            Stage10SaveRuntime.Configure(fake);
            RealityRoomSaveCoordinator coordinator = CreateCoordinator(out _, out _, out _);

            Assert.That(coordinator.SaveManual(0), Is.True);
            Assert.That(coordinator.SaveManual(1), Is.True);
            Assert.That(coordinator.SaveManual(2), Is.True);
            Assert.That(fake.Slots.Count, Is.EqualTo(3));
        }

        private RealityRoomSaveCoordinator CreateCoordinator(out Stage7DialogueView dialogue,
            out Stage8RecordView records, out PhotoPuzzleView puzzle)
        {
            GameObject root = New("Coordinator");
            dialogue = New("Dialogue").AddComponent<Stage7DialogueView>();
            records = New("Records").AddComponent<Stage8RecordView>();
            puzzle = New("Puzzle").AddComponent<PhotoPuzzleView>();
            puzzle.Configure(null, null, null, null, Array.Empty<UnityEngine.UI.Button>(), null, null, null, dialogue, records, null);
            RealityRoomSaveCoordinator coordinator = root.AddComponent<RealityRoomSaveCoordinator>();
            coordinator.Configure(null, dialogue, records, puzzle, null);
            return coordinator;
        }

        private GameObject New(string name) { var value = new GameObject(name); objects.Add(value); return value; }

        private sealed class MemorySaveService : IGameSaveService
        {
            public readonly Dictionary<int, SaveData> Slots = new Dictionary<int, SaveData>();
            public bool AutoSave(SaveData data) => true;
            public bool SaveManual(int slotIndex, SaveData data) { Slots[slotIndex] = data; return true; }
            public SaveReadResult LoadLatestAutoSave() => SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveReadResult LoadManual(int slotIndex) => Slots.TryGetValue(slotIndex, out SaveData data) ? SaveReadResult.Success(data, "memory") : SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveData StartNewGame() { Slots.Clear(); return SaveData.CreateNew(); }
        }
    }
}
