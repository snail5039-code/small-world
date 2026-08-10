using System;
using System.Collections.Generic;
using System.IO;

namespace SmallWorld.Save.Stage10
{
    /// <summary>Deterministic, Unity-independent serializer for the versioned save contract.</summary>
    public sealed class BinarySaveDataSerializer : ISaveDataSerializer
    {
        private const int MaxEntries = 100000;

        public byte[] Serialize(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(data.Version);
                writer.Write(data.SaveId ?? string.Empty);
                writer.Write(data.SavedAtUtcTicks);
                writer.Write(data.CheckpointId ?? string.Empty);
                writer.Write(data.ActiveSceneId ?? string.Empty);
                WriteList(writer, data.Puzzles, (w, x) => { w.Write(x.PuzzleId ?? ""); w.Write(x.Status); w.Write(x.CurrentStep); w.Write(x.IncorrectAttempts); w.Write(x.Snapshot ?? ""); });
                WriteList(writer, data.Relationships, (w, x) => { w.Write(x.CharacterId ?? ""); w.Write(x.Value); });
                WriteList(writer, data.Inventory, (w, x) => { w.Write(x.ItemId ?? ""); w.Write(x.Quantity); w.Write(x.IsUsed); });
                WriteList(writer, data.Memories, (w, x) => { w.Write(x.MemoryId ?? ""); w.Write(x.IsUnlocked); w.Write(x.IsRead); });
                WriteList(writer, data.SceneStates, (w, x) => { w.Write(x.SceneId ?? ""); w.Write(x.StateKey ?? ""); w.Write(x.Value ?? ""); });
                WriteList(writer, data.Extensions, WriteExtension);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public bool TryDeserialize(byte[] bytes, out SaveData data)
        {
            data = null;
            if (bytes == null) return false;
            try
            {
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream))
                {
                    var result = new SaveData
                    {
                        Version = reader.ReadInt32(),
                        SaveId = reader.ReadString(),
                        SavedAtUtcTicks = reader.ReadInt64(),
                        CheckpointId = reader.ReadString(),
                        ActiveSceneId = reader.ReadString(),
                        Puzzles = ReadList(reader, r => new PuzzleSaveEntry { PuzzleId = r.ReadString(), Status = r.ReadInt32(), CurrentStep = r.ReadInt32(), IncorrectAttempts = r.ReadInt32(), Snapshot = r.ReadString() }),
                        Relationships = ReadList(reader, r => new RelationshipSaveEntry { CharacterId = r.ReadString(), Value = r.ReadInt32() }),
                        Inventory = ReadList(reader, r => new InventorySaveEntry { ItemId = r.ReadString(), Quantity = r.ReadInt32(), IsUsed = r.ReadBoolean() }),
                        Memories = ReadList(reader, r => new MemorySaveEntry { MemoryId = r.ReadString(), IsUnlocked = r.ReadBoolean(), IsRead = r.ReadBoolean() }),
                        SceneStates = ReadList(reader, r => new SceneStateSaveEntry { SceneId = r.ReadString(), StateKey = r.ReadString(), Value = r.ReadString() }),
                        Extensions = ReadList(reader, ReadExtension)
                    };
                    if (stream.Position != stream.Length) return false;
                    data = result;
                    return true;
                }
            }
            catch { return false; }
        }

        private static void WriteExtension(BinaryWriter writer, ExtensionSaveEntry value)
        {
            writer.Write(value.Key ?? string.Empty);
            writer.Write(value.Version);
            writer.Write(value.Payload ?? string.Empty);
        }

        private static ExtensionSaveEntry ReadExtension(BinaryReader reader) => new ExtensionSaveEntry
        {
            Key = reader.ReadString(), Version = reader.ReadInt32(), Payload = reader.ReadString()
        };

        private static void WriteList<T>(BinaryWriter writer, IList<T> values, Action<BinaryWriter, T> write)
        {
            var count = values == null ? 0 : values.Count;
            writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                if (values[i] == null) throw new InvalidDataException("Save lists cannot contain null entries.");
                write(writer, values[i]);
            }
        }

        private static List<T> ReadList<T>(BinaryReader reader, Func<BinaryReader, T> read)
        {
            var count = reader.ReadInt32();
            if (count < 0 || count > MaxEntries) throw new InvalidDataException("Invalid save entry count.");
            var result = new List<T>(count);
            for (var i = 0; i < count; i++) result.Add(read(reader));
            return result;
        }
    }
}
