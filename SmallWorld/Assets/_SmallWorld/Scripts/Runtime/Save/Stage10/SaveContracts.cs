using System;

namespace SmallWorld.Save.Stage10
{
    public enum SaveSlotKind { Auto, Manual }

    public struct SaveSlot : IEquatable<SaveSlot>
    {
        public SaveSlot(SaveSlotKind kind, int index)
        {
            if (kind == SaveSlotKind.Auto && (index < 0 || index > 1))
                throw new ArgumentOutOfRangeException(nameof(index), "Auto save index must be 0 or 1.");
            if (kind == SaveSlotKind.Manual && (index < 0 || index > 2))
                throw new ArgumentOutOfRangeException(nameof(index), "Manual save index must be 0, 1, or 2.");
            Kind = kind;
            Index = index;
        }

        public SaveSlotKind Kind { get; }
        public int Index { get; }
        public string FileStem => Kind == SaveSlotKind.Auto ? "auto-" + Index : "manual-" + Index;
        public bool Equals(SaveSlot other) => Kind == other.Kind && Index == other.Index;
        public override bool Equals(object obj) => obj is SaveSlot other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Index;
    }

    public enum SaveReadStatus { Success, Missing, Corrupt, UnsupportedVersion, IoFailure }

    public sealed class SaveReadResult
    {
        private SaveReadResult(SaveReadStatus status, SaveData data, string source)
        {
            Status = status;
            Data = data;
            Source = source ?? string.Empty;
        }

        public SaveReadStatus Status { get; }
        public SaveData Data { get; }
        public string Source { get; }
        public bool IsSuccess => Status == SaveReadStatus.Success;
        public static SaveReadResult Success(SaveData data, string source) => new SaveReadResult(SaveReadStatus.Success, data, source);
        public static SaveReadResult Failure(SaveReadStatus status, string source = null) => new SaveReadResult(status, null, source);
    }

    public interface ISaveDataSerializer
    {
        byte[] Serialize(SaveData data);
        bool TryDeserialize(byte[] bytes, out SaveData data);
    }

    public interface ISaveGameStore
    {
        bool Write(SaveSlot slot, SaveData data);
        SaveReadResult Read(SaveSlot slot);
        bool Delete(SaveSlot slot);
        void DeleteAllProgress();
    }

    public interface IGameSaveService
    {
        bool AutoSave(SaveData data);
        bool SaveManual(int slotIndex, SaveData data);
        SaveReadResult LoadLatestAutoSave();
        SaveReadResult LoadManual(int slotIndex);
        SaveData StartNewGame();
    }
}
