using System;
using System.Collections.Generic;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Character.Stage11
{
    public static class GirlCharacterKeys
    {
        public static string Relationship(string characterId) => Prefix(characterId) + "relationship";
        public static string Mood(string characterId) => Prefix(characterId) + "mood";
        public static string LastAction(string characterId) => Prefix(characterId) + "last-action";
        public static string InteractionCount(string characterId) => Prefix(characterId) + "interaction-count";
        public static string SharedMemory(string characterId) => Prefix(characterId) + "shared-memory";
        public static string DeathCount(string characterId) => Prefix(characterId) + "death-count";
        public static string LastDeathHandling(string characterId) => Prefix(characterId) + "last-death-handling";
        public static string ReactedDeathCount(string characterId) => Prefix(characterId) + "reacted-death-count";

        private static string Prefix(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("Value cannot be empty.", nameof(characterId));
            return "character." + characterId + ".";
        }
    }

    public static class GirlDialogueBinding
    {
        public static void WriteTo(GirlCharacterState source, DialogueState destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            string id = source.CharacterId;
            destination.Set(GirlCharacterKeys.Relationship(id), source.Relationship);
            destination.Set(GirlCharacterKeys.Mood(id), (int)source.Mood);
            destination.Set(GirlCharacterKeys.LastAction(id), (int)source.LastPlayerAction);
            destination.Set(GirlCharacterKeys.InteractionCount(id), source.InteractionCount);
            destination.Set(GirlCharacterKeys.SharedMemory(id), source.SharedPrivateMemory ? 1 : 0);
            destination.Set(GirlCharacterKeys.DeathCount(id), source.DeathCount);
            destination.Set(GirlCharacterKeys.LastDeathHandling(id), (int)source.LastDeathHandling);
            destination.Set(GirlCharacterKeys.ReactedDeathCount(id), source.ReactedDeathCount);
        }

        public static void ReadFrom(DialogueState source, GirlCharacterState destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            string id = destination.CharacterId;
            destination.Restore(source.Get(GirlCharacterKeys.Relationship(id)),
                (GirlMood)source.Get(GirlCharacterKeys.Mood(id)),
                (PlayerAction)source.Get(GirlCharacterKeys.LastAction(id)),
                Math.Max(0, source.Get(GirlCharacterKeys.InteractionCount(id))),
                source.Get(GirlCharacterKeys.SharedMemory(id)) != 0,
                Math.Max(0, source.Get(GirlCharacterKeys.DeathCount(id))),
                (DeathMemoryHandling)source.Get(GirlCharacterKeys.LastDeathHandling(id)),
                Math.Min(Math.Max(0, source.Get(GirlCharacterKeys.ReactedDeathCount(id))),
                    Math.Max(0, source.Get(GirlCharacterKeys.DeathCount(id)))));
        }
    }

    public static class GirlSaveBinding
    {
        public static void Capture(GirlCharacterState source, SaveData destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            RemoveCharacterEntries(destination.Relationships, source.CharacterId);
            Add(destination, GirlCharacterKeys.Relationship(source.CharacterId), source.Relationship);
            Add(destination, GirlCharacterKeys.Mood(source.CharacterId), (int)source.Mood);
            Add(destination, GirlCharacterKeys.LastAction(source.CharacterId), (int)source.LastPlayerAction);
            Add(destination, GirlCharacterKeys.InteractionCount(source.CharacterId), source.InteractionCount);
            Add(destination, GirlCharacterKeys.SharedMemory(source.CharacterId), source.SharedPrivateMemory ? 1 : 0);
            if (source.DeathCount > 0)
            {
                Add(destination, GirlCharacterKeys.DeathCount(source.CharacterId), source.DeathCount);
                Add(destination, GirlCharacterKeys.LastDeathHandling(source.CharacterId), (int)source.LastDeathHandling);
                Add(destination, GirlCharacterKeys.ReactedDeathCount(source.CharacterId), source.ReactedDeathCount);
            }
        }

        public static bool Restore(SaveData source, GirlCharacterState destination)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            int relationship;
            if (!TryGet(source.Relationships, GirlCharacterKeys.Relationship(destination.CharacterId), out relationship))
                return false;
            int mood, action, count, memory, deathCount, deathHandling, reactedDeathCount;
            TryGet(source.Relationships, GirlCharacterKeys.Mood(destination.CharacterId), out mood);
            TryGet(source.Relationships, GirlCharacterKeys.LastAction(destination.CharacterId), out action);
            TryGet(source.Relationships, GirlCharacterKeys.InteractionCount(destination.CharacterId), out count);
            TryGet(source.Relationships, GirlCharacterKeys.SharedMemory(destination.CharacterId), out memory);
            TryGet(source.Relationships, GirlCharacterKeys.DeathCount(destination.CharacterId), out deathCount);
            TryGet(source.Relationships, GirlCharacterKeys.LastDeathHandling(destination.CharacterId), out deathHandling);
            TryGet(source.Relationships, GirlCharacterKeys.ReactedDeathCount(destination.CharacterId), out reactedDeathCount);
            deathCount = Math.Max(0, deathCount);
            destination.Restore(relationship, (GirlMood)mood, (PlayerAction)action, Math.Max(0, count), memory != 0,
                deathCount, (DeathMemoryHandling)deathHandling, Math.Min(Math.Max(0, reactedDeathCount), deathCount));
            return true;
        }

        private static void Add(SaveData data, string key, int value) =>
            data.Relationships.Add(new RelationshipSaveEntry { CharacterId = key, Value = value });

        private static bool TryGet(List<RelationshipSaveEntry> entries, string key, out int value)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                RelationshipSaveEntry entry = entries[i];
                if (entry != null && entry.CharacterId == key)
                {
                    value = entry.Value;
                    return true;
                }
            }
            value = 0;
            return false;
        }

        private static void RemoveCharacterEntries(List<RelationshipSaveEntry> entries, string characterId)
        {
            string prefix = "character." + characterId + ".";
            entries.RemoveAll(entry => entry != null && entry.CharacterId != null &&
                entry.CharacterId.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
