using System;
using System.Collections.Generic;

namespace SmallWorld.Puzzle.Stage9
{
    public sealed class PuzzleRuntime : IPuzzleRuntime
    {
        private readonly Dictionary<string, PuzzleDefinition> definitions;
        private readonly Dictionary<string, MutableState> states;
        private readonly IPuzzleCompletionSink completionSink;

        public PuzzleRuntime(IEnumerable<PuzzleDefinition> definitions, IPuzzleCompletionSink completionSink = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            this.definitions = new Dictionary<string, PuzzleDefinition>(StringComparer.Ordinal);
            states = new Dictionary<string, MutableState>(StringComparer.Ordinal);
            this.completionSink = completionSink;
            foreach (PuzzleDefinition definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Definitions cannot contain null.", nameof(definitions));
                if (this.definitions.ContainsKey(definition.Id)) throw new ArgumentException("Puzzle ids must be unique.", nameof(definitions));
                this.definitions.Add(definition.Id, definition);
                states.Add(definition.Id, new MutableState());
            }
        }

        public event Action<PuzzleStateChangedEvent> StateChanged;
        public event Action<HintAvailableEvent> HintAvailable;
        public event Action<SpatialChangeRequestedEvent> SpatialChangeRequested;

        public bool TryGetState(string puzzleId, out PuzzleState state)
        {
            if (!definitions.TryGetValue(puzzleId ?? string.Empty, out PuzzleDefinition definition))
            {
                state = null;
                return false;
            }
            state = states[puzzleId].ToPublic(definition);
            return true;
        }

        public PuzzleActionResult Start(string puzzleId)
        {
            if (!TryFind(puzzleId, out PuzzleDefinition definition, out MutableState state)) return PuzzleActionResult.UnknownPuzzle;
            if (state.Status == PuzzleStatus.Completed) return PuzzleActionResult.AlreadyCompleted;
            if (state.Status == PuzzleStatus.InProgress) return PuzzleActionResult.AlreadyStarted;
            state.Status = PuzzleStatus.InProgress;
            PublishState(definition, state);
            return PuzzleActionResult.Accepted;
        }

        public PuzzleActionResult Submit(string puzzleId, bool isCorrect)
        {
            if (!TryFind(puzzleId, out PuzzleDefinition definition, out MutableState state)) return PuzzleActionResult.UnknownPuzzle;
            if (state.Status == PuzzleStatus.Completed) return PuzzleActionResult.AlreadyCompleted;
            if (state.Status != PuzzleStatus.InProgress) return PuzzleActionResult.NotStarted;
            if (!isCorrect)
            {
                state.IncorrectAttempts++;
                PublishNewHints(definition, state);
                PublishState(definition, state);
                return PuzzleActionResult.Incorrect;
            }

            state.CurrentStep++;
            if (state.CurrentStep < definition.StepCount)
            {
                PublishState(definition, state);
                return PuzzleActionResult.Accepted;
            }

            state.Status = PuzzleStatus.Completed;
            PublishState(definition, state);
            PublishSpatialChanges(definition, false);
            completionSink?.OnPuzzleCompleted(definition.Id);
            return PuzzleActionResult.Accepted;
        }

        public PuzzleSnapshot CaptureSnapshot()
        {
            var entries = new List<PuzzleSnapshotEntry>();
            foreach (KeyValuePair<string, MutableState> pair in states)
                entries.Add(new PuzzleSnapshotEntry(pair.Key, pair.Value.Status, pair.Value.CurrentStep, pair.Value.IncorrectAttempts));
            entries.Sort((left, right) => string.CompareOrdinal(left.PuzzleId, right.PuzzleId));
            return new PuzzleSnapshot(entries);
        }

        public void RestoreSnapshot(PuzzleSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Entries == null) throw new ArgumentException("Snapshot entries cannot be null.", nameof(snapshot));

