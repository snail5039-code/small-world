using System;
using System.Collections.Generic;

namespace SmallWorld.Puzzle.Stage9
{
    public sealed class PuzzleDefinition
    {
        public PuzzleDefinition(
            string id,
            int stepCount = 1,
            IEnumerable<HintRule> hintRules = null,
            IEnumerable<SpatialChangeCommand> completionChanges = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Puzzle id cannot be empty.", nameof(id));
            if (stepCount < 1) throw new ArgumentOutOfRangeException(nameof(stepCount));
            Id = id;
            StepCount = stepCount;
            HintRules = CopyAndValidate(hintRules);
            CompletionChanges = Copy(completionChanges);
        }

        public string Id { get; }
        public int StepCount { get; }
        public IReadOnlyList<HintRule> HintRules { get; }
        public IReadOnlyList<SpatialChangeCommand> CompletionChanges { get; }

        private static IReadOnlyList<HintRule> CopyAndValidate(IEnumerable<HintRule> source)
        {
            var result = source == null ? new List<HintRule>() : new List<HintRule>(source);
            result.Sort((left, right) => left.IncorrectAttemptsRequired.CompareTo(right.IncorrectAttemptsRequired));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (HintRule rule in result)
            {
                if (rule == null) throw new ArgumentException("Hint rules cannot contain null.", nameof(source));
                if (!ids.Add(rule.HintId)) throw new ArgumentException("Hint ids must be unique.", nameof(source));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<SpatialChangeCommand> Copy(IEnumerable<SpatialChangeCommand> source)
        {
            var result = source == null ? new List<SpatialChangeCommand>() : new List<SpatialChangeCommand>(source);
            if (result.Contains(null)) throw new ArgumentException("Spatial changes cannot contain null.", nameof(source));
            return result.AsReadOnly();
        }
    }

    public sealed class HintRule
    {
        public HintRule(string hintId, int incorrectAttemptsRequired)
        {
            if (string.IsNullOrWhiteSpace(hintId)) throw new ArgumentException("Hint id cannot be empty.", nameof(hintId));
            if (incorrectAttemptsRequired < 1) throw new ArgumentOutOfRangeException(nameof(incorrectAttemptsRequired));
            HintId = hintId;
            IncorrectAttemptsRequired = incorrectAttemptsRequired;
        }

        public string HintId { get; }
        public int IncorrectAttemptsRequired { get; }
    }

    /// <summary>A Unity-agnostic instruction interpreted by scene integration code.</summary>
    public sealed class SpatialChangeCommand
    {
        public SpatialChangeCommand(string targetId, string operation, string value = "")
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("Target id cannot be empty.", nameof(targetId));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation cannot be empty.", nameof(operation));
            TargetId = targetId;
            Operation = operation;
            Value = value ?? string.Empty;
        }

        public string TargetId { get; }
        public string Operation { get; }
        public string Value { get; }
    }

    public sealed class PuzzleState
    {
        internal PuzzleState(
            string puzzleId,
            PuzzleStatus status,
            int currentStep,
            int stepCount,
            int incorrectAttempts,
            IReadOnlyList<string> revealedHintIds)
        {
            PuzzleId = puzzleId;
            Status = status;
            CurrentStep = currentStep;
            StepCount = stepCount;
            IncorrectAttempts = incorrectAttempts;
            RevealedHintIds = revealedHintIds;
        }

        public string PuzzleId { get; }
        public PuzzleStatus Status { get; }
        public int CurrentStep { get; }
        public int StepCount { get; }
        public int IncorrectAttempts { get; }
        /// <summary>Hints already unlocked at the current attempt count, suitable for rebuilding UI after restore.</summary>
        public IReadOnlyList<string> RevealedHintIds { get; }
    }
}
