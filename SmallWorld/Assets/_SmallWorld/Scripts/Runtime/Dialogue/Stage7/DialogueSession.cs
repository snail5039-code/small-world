using System;
using System.Collections.Generic;

namespace SmallWorld.Dialogue.Stage7
{
    public sealed class DialogueSession
    {
        private const int MaximumSkipSteps = 4096;
        private readonly DialogueDefinition definition;
        private readonly DialogueState state;
        private readonly List<DialogueHistoryEntry> history = new List<DialogueHistoryEntry>();
        private float elapsed;

        public DialogueSession(DialogueDefinition definition, DialogueState state)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            Enter(definition.StartNodeId);
        }

        public event Action<DialogueFrame> FrameChanged;
        public event Action Completed;
        public bool IsComplete { get; private set; }
        public DialogueFrame Current { get; private set; }
        public IReadOnlyList<DialogueHistoryEntry> History => history.AsReadOnly();
        public DialogueState State => state;

        public void Advance()
        {
            EnsureActive();
            if (Current.Choices.Count > 0) throw new InvalidOperationException("A choice must be selected before advancing.");
            DialogueNode node = definition.GetNode(Current.NodeId);
            MoveTo(node.NextNodeId);
        }

        public void SelectChoice(string choiceId)
        {
            EnsureActive();
            DialogueChoice selected = null;
            for (int i = 0; i < Current.Choices.Count; i++)
                if (Current.Choices[i].Id == choiceId) { selected = Current.Choices[i]; break; }
            if (selected == null) throw new ArgumentException("Choice is not available: " + choiceId, nameof(choiceId));
            state.Apply(selected.Effects);
            history.Add(new DialogueHistoryEntry(Current.NodeId, Current.SpeakerName, selected.Text, selected.Id));
            MoveTo(selected.NextNodeId);
        }

        public bool Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (IsComplete || Current.Choices.Count > 0 || Current.AutoAdvanceSeconds <= 0f) return false;
            elapsed += deltaSeconds;
            if (elapsed < Current.AutoAdvanceSeconds) return false;
            Advance();
            return true;
        }

        public void Skip()
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            int steps = 0;
            while (!IsComplete)
            {
                if (Current.Choices.Count > 0) return;
                if (!visited.Add(Current.NodeId))
                    throw new InvalidOperationException("Dialogue skip cycle detected at node: " + Current.NodeId);
                if (++steps > MaximumSkipSteps)
                    throw new InvalidOperationException("Dialogue skip exceeded the maximum step count of " + MaximumSkipSteps + ".");
                Advance();
            }
        }

        private void MoveTo(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                IsComplete = true;
                Current = null;
                Completed?.Invoke();
                return;
            }
            Enter(nodeId);
        }

        private void Enter(string nodeId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                if (!visited.Add(nodeId)) throw new InvalidOperationException("Conditional fallback cycle detected at: " + nodeId);
                DialogueNode node = definition.GetNode(nodeId);
                if (node.IsAvailable(state.Variables))
                {
                    state.Apply(node.Effects);
                    var choices = new List<DialogueChoice>();
                    for (int i = 0; i < node.Choices.Count; i++)
                        if (node.Choices[i].IsAvailable(state.Variables)) choices.Add(node.Choices[i]);
                    Current = new DialogueFrame(node, choices.AsReadOnly());
                    history.Add(new DialogueHistoryEntry(node.Id, node.SpeakerName, node.Text));
                    elapsed = 0f;
                    FrameChanged?.Invoke(Current);
                    return;
                }
                if (string.IsNullOrEmpty(node.FallbackNodeId))
                {
                    IsComplete = true;
                    Current = null;
                    Completed?.Invoke();
                    return;
                }
                nodeId = node.FallbackNodeId;
            }
        }

        private void EnsureActive()
        {
            if (IsComplete) throw new InvalidOperationException("Dialogue has already completed.");
        }
    }
}