            // Validate and stage every entry before mutating any live state.
            var encounteredIds = new HashSet<string>(StringComparer.Ordinal);
            var staged = new List<StagedRestore>();
            foreach (PuzzleSnapshotEntry entry in snapshot.Entries)
            {
                if (entry == null) throw new ArgumentException("Snapshot entries cannot contain null.", nameof(snapshot));
                if (string.IsNullOrWhiteSpace(entry.PuzzleId)) throw new ArgumentException("Snapshot puzzle ids cannot be empty.", nameof(snapshot));
                if (!encounteredIds.Add(entry.PuzzleId)) throw new ArgumentException("Snapshot contains duplicate puzzle ids.", nameof(snapshot));
                if (!Enum.IsDefined(typeof(PuzzleStatus), entry.Status)) throw new ArgumentException("Snapshot contains an undefined puzzle status.", nameof(snapshot));
                if (entry.IncorrectAttempts < 0) throw new ArgumentException("Incorrect attempts cannot be negative.", nameof(snapshot));
                if (!definitions.TryGetValue(entry.PuzzleId, out PuzzleDefinition definition)) continue;
                ValidateSnapshotEntry(entry, definition);
                staged.Add(new StagedRestore(definition, entry));
            }

            foreach (StagedRestore item in staged)
            {
                MutableState state = states[item.Definition.Id];
                state.Status = item.Entry.Status;
                state.CurrentStep = item.Entry.CurrentStep;
                state.IncorrectAttempts = item.Entry.IncorrectAttempts;
            }

            // Publish only after the complete state graph has been applied. Completion
            // rewards deliberately remain suppressed during restoration.
            foreach (StagedRestore item in staged)
            {
                MutableState state = states[item.Definition.Id];
                PublishState(item.Definition, state);
                if (state.Status == PuzzleStatus.Completed) PublishSpatialChanges(item.Definition, true);
            }
        }

        private bool TryFind(string puzzleId, out PuzzleDefinition definition, out MutableState state)
        {
            if (!definitions.TryGetValue(puzzleId ?? string.Empty, out definition))
            {
                state = null;
                return false;
            }
            state = states[puzzleId];
            return true;
        }

        private void PublishNewHints(PuzzleDefinition definition, MutableState state)
        {
            foreach (HintRule rule in definition.HintRules)
                if (state.IncorrectAttempts == rule.IncorrectAttemptsRequired)
                    HintAvailable?.Invoke(new HintAvailableEvent(definition.Id, rule.HintId));
        }

        private void PublishState(PuzzleDefinition definition, MutableState state) =>
            StateChanged?.Invoke(new PuzzleStateChangedEvent(state.ToPublic(definition)));

        private void PublishSpatialChanges(PuzzleDefinition definition, bool isRestore)
        {
            foreach (SpatialChangeCommand command in definition.CompletionChanges)
                SpatialChangeRequested?.Invoke(new SpatialChangeRequestedEvent(definition.Id, command, isRestore));
        }

        private static void ValidateSnapshotEntry(PuzzleSnapshotEntry entry, PuzzleDefinition definition)
        {
            if (entry.CurrentStep < 0 || entry.CurrentStep > definition.StepCount) throw new ArgumentException("Snapshot step is out of range.");
            if (entry.Status == PuzzleStatus.NotStarted && entry.CurrentStep != 0) throw new ArgumentException("A not-started puzzle cannot have progress.");
            if (entry.Status == PuzzleStatus.Completed && entry.CurrentStep != definition.StepCount) throw new ArgumentException("A completed puzzle must contain all steps.");
            if (entry.Status != PuzzleStatus.Completed && entry.CurrentStep == definition.StepCount) throw new ArgumentException("All completed steps require completed status.");
        }

        private sealed class MutableState
        {
            public PuzzleStatus Status;
            public int CurrentStep;
            public int IncorrectAttempts;

            public PuzzleState ToPublic(PuzzleDefinition definition)
            {
                var revealedHints = new List<string>();
                foreach (HintRule rule in definition.HintRules)
                    if (IncorrectAttempts >= rule.IncorrectAttemptsRequired) revealedHints.Add(rule.HintId);
                return new PuzzleState(
                    definition.Id,
                    Status,
                    CurrentStep,
                    definition.StepCount,
                    IncorrectAttempts,
                    revealedHints.AsReadOnly());
            }
        }

        private sealed class StagedRestore
        {
            public StagedRestore(PuzzleDefinition definition, PuzzleSnapshotEntry entry)
            {
                Definition = definition;
                Entry = entry;
            }

            public PuzzleDefinition Definition { get; }
            public PuzzleSnapshotEntry Entry { get; }
        }
    }
}
