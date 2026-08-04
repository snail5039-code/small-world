using System.Collections.Generic;
using NUnit.Framework;

namespace SmallWorld.Inventory.Stage8.Tests
{
    public sealed class InventoryJournalTests
    {
        [Test]
        public void Add_StoresEveryRecordKind_AndRejectsDuplicateIds()
        {
            var journal = new InventoryJournal();
            int notifications = 0;
            journal.RecordAdded += _ => notifications++;

            Assert.That(journal.Add(new InventoryRecord("key", RecordKind.KeyItem, "Key")), Is.True);
            Assert.That(journal.Add(new InventoryRecord("memory", RecordKind.MemoryFragment, "Memory")), Is.True);
            Assert.That(journal.Add(new InventoryRecord("clue", RecordKind.Investigation, "Clue")), Is.True);
            Assert.That(journal.Add(new InventoryRecord("photo", RecordKind.Photo, "Photo")), Is.True);
            Assert.That(journal.Add(new InventoryRecord("name", RecordKind.NameFragment, "Name")), Is.True);
            Assert.That(journal.Add(new InventoryRecord("key", RecordKind.KeyItem, "Duplicate")), Is.False);

            Assert.That(journal.Count, Is.EqualTo(5));
            Assert.That(notifications, Is.EqualTo(5));
        }

        [Test]
        public void Queries_FilterAndSortDeterministically()
        {
            var journal = new InventoryJournal();
            journal.Add(new InventoryRecord("b", RecordKind.Photo, "B", sortOrder: 2));
            journal.Add(new InventoryRecord("a", RecordKind.Photo, "A", sortOrder: 1));
            journal.Add(new InventoryRecord("memory", RecordKind.MemoryFragment, "M"));

            Assert.That(journal.GetAll(RecordKind.Photo)[0].Record.Id, Is.EqualTo("a"));
            Assert.That(journal.GetAll(null, RecordSort.AcquiredNewest)[0].Record.Id, Is.EqualTo("memory"));
            Assert.That(journal.TryGet("b", out StoredRecord found), Is.True);
            Assert.That(found.Record.Title, Is.EqualTo("B"));
        }

        [Test]
        public void ItemUseRule_RequiresOwnedItem_RecordAndRelationshipState()
        {
            var journal = new InventoryJournal();
            var state = new FakeState { ["affection"] = 3 };
            var rule = new ItemUseRule("key",
                new RequiredRecordCondition("name"),
                new StateThresholdCondition("affection", 2));

            Assert.That(rule.CanUse(journal, state), Is.False);
            journal.Add(new InventoryRecord("key", RecordKind.KeyItem, "Key"));
            Assert.That(rule.CanUse(journal, state), Is.False);
            journal.Add(new InventoryRecord("name", RecordKind.NameFragment, "Name"));
            Assert.That(rule.CanUse(journal, state), Is.True);
        }

        [Test]
        public void DelegateStateReader_AdaptsStage7StyleGetMethod()
        {
            var values = new Dictionary<string, int> { ["trust"] = 4 };
            var adapter = new DelegateIntegerStateReader(key => values.TryGetValue(key, out int value) ? value : 0);

            Assert.That(adapter.Get("trust"), Is.EqualTo(4));
            Assert.That(adapter.Get("missing"), Is.Zero);
        }

        [Test]
        public void ItemUseRule_DefensivelyCopiesConditionArray()
        {
            var journal = new InventoryJournal();
            journal.Add(new InventoryRecord("key", RecordKind.KeyItem, "Key"));
            journal.Add(new InventoryRecord("name", RecordKind.NameFragment, "Name"));
            IItemUseCondition originalCondition = new RequiredRecordCondition("name");
            var source = new[] { originalCondition };
            var rule = new ItemUseRule("key", source);

            source[0] = new RequiredRecordCondition("missing");

            Assert.That(rule.Conditions[0], Is.SameAs(originalCondition));
            Assert.That(rule.CanUse(journal), Is.True);
        }

        private sealed class FakeState : Dictionary<string, int>, IIntegerStateReader
        {
            public int Get(string key) => TryGetValue(key, out int value) ? value : 0;
        }
    }
}
