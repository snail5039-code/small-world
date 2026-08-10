using System;

namespace SmallWorld.Save.Stage10
{
    public sealed class GameSaveService : IGameSaveService
    {
        private readonly ISaveGameStore store;
        private readonly Func<DateTime> utcNow;
        private int nextAutoSlot;

        public GameSaveService(ISaveGameStore store, Func<DateTime> utcNow = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            nextAutoSlot = FindNextAutoSlot();
        }

        public bool AutoSave(SaveData data)
        {
            Prepare(data);
            var slot = new SaveSlot(SaveSlotKind.Auto, nextAutoSlot);
            if (!store.Write(slot, data)) return false;
            nextAutoSlot = 1 - nextAutoSlot;
            return true;
        }

        public bool SaveManual(int slotIndex, SaveData data)
        {
            Prepare(data);
            return store.Write(new SaveSlot(SaveSlotKind.Manual, slotIndex), data);
        }

        public SaveReadResult LoadLatestAutoSave()
        {
            var first = store.Read(new SaveSlot(SaveSlotKind.Auto, 0));
            var second = store.Read(new SaveSlot(SaveSlotKind.Auto, 1));
            if (first.IsSuccess && second.IsSuccess)
                return first.Data.SavedAtUtcTicks >= second.Data.SavedAtUtcTicks ? first : second;
            if (first.IsSuccess) return first;
            if (second.IsSuccess) return second;
            return first.Status == SaveReadStatus.Missing ? second : first;
        }

        public SaveReadResult LoadManual(int slotIndex) => store.Read(new SaveSlot(SaveSlotKind.Manual, slotIndex));

        public SaveData StartNewGame()
        {
            store.DeleteAllProgress();
            nextAutoSlot = 0;
            return SaveData.CreateNew();
        }

        private int FindNextAutoSlot()
        {
            var first = store.Read(new SaveSlot(SaveSlotKind.Auto, 0));
            var second = store.Read(new SaveSlot(SaveSlotKind.Auto, 1));
            if (!first.IsSuccess) return 0;
            if (!second.IsSuccess) return 1;
            return first.Data.SavedAtUtcTicks <= second.Data.SavedAtUtcTicks ? 0 : 1;
        }

        private void Prepare(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(data.SaveId)) data.SaveId = Guid.NewGuid().ToString("N");
            data.Version = SaveData.CurrentVersion;
            data.SavedAtUtcTicks = utcNow().ToUniversalTime().Ticks;
        }
    }
}
