using System;
using System.Collections.Generic;

namespace SmallWorld.Dialogue.Stage7
{
    public sealed class DialogueState
    {
        private readonly Dictionary<string, int> variables = new Dictionary<string, int>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, int> Variables => variables;

        public int Get(string key)
        {
            int value;
            return variables.TryGetValue(key, out value) ? value : 0;
        }

        public void Set(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be empty.", nameof(key));
            variables[key] = value;
        }

        public void Add(string key, int amount) => Set(key, Get(key) + amount);

        public void Clear() => variables.Clear();

        internal void Apply(IReadOnlyList<DialogueEffect> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                DialogueEffect effect = effects[i];
                if (effect.Replace) Set(effect.Key, effect.Amount);
                else Add(effect.Key, effect.Amount);
            }
        }
    }

    public sealed class DialogueHistoryEntry
    {
        public DialogueHistoryEntry(string nodeId, string speakerName, string text, string choiceId = null)
        {
            NodeId = nodeId;
            SpeakerName = speakerName;
            Text = text;
            ChoiceId = choiceId ?? string.Empty;
        }

        public string NodeId { get; }
        public string SpeakerName { get; }
        public string Text { get; }
        public string ChoiceId { get; }
    }

    public sealed class DialogueFrame
    {
        public DialogueFrame(DialogueNode node, IReadOnlyList<DialogueChoice> choices)
        {
            NodeId = node.Id;
            SpeakerName = node.SpeakerName;
            Text = node.Text;
            Choices = choices;
            AutoAdvanceSeconds = node.AutoAdvanceSeconds;
        }

        public string NodeId { get; }
        public string SpeakerName { get; }
        public string Text { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }
        public float AutoAdvanceSeconds { get; }
    }
}
