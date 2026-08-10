using System;
using System.Collections.Generic;

namespace SmallWorld.Puzzle.Stage9
{
    public enum PuzzleStatus
    {
        NotStarted,
        InProgress,
        Completed
    }

    public enum PuzzleActionResult
    {
        Accepted,
        UnknownPuzzle,
        AlreadyStarted,
        NotStarted,
        AlreadyCompleted,
        Incorrect
    }

    public interface IPuzzleRuntime
    {
        event Action<PuzzleStateChangedEvent> StateChanged;
        event Action<HintAvailableEvent> HintAvailable;
        event Action<SpatialChangeRequestedEvent> SpatialChangeRequested;

        bool TryGetState(string puzzleId, out PuzzleState state);
        PuzzleActionResult Start(string puzzleId);
        PuzzleActionResult Submit(string puzzleId, bool isCorrect);
        PuzzleSnapshot CaptureSnapshot();
        void RestoreSnapshot(PuzzleSnapshot snapshot);
    }

    /// <summary>Implemented by inventory, journal, or progression code that rewards puzzle completion.</summary>
    public interface IPuzzleCompletionSink
    {
        void OnPuzzleCompleted(string puzzleId);
    }

    public sealed class DelegatePuzzleCompletionSink : IPuzzleCompletionSink
    {
        private readonly Action<string> callback;

        public DelegatePuzzleCompletionSink(Action<string> callback)
        {
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void OnPuzzleCompleted(string puzzleId) => callback(puzzleId);
    }

    public sealed class PuzzleStateChangedEvent
    {
        public PuzzleStateChangedEvent(PuzzleState state) { State = state; }
        public PuzzleState State { get; }
    }

    public sealed class HintAvailableEvent
    {
        public HintAvailableEvent(string puzzleId, string hintId)
        {
            PuzzleId = puzzleId;
            HintId = hintId;
        }

        public string PuzzleId { get; }
        public string HintId { get; }
    }

    public sealed class SpatialChangeRequestedEvent
    {
        public SpatialChangeRequestedEvent(string puzzleId, SpatialChangeCommand command) : this(puzzleId, command, false) { }

        public SpatialChangeRequestedEvent(string puzzleId, SpatialChangeCommand command, bool isRestore)
        {
            PuzzleId = puzzleId;
            Command = command;
            IsRestore = isRestore;
        }

        public string PuzzleId { get; }
        public SpatialChangeCommand Command { get; }
        /// <summary>True when a saved world state is being reapplied without granting completion rewards.</summary>
        public bool IsRestore { get; }
    }

    [Serializable]
    public sealed class PuzzleSnapshot
    {
        public PuzzleSnapshot() { Entries = new List<PuzzleSnapshotEntry>(); }

        public PuzzleSnapshot(IEnumerable<PuzzleSnapshotEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            Entries = new List<PuzzleSnapshotEntry>(entries);
        }

        // Public fields and parameterless constructors keep this DTO compatible with
        // Unity JsonUtility as well as a future Stage 10 save serializer.
        public List<PuzzleSnapshotEntry> Entries;
    }

    [Serializable]
    public sealed class PuzzleSnapshotEntry
    {
        public PuzzleSnapshotEntry() { PuzzleId = string.Empty; }

        public PuzzleSnapshotEntry(string puzzleId, PuzzleStatus status, int currentStep, int incorrectAttempts)
        {
            if (string.IsNullOrWhiteSpace(puzzleId)) throw new ArgumentException("Puzzle id cannot be empty.", nameof(puzzleId));
            PuzzleId = puzzleId;
            Status = status;
            CurrentStep = currentStep;
            IncorrectAttempts = incorrectAttempts;
        }

        public string PuzzleId;
        public PuzzleStatus Status;
        public int CurrentStep;
        public int IncorrectAttempts;
    }
}
