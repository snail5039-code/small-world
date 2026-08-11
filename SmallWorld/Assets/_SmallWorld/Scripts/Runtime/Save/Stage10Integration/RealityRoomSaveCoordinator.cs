using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Player;
using SmallWorld.Puzzle.Stage9;
using SmallWorld.Puzzle.Stage9Integration;
using SmallWorld.Puzzle.Stage9.Persistence;
using SmallWorld.Save.Stage10;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Save.Stage10.Integration
{
    public static class Stage10SaveRuntime
    {
        private static IGameSaveService service;
        public static SaveData PendingLoad { get; private set; }

        public static IGameSaveService Service => service ?? (service = CreateDefaultService());

        public static void Configure(IGameSaveService value)
        {
            service = value ?? throw new ArgumentNullException(nameof(value));
            PendingLoad = null;
        }

        public static void QueueLoad(SaveData data) => PendingLoad = data;
        public static SaveData ConsumePendingLoad()
        {
            SaveData value = PendingLoad;
            PendingLoad = null;
            return value;
        }

        public static SaveReadResult FindLatest()
        {
            SaveReadResult latest = Service.LoadLatestAutoSave();
            for (int i = 0; i < 3; i++)
            {
                SaveReadResult candidate = Service.LoadManual(i);
                if (candidate.IsSuccess && (!latest.IsSuccess || candidate.Data.SavedAtUtcTicks > latest.Data.SavedAtUtcTicks))
                    latest = candidate;
            }
            return latest;
        }

        private static IGameSaveService CreateDefaultService()
        {
            string directory = Path.Combine(Application.persistentDataPath, "progress");
            return new GameSaveService(new AtomicFileSaveStore(directory, new BinarySaveDataSerializer()));
        }
    }

    [Serializable]
    public sealed class RecordMetadata
    {
        public string Id;
        public int Kind;
        public string Title;
        public string Description;
        public int SortOrder;
    }

    public sealed class RealityRoomSaveCoordinator : MonoBehaviour
    {
        public const string WhiteChairCheckpoint = "reality.white-chair";
        private const string PlayerPositionKey = "player.position";
        private const string PlayerRotationKey = "player.rotation";
        private const string RecordMetadataPrefix = "inventory.record.";

        [SerializeField] private FirstPersonPlayerController player;
        [SerializeField] private Stage7DialogueView dialogue;
        [SerializeField] private Stage8RecordView records;
        [SerializeField] private PhotoPuzzleView photoPuzzle;
        [SerializeField] private Stage10ManualSavePanel manualPanel;
        [SerializeField] private string sceneId = "RealityRoom";
        private SaveData current;

        public SaveData Current => current;

        public void Configure(FirstPersonPlayerController playerController, Stage7DialogueView dialogueView,
            Stage8RecordView recordView, PhotoPuzzleView puzzleView, Stage10ManualSavePanel panel)
        {
            player = playerController;
            dialogue = dialogueView;
            records = recordView;
            photoPuzzle = puzzleView;
            manualPanel = panel;
            manualPanel?.Configure(this);
            manualPanel?.Configure(player);
        }

        private void Start()
        {
            manualPanel?.Configure(player);
            current = Stage10SaveRuntime.ConsumePendingLoad() ?? SaveData.CreateNew();
            if (HasProgress(current)) Restore(current);
            else MigrateLegacyPhotoPuzzle();
        }

        public bool ReachWhiteChair()
        {
            bool saved = AutoSave(WhiteChairCheckpoint);
            manualPanel?.Open();
            return saved;
        }

        public bool AutoSave(string checkpointId = "reality.autosave") =>
            Stage10SaveRuntime.Service.AutoSave(Capture(checkpointId));

        public bool SaveManual(int slotIndex) =>
            Stage10SaveRuntime.Service.SaveManual(slotIndex, Capture(WhiteChairCheckpoint));

        public bool LoadManual(int slotIndex)
        {
            SaveReadResult result = Stage10SaveRuntime.Service.LoadManual(slotIndex);
            if (!result.IsSuccess) return false;
            current = result.Data;
            Restore(current);
            return true;
        }

        public SaveData Capture(string checkpointId)
        {
            SaveData data = current ?? SaveData.CreateNew();
            data.CheckpointId = checkpointId ?? string.Empty;
            data.ActiveSceneId = sceneId;
            data.Puzzles.Clear();
            data.Relationships.Clear();
            data.Inventory.Clear();
            data.SceneStates.Clear();
            data.Extensions.RemoveAll(entry => entry != null && entry.Key != null && entry.Key.StartsWith(RecordMetadataPrefix, StringComparison.Ordinal));
            CapturePuzzle(data);
            CaptureRelationships(data);
            CaptureInventory(data);
            CaptureScene(data);
            current = data;
            return data;
        }

        public void Restore(SaveData data)
        {
            if (data == null) return;
            RestorePuzzle(data);
            RestoreRelationships(data);
            RestoreInventory(data);
            RestoreScene(data);
        }

        private void CapturePuzzle(SaveData data)
        {
            if (photoPuzzle == null) return;
            PuzzleSnapshot snapshot = photoPuzzle.CaptureSnapshot();
            for (int i = 0; i < snapshot.Entries.Count; i++)
            {
                PuzzleSnapshotEntry entry = snapshot.Entries[i];
                data.Puzzles.Add(new PuzzleSaveEntry { PuzzleId = entry.PuzzleId, Status = (int)entry.Status,
                    CurrentStep = entry.CurrentStep, IncorrectAttempts = entry.IncorrectAttempts });
            }
        }

        private void RestorePuzzle(SaveData data)
        {
            if (photoPuzzle == null || data.Puzzles.Count == 0) return;
            var entries = new List<PuzzleSnapshotEntry>();
            for (int i = 0; i < data.Puzzles.Count; i++)
            {
                PuzzleSaveEntry entry = data.Puzzles[i];
                entries.Add(new PuzzleSnapshotEntry(entry.PuzzleId, (PuzzleStatus)entry.Status, entry.CurrentStep, entry.IncorrectAttempts));
            }
            photoPuzzle.RestoreSnapshot(new PuzzleSnapshot(entries));
        }

        private void CaptureRelationships(SaveData data)
        {
            if (dialogue == null) return;
            foreach (KeyValuePair<string, int> pair in dialogue.State.Variables)
                data.Relationships.Add(new RelationshipSaveEntry { CharacterId = pair.Key, Value = pair.Value });
        }

        private void RestoreRelationships(SaveData data)
        {
            if (dialogue == null) return;
            dialogue.State.Clear();
            for (int i = 0; i < data.Relationships.Count; i++)
                dialogue.State.Set(data.Relationships[i].CharacterId, data.Relationships[i].Value);
        }

        private void CaptureInventory(SaveData data)
        {
            if (records == null) return;
            IReadOnlyList<StoredRecord> all = records.CaptureRecords();
            for (int i = all.Count - 1; i >= 0; i--)
            {
                InventoryRecord record = all[i].Record;
                data.Inventory.Add(new InventorySaveEntry { ItemId = record.Id, Quantity = 1 });
                var metadata = new RecordMetadata { Id = record.Id, Kind = (int)record.Kind, Title = record.Title,
                    Description = record.Description, SortOrder = record.SortOrder };
                data.Extensions.Add(new ExtensionSaveEntry { Key = RecordMetadataPrefix + record.Id, Version = 1,
                    Payload = JsonUtility.ToJson(metadata) });
            }
        }

        private void RestoreInventory(SaveData data)
        {
            if (records == null) return;
            var restored = new List<InventoryRecord>();
            for (int i = 0; i < data.Inventory.Count; i++)
            {
                InventorySaveEntry item = data.Inventory[i];
                ExtensionSaveEntry extension = data.Extensions.Find(value => value.Key == RecordMetadataPrefix + item.ItemId);
                RecordMetadata metadata = extension == null ? null : JsonUtility.FromJson<RecordMetadata>(extension.Payload);
                restored.Add(metadata == null
                    ? new InventoryRecord(item.ItemId, RecordKind.KeyItem, item.ItemId)
                    : new InventoryRecord(metadata.Id, (RecordKind)metadata.Kind, metadata.Title, metadata.Description, metadata.SortOrder));
            }
            records.RestoreRecords(restored);
        }

        private void CaptureScene(SaveData data)
        {
            if (player != null)
            {
                AddScene(data, PlayerPositionKey, Encode(player.transform.position));
                AddScene(data, PlayerRotationKey, Encode(player.transform.eulerAngles));
            }
            DoorInteractable[] doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++) AddScene(data, "door." + doors[i].name, doors[i].IsOpen ? "1" : "0");
            ToggleUseInteractable[] toggles = FindObjectsByType<ToggleUseInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < toggles.Length; i++) AddScene(data, "toggle." + toggles[i].name, toggles[i].IsUsed ? "1" : "0");
        }

        private void RestoreScene(SaveData data)
        {
            if (player != null)
            {
                SceneStateSaveEntry position = FindScene(data, PlayerPositionKey);
                SceneStateSaveEntry rotation = FindScene(data, PlayerRotationKey);
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                if (position != null) player.transform.position = Decode(position.Value);
                if (rotation != null) player.transform.eulerAngles = Decode(rotation.Value);
                if (controller != null) controller.enabled = true;
            }
            DoorInteractable[] doors = FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                SceneStateSaveEntry entry = FindScene(data, "door." + doors[i].name);
                if (entry != null) doors[i].RestoreOpenState(entry.Value == "1");
            }
            ToggleUseInteractable[] toggles = FindObjectsByType<ToggleUseInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < toggles.Length; i++)
            {
                SceneStateSaveEntry entry = FindScene(data, "toggle." + toggles[i].name);
                if (entry != null) toggles[i].RestoreUsedState(entry.Value == "1");
            }
        }

        private void MigrateLegacyPhotoPuzzle()
        {
            if (photoPuzzle == null) return;
            var storage = new PlayerPrefsPhotoPuzzleStorage();
            var legacy = new PhotoPuzzlePersistence(PhotoPuzzleView.PersistenceKey, storage);
            PuzzleSnapshot snapshot;
            if (!legacy.TryRestore(out snapshot)) return;
            photoPuzzle.RestoreSnapshot(snapshot);
            AutoSave("legacy.stage9-migration");
            legacy.Clear();
        }

        private void AddScene(SaveData data, string key, string value) => data.SceneStates.Add(new SceneStateSaveEntry { SceneId = sceneId, StateKey = key, Value = value });
        private SceneStateSaveEntry FindScene(SaveData data, string key) => data.SceneStates.Find(entry => entry.SceneId == sceneId && entry.StateKey == key);
        private static bool HasProgress(SaveData data) => data.Puzzles.Count + data.Relationships.Count + data.Inventory.Count + data.SceneStates.Count > 0;
        private static string Encode(Vector3 value) => value.x.ToString("R", CultureInfo.InvariantCulture) + "," +
            value.y.ToString("R", CultureInfo.InvariantCulture) + "," + value.z.ToString("R", CultureInfo.InvariantCulture);
        private static Vector3 Decode(string value)
        {
            string[] parts = (value ?? string.Empty).Split(',');
            float x, y, z;
            return parts.Length == 3 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)
                ? new Vector3(x, y, z) : Vector3.zero;
        }
    }

}
