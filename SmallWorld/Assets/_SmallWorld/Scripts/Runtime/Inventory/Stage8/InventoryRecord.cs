using System;

namespace SmallWorld.Inventory.Stage8
{
    public enum RecordKind
    {
        KeyItem,
        MemoryFragment,
        Investigation,
        Photo,
        NameFragment
    }

    public sealed class InventoryRecord
    {
        public InventoryRecord(string id, RecordKind kind, string title, string description = "", int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Record id cannot be empty.", nameof(id));
            Id = id;
            Kind = kind;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            SortOrder = sortOrder;
        }

        public string Id { get; }
        public RecordKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public int SortOrder { get; }
    }

    public sealed class StoredRecord
    {
        internal StoredRecord(InventoryRecord record, long acquiredSequence)
        {
            Record = record;
            AcquiredSequence = acquiredSequence;
        }

        public InventoryRecord Record { get; }
        public long AcquiredSequence { get; }
    }

    public sealed class NewRecordEvent
    {
        internal NewRecordEvent(StoredRecord entry) { Entry = entry; }
        public StoredRecord Entry { get; }
    }
}
