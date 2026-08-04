using System;
using System.Collections.Generic;

namespace SmallWorld.Dialogue.Stage7
{
    public enum DialogueComparison
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    public sealed class DialogueCondition
    {
        public DialogueCondition(string key, int value, DialogueComparison comparison = DialogueComparison.GreaterOrEqual)
        {
            Key = RequireText(key, nameof(key));
            Value = value;
            Comparison = comparison;
        }

        public string Key { get; }
        public int Value { get; }
        public DialogueComparison Comparison { get; }

        public bool IsMet(IReadOnlyDictionary<string, int> variables)
        {
            int actual;
            variables.TryGetValue(Key, out actual);
            switch (Comparison)
            {
                case DialogueComparison.Equal: return actual == Value;
                case DialogueComparison.NotEqual: return actual != Value;
                case DialogueComparison.Greater: return actual > Value;
                case DialogueComparison.GreaterOrEqual: return actual >= Value;
                case DialogueComparison.Less: return actual < Value;
                case DialogueComparison.LessOrEqual: return actual <= Value;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private static string RequireText(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameter);
            return value;
        }
    }

    public sealed class DialogueEffect
    {
        public DialogueEffect(string key, int amount, bool replace = false)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be empty.", nameof(key));
            Key = key;
            Amount = amount;
            Replace = replace;
        }

        public string Key { get; }
        public int Amount { get; }
        public bool Replace { get; }
    }

    public sealed class DialogueChoice
    {
        public DialogueChoice(string id, string text, string nextNodeId,
            IEnumerable<DialogueCondition> conditions = null, IEnumerable<DialogueEffect> effects = null)
        {
            Id = RequireText(id, nameof(id));
            Text = RequireText(text, nameof(text));
            NextNodeId = nextNodeId ?? string.Empty;
            Conditions = Copy(conditions);
            Effects = Copy(effects);
        }

        public string Id { get; }
        public string Text { get; }
        public string NextNodeId { get; }
        public IReadOnlyList<DialogueCondition> Conditions { get; }
        public IReadOnlyList<DialogueEffect> Effects { get; }

        internal bool IsAvailable(IReadOnlyDictionary<string, int> variables)
        {
            for (int i = 0; i < Conditions.Count; i++)
                if (!Conditions[i].IsMet(variables)) return false;
            return true;
        }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
            Array.AsReadOnly(source == null ? Array.Empty<T>() : new List<T>(source).ToArray());

        private static string RequireText(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value cannot be empty.", parameter);
            return value;
        }
    }

    public sealed class DialogueNode
    {
        public DialogueNode(string id, string speakerName, string text, string nextNodeId = null,
            IEnumerable<DialogueChoice> choices = null, IEnumerable<DialogueCondition> conditions = null,
            IEnumerable<DialogueEffect> effects = null, float autoAdvanceSeconds = 0f, string fallbackNodeId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Value cannot be empty.", nameof(id));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (autoAdvanceSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(autoAdvanceSeconds));
            Id = id;
            SpeakerName = speakerName ?? string.Empty;
            Text = text;
            NextNodeId = nextNodeId ?? string.Empty;
            Choices = Copy(choices);
            Conditions = Copy(conditions);
            Effects = Copy(effects);
            AutoAdvanceSeconds = autoAdvanceSeconds;
            FallbackNodeId = fallbackNodeId ?? string.Empty;
        }

        public string Id { get; }
        public string SpeakerName { get; }
        public string Text { get; }
        public string NextNodeId { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }
        public IReadOnlyList<DialogueCondition> Conditions { get; }
        public IReadOnlyList<DialogueEffect> Effects { get; }
        public float AutoAdvanceSeconds { get; }
        public string FallbackNodeId { get; }

        internal bool IsAvailable(IReadOnlyDictionary<string, int> variables)
        {
            for (int i = 0; i < Conditions.Count; i++)
                if (!Conditions[i].IsMet(variables)) return false;
            return true;
        }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> source) =>
            Array.AsReadOnly(source == null ? Array.Empty<T>() : new List<T>(source).ToArray());
    }

    public sealed class DialogueDefinition
    {
        private readonly Dictionary<string, DialogueNode> nodes;

        public DialogueDefinition(string id, string startNodeId, IEnumerable<DialogueNode> nodes,
            IEnumerable<DialogueCondition> menuConditions = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Value cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(startNodeId)) throw new ArgumentException("Value cannot be empty.", nameof(startNodeId));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            Id = id;
            StartNodeId = startNodeId;
            this.nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
            foreach (DialogueNode node in nodes)
            {
                if (node == null) throw new ArgumentException("Nodes cannot contain null.", nameof(nodes));
                if (this.nodes.ContainsKey(node.Id)) throw new ArgumentException("Duplicate node id: " + node.Id, nameof(nodes));
                this.nodes.Add(node.Id, node);
            }
            if (!this.nodes.ContainsKey(StartNodeId)) throw new ArgumentException("Start node does not exist.", nameof(startNodeId));
            MenuConditions = Array.AsReadOnly(menuConditions == null ? Array.Empty<DialogueCondition>() : new List<DialogueCondition>(menuConditions).ToArray());
            ValidateLinks();
        }

        public string Id { get; }
        public string StartNodeId { get; }
        public IReadOnlyList<DialogueCondition> MenuConditions { get; }
        public IReadOnlyDictionary<string, DialogueNode> Nodes => nodes;

        public bool CanShowInMenu(DialogueState state) => ConditionsMet(MenuConditions, state.Variables);

        internal DialogueNode GetNode(string id)
        {
            DialogueNode node;
            if (!nodes.TryGetValue(id, out node)) throw new InvalidOperationException("Dialogue node not found: " + id);
            return node;
        }

        private void ValidateLinks()
        {
            foreach (DialogueNode node in nodes.Values)
            {
                ValidateLink(node.NextNodeId, node.Id);
                ValidateLink(node.FallbackNodeId, node.Id);
                for (int i = 0; i < node.Choices.Count; i++) ValidateLink(node.Choices[i].NextNodeId, node.Id);
            }
        }

        private void ValidateLink(string target, string source)
        {
            if (!string.IsNullOrEmpty(target) && !nodes.ContainsKey(target))
                throw new ArgumentException("Node '" + source + "' links to missing node '" + target + "'.");
        }

        private static bool ConditionsMet(IReadOnlyList<DialogueCondition> conditions, IReadOnlyDictionary<string, int> variables)
        {
            for (int i = 0; i < conditions.Count; i++) if (!conditions[i].IsMet(variables)) return false;
            return true;
        }
    }
}
