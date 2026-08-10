using System;
using System.Collections.Generic;

namespace SmallWorld.Save.Stage10
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SaveId = string.Empty;
        public long SavedAtUtcTicks;
        public string CheckpointId = string.Empty;
        public string ActiveSceneId = string.Empty;
        public List<PuzzleSaveEntry> Puzzles = new List<PuzzleSaveEntry>();
        public List<RelationshipSaveEntry> Relationships = new List<RelationshipSaveEntry>();
        public List<InventorySaveEntry> Inventory = new List<InventorySaveEntry>();
        public List<MemorySaveEntry> Memories = new List<MemorySaveEntry>();
        public List<SceneStateSaveEntry> SceneStates = new List<SceneStateSaveEntry>();
        public List<ExtensionSaveEntry> Extensions = new List<ExtensionSaveEntry>();

        public static SaveData CreateNew() => new SaveData { SaveId = Guid.NewGuid().ToString("N") };
    }

    [Serializable]
    public sealed class PuzzleSaveEntry
    {
        public string PuzzleId = string.Empty;
        public int Status;
        public int CurrentStep;
        public int IncorrectAttempts;
        public string Snapshot = string.Empty;
    }

    [Serializable]
    public sealed class RelationshipSaveEntry
    {
        public string CharacterId = string.Empty;
        public int Value;
    }

    [Serializable]
    public sealed class InventorySaveEntry
    {
        public string ItemId = string.Empty;
        public int Quantity;
        public bool IsUsed;
    }

    [Serializable]
    public sealed class MemorySaveEntry
    {
        public string MemoryId = string.Empty;
        public bool IsUnlocked;
        public bool IsRead;
    }

    [Serializable]
    public sealed class SceneStateSaveEntry
    {
        public string SceneId = string.Empty;
        public string StateKey = string.Empty;
        public string Value = string.Empty;
    }

    [Serializable]
    public sealed class ExtensionSaveEntry
    {
        public string Key = string.Empty;
        public int Version;
        public string Payload = string.Empty;
    }

    [Serializable]
    public sealed class SaveSettingsData
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public List<ExtensionSaveEntry> Values = new List<ExtensionSaveEntry>();
    }
}
