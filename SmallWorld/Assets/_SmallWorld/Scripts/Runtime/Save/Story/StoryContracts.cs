using System;
using System.Collections.Generic;
using System.IO;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Save.Story
{
    public enum StoryChapterId { Prologue, Chapter1, Chapter2, Chapter3, Chapter4, Chapter5, Chapter6, FinalChapter }

    [Serializable]
    public sealed class StoryChapterDefinition
    {
        public StoryChapterId Id;
        public string SummaryId = string.Empty;
        public string ObjectiveId = string.Empty;
        public string DialogueId = string.Empty;
        public string PuzzleId = string.Empty;
        public string MemorySpaceId = string.Empty;
    }

    public static class StoryCatalog
    {
        private static readonly StoryChapterDefinition[] Chapters =
        {
            Define(StoryChapterId.Prologue, "already-running", "place-first-furniture", "first-meeting", "first-memory-door", "reality-room"),
            Define(StoryChapterId.Chapter1, "fourth-seat", "restore-family-memory", "seoyun-echo", "four-clocks", "fourth-seat"),
            Define(StoryChapterId.Chapter2, "last-platform", "find-passenger-destination", "shutdown-time", "missing-line", "last-platform"),
            Define(StoryChapterId.Chapter3, "perfect-day", "break-false-memory", "preference-model", "refuse-perfect-loop", "perfect-day"),
            Define(StoryChapterId.Chapter4, "faceless-office", "recover-deletion-record", "developer-record", "identity-access", "faceless-office"),
            Define(StoryChapterId.Chapter5, "graveless-funeral", "disprove-invented-death", "nameless-answer", "reject-false-name", "graveless-funeral"),
            Define(StoryChapterId.Chapter6, "window-city", "stop-reality-replica", "future-admin", "reality-link", "window-city"),
            Define(StoryChapterId.FinalChapter, "white-room", "restore-and-choose", "final-conversation", "living-house", "white-room")
        };

        public static IReadOnlyList<StoryChapterDefinition> All => Chapters;
        public static StoryChapterDefinition Get(StoryChapterId id) => Chapters[(int)id];

        private static StoryChapterDefinition Define(StoryChapterId id, string summary, string objective, string dialogue, string puzzle, string memory) =>
            new StoryChapterDefinition { Id = id, SummaryId = summary, ObjectiveId = objective, DialogueId = dialogue, PuzzleId = puzzle, MemorySpaceId = memory };
    }

    [Serializable]
    public sealed class StoryChapterProgress
    {
        public StoryChapterId Chapter;
        public bool ObjectiveCompleted;
        public bool DialogueCompleted;
        public bool PuzzleCompleted;
        public bool MemorySpaceCompleted;
        public bool IsComplete => ObjectiveCompleted && DialogueCompleted && PuzzleCompleted && MemorySpaceCompleted;
    }

    [Serializable]
    public sealed class StoryChoiceState { public string ChoiceId = string.Empty; public string OutcomeId = string.Empty; }

    [Serializable]
    public sealed class StoryProgress
    {
        public StoryChapterId CurrentChapter = StoryChapterId.Prologue;
        public List<StoryChapterProgress> Chapters = new List<StoryChapterProgress>();
        public List<StoryChoiceState> ImportantChoices = new List<StoryChoiceState>();
        public List<string> ExternalEntityFlags = new List<string>();
        public List<string> ForeshadowFlags = new List<string>();

        public StoryChapterProgress GetChapter(StoryChapterId id)
        {
            StoryChapterProgress found = Chapters.Find(x => x.Chapter == id);
            if (found != null) return found;
            found = new StoryChapterProgress { Chapter = id };
            Chapters.Add(found);
            return found;
        }
    }

    public interface IStoryProgressStore
    {
        StoryProgress Load(SaveData save);
        void Save(SaveData save, StoryProgress progress);
    }

    public sealed class StoryRelationshipService
    {
        public const int Minimum = -100;
        public const int Maximum = 100;

        public int Get(SaveData save, string characterId)
        {
            Validate(save, characterId);
            RelationshipSaveEntry entry = save.Relationships.Find(x => x.CharacterId == characterId);
            return entry == null ? 0 : entry.Value;
        }

        public int Set(SaveData save, string characterId, int value)
        {
            Validate(save, characterId);
            value = Math.Max(Minimum, Math.Min(Maximum, value));
            RelationshipSaveEntry entry = save.Relationships.Find(x => x.CharacterId == characterId);
            if (entry == null) { entry = new RelationshipSaveEntry { CharacterId = characterId }; save.Relationships.Add(entry); }
            entry.Value = value;
            return value;
        }

        private static void Validate(SaveData save, string characterId)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (string.IsNullOrWhiteSpace(characterId)) throw new ArgumentException("Character identifier is required.");
        }
    }

    public sealed class StoryFlowService
    {
        public StoryChapterDefinition Current(StoryProgress progress) => StoryCatalog.Get(Require(progress).CurrentChapter);

        public bool CanEnterFinalChapter(StoryProgress progress)
        {
            progress = Require(progress);
            for (int i = 0; i <= (int)StoryChapterId.Chapter6; i++)
                if (!progress.GetChapter((StoryChapterId)i).IsComplete) return false;
            return true;
        }

        public bool TryAdvance(StoryProgress progress)
        {
            progress = Require(progress);
            StoryChapterId current = progress.CurrentChapter;
            if (current == StoryChapterId.FinalChapter || !progress.GetChapter(current).IsComplete) return false;
            StoryChapterId next = (StoryChapterId)((int)current + 1);
            if (next == StoryChapterId.FinalChapter && !CanEnterFinalChapter(progress)) return false;
            progress.CurrentChapter = next;
            return true;
        }

        public void RecordChoice(StoryProgress progress, string choiceId, string outcomeId)
        {
            if (string.IsNullOrWhiteSpace(choiceId) || string.IsNullOrWhiteSpace(outcomeId)) throw new ArgumentException("Choice identifiers are required.");
            progress = Require(progress);
            StoryChoiceState state = progress.ImportantChoices.Find(x => x.ChoiceId == choiceId);
            if (state == null) progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = choiceId, OutcomeId = outcomeId });
            else state.OutcomeId = outcomeId;
        }

        public void SetFlag(StoryProgress progress, string flagId, bool externalEntity)
        {
            if (string.IsNullOrWhiteSpace(flagId)) throw new ArgumentException("Flag identifier is required.");
            List<string> flags = externalEntity ? Require(progress).ExternalEntityFlags : Require(progress).ForeshadowFlags;
            if (!flags.Contains(flagId)) flags.Add(flagId);
        }

        private static StoryProgress Require(StoryProgress progress) => progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public sealed class SaveDataStoryProgressStore : IStoryProgressStore
    {
        public const string ExtensionKey = "story-progress";
        public const int ExtensionVersion = 1;

        public StoryProgress Load(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            ExtensionSaveEntry entry = save.Extensions.Find(x => x.Key == ExtensionKey);
            if (entry == null) return new StoryProgress();
            if (entry.Version != ExtensionVersion || !TryDecode(entry.Payload, out StoryProgress progress)) return new StoryProgress();
            return progress;
        }

        public void Save(SaveData save, StoryProgress progress)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            ExtensionSaveEntry entry = save.Extensions.Find(x => x.Key == ExtensionKey);
            if (entry == null) { entry = new ExtensionSaveEntry { Key = ExtensionKey }; save.Extensions.Add(entry); }
            entry.Version = ExtensionVersion;
            entry.Payload = Encode(progress);
        }

        private static string Encode(StoryProgress value)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int)value.CurrentChapter);
                WriteList(writer, value.Chapters, (w, x) => { w.Write((int)x.Chapter); w.Write(x.ObjectiveCompleted); w.Write(x.DialogueCompleted); w.Write(x.PuzzleCompleted); w.Write(x.MemorySpaceCompleted); });
                WriteList(writer, value.ImportantChoices, (w, x) => { w.Write(x.ChoiceId ?? ""); w.Write(x.OutcomeId ?? ""); });
                WriteList(writer, value.ExternalEntityFlags, (w, x) => w.Write(x ?? ""));
                WriteList(writer, value.ForeshadowFlags, (w, x) => w.Write(x ?? ""));
                writer.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private static bool TryDecode(string payload, out StoryProgress value)
        {
            value = null;
            try
            {
                byte[] bytes = Convert.FromBase64String(payload ?? "");
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream))
                {
                    int chapter = reader.ReadInt32();
                    if (chapter < 0 || chapter > (int)StoryChapterId.FinalChapter) return false;
                    var result = new StoryProgress { CurrentChapter = (StoryChapterId)chapter };
                    result.Chapters = ReadList(reader, r => new StoryChapterProgress { Chapter = (StoryChapterId)r.ReadInt32(), ObjectiveCompleted = r.ReadBoolean(), DialogueCompleted = r.ReadBoolean(), PuzzleCompleted = r.ReadBoolean(), MemorySpaceCompleted = r.ReadBoolean() });
                    result.ImportantChoices = ReadList(reader, r => new StoryChoiceState { ChoiceId = r.ReadString(), OutcomeId = r.ReadString() });
                    result.ExternalEntityFlags = ReadList(reader, r => r.ReadString());
                    result.ForeshadowFlags = ReadList(reader, r => r.ReadString());
                    if (stream.Position != stream.Length) return false;
                    value = result;
                    return true;
                }
            }
            catch { return false; }
        }

        private static void WriteList<T>(BinaryWriter writer, IList<T> values, Action<BinaryWriter, T> write)
        {
            int count = values == null ? 0 : values.Count;
            writer.Write(count);
            for (int i = 0; i < count; i++) write(writer, values[i]);
        }

        private static List<T> ReadList<T>(BinaryReader reader, Func<BinaryReader, T> read)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 10000) throw new InvalidDataException();
            var values = new List<T>(count);
            for (int i = 0; i < count; i++) values.Add(read(reader));
            return values;
        }
    }
}

