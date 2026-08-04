using System;
using System.Collections.Generic;

namespace SmallWorld.Inventory.Stage8
{
    public interface IIntegerStateReader
    {
        int Get(string key);
    }

    public interface IItemUseCondition
    {
        bool IsMet(IInventoryReader journal, IIntegerStateReader state);
    }

    public sealed class DelegateIntegerStateReader : IIntegerStateReader
    {
        private readonly Func<string, int> reader;

        public DelegateIntegerStateReader(Func<string, int> reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public int Get(string key) => reader(key);
    }

    public sealed class RequiredRecordCondition : IItemUseCondition
    {
        public RequiredRecordCondition(string recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId)) throw new ArgumentException("Record id cannot be empty.", nameof(recordId));
            RecordId = recordId;
        }

        public string RecordId { get; }
        public bool IsMet(IInventoryReader journal, IIntegerStateReader state) => journal != null && journal.Contains(RecordId);
    }

    public sealed class StateThresholdCondition : IItemUseCondition
    {
        public StateThresholdCondition(string key, int minimum)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("State key cannot be empty.", nameof(key));
            Key = key;
            Minimum = minimum;
        }

        public string Key { get; }
        public int Minimum { get; }
        public bool IsMet(IInventoryReader journal, IIntegerStateReader state) => state != null && state.Get(Key) >= Minimum;
    }

    public sealed class ItemUseRule
    {
        private readonly IReadOnlyList<IItemUseCondition> conditions;

        public ItemUseRule(string itemId, params IItemUseCondition[] conditions)
        {
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
            ItemId = itemId;
            IItemUseCondition[] snapshot = conditions == null
                ? Array.Empty<IItemUseCondition>()
                : (IItemUseCondition[])conditions.Clone();
            this.conditions = Array.AsReadOnly(snapshot);
        }

        public string ItemId { get; }
        public IReadOnlyList<IItemUseCondition> Conditions => conditions;

        public bool CanUse(IInventoryReader journal, IIntegerStateReader state = null)
        {
            if (journal == null || !journal.Contains(ItemId)) return false;
            for (int i = 0; i < conditions.Count; i++)
                if (conditions[i] == null || !conditions[i].IsMet(journal, state)) return false;
            return true;
        }
    }
}
