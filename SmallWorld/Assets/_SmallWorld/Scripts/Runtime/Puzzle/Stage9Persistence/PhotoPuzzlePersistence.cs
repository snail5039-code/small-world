using System;
using SmallWorld.Puzzle.Stage9;
using UnityEngine;

namespace SmallWorld.Puzzle.Stage9.Persistence
{
    public static class PhotoPuzzleSaveContract
    {
        public const string Key = "smallworld.stage9.photo-puzzle.v1";
        public static void Clear(IPhotoPuzzleStorage storage) =>
            new PhotoPuzzlePersistence(Key, storage).Clear();
    }

    /// <summary>Replaceable persistence boundary for the Stage 9 photo puzzle.</summary>
    public interface IPhotoPuzzleStorage
    {
        bool TryRead(string key, out string value);
        void Write(string key, string value);
        void Delete(string key);
        void Quarantine(string key, string value);
    }

    public interface IPhotoPuzzleSnapshotSerializer
    {
        string Serialize(PuzzleSnapshot snapshot);
        bool TryDeserialize(string value, out PuzzleSnapshot snapshot);
    }

    public sealed class PlayerPrefsPhotoPuzzleStorage : IPhotoPuzzleStorage
    {
        public bool TryRead(string key, out string value)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                value = null;
                return false;
            }
            value = PlayerPrefs.GetString(key);
            return true;
        }

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public void Quarantine(string key, string value)
        {
            PlayerPrefs.SetString(key + ".corrupt", value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public sealed class JsonPhotoPuzzleSnapshotSerializer : IPhotoPuzzleSnapshotSerializer
    {
        public string Serialize(PuzzleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return JsonUtility.ToJson(snapshot);
        }

        public bool TryDeserialize(string value, out PuzzleSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            try
            {
                snapshot = JsonUtility.FromJson<PuzzleSnapshot>(value);
                if (snapshot == null || snapshot.Entries == null)
                {
                    snapshot = null;
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                snapshot = null;
                return false;
            }
        }
    }

    public sealed class PhotoPuzzlePersistence
    {
        private readonly string key;
        private readonly IPhotoPuzzleStorage storage;
        private readonly IPhotoPuzzleSnapshotSerializer serializer;

        public PhotoPuzzlePersistence(string key, IPhotoPuzzleStorage storage,
            IPhotoPuzzleSnapshotSerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Persistence key cannot be empty.", nameof(key));
            this.key = key;
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.serializer = serializer ?? new JsonPhotoPuzzleSnapshotSerializer();
        }

        public void Save(PuzzleSnapshot snapshot) => storage.Write(key, serializer.Serialize(snapshot));

        public bool TryRestore(out PuzzleSnapshot snapshot)
        {
            snapshot = null;
            if (!storage.TryRead(key, out string value)) return false;
            if (serializer.TryDeserialize(value, out snapshot)) return true;
            storage.Quarantine(key, value);
            storage.Delete(key);
            snapshot = null;
            return false;
        }

        public void Clear()
        {
            storage.Delete(key);
            storage.Delete(key + ".corrupt");
        }

        public void QuarantineCurrent()
        {
            if (!storage.TryRead(key, out string value)) return;
            storage.Quarantine(key, value);
            storage.Delete(key);
        }
    }
}
