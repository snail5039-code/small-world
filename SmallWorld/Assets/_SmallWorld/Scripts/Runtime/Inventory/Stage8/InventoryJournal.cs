using System;
using System.Collections.Generic;

namespace SmallWorld.Inventory.Stage8
{
    public interface IInventoryReader
    {
        bool Contains(string id);
        bool TryGet(string id, out StoredRecord record);
        IReadOnlyList<StoredRecord> GetAll(RecordKind? kind = null, RecordSort sort = RecordSort.Catalog);
    }

    public interface IRecordCollector
    {
        bool Add(InventoryRecord record);
    }

    public sealed class InventoryJournal : IInventoryReader, IRecordCollector
    {
        private readonly Dictionary<string, StoredRecord> records = new Dictionary<string, StoredRecord>(StringComparer.Ordinal);
        private long sequence;

        public event Action<NewRecordEvent> RecordAdded;
        public int Count => records.Count;

        public bool Add(InventoryRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (records.ContainsKey(record.Id)) return false;
            var stored = new StoredRecord(record, ++sequence);
            records.Add(record.Id, stored);
            RecordAdded?.Invoke(new NewRecordEvent(stored));
            return true;
        }

        public bool Contains(string id) => !string.IsNullOrEmpty(id) && records.ContainsKey(id);

        public bool TryGet(string id, out StoredRecord record) => records.TryGetValue(id ?? string.Empty, out record);

        public void Clear()
        {
            records.Clear();
            sequence = 0;
        }

        public IReadOnlyList<StoredRecord> GetAll(RecordKind? kind = null, RecordSort sort = RecordSort.Catalog)
        {
            var result = new List<StoredRecord>();
            foreach (StoredRecord entry in records.Values)
                if (!kind.HasValue || entry.Record.Kind == kind.Value) result.Add(entry);
            result.Sort(sort == RecordSort.AcquiredNewest ? CompareNewest : CompareCatalog);
            return result.AsReadOnly();
        }

        private static int CompareCatalog(StoredRecord left, StoredRecord right)
        {
            int kind = left.Record.Kind.CompareTo(right.Record.Kind);
            if (kind != 0) return kind;
            int order = left.Record.SortOrder.CompareTo(right.Record.SortOrder);
            return order != 0 ? order : string.CompareOrdinal(left.Record.Id, right.Record.Id);
        }

        private static int CompareNewest(StoredRecord left, StoredRecord right) => right.AcquiredSequence.CompareTo(left.AcquiredSequence);
    }

    public enum RecordSort
    {
        Catalog,
        AcquiredNewest
    }
}
