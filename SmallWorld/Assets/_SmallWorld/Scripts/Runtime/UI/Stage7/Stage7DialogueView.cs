using System;
using System.Collections.Generic;
using System.Text;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI.Stage7
{
    public sealed class Stage7DialogueView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup dialogueGroup;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text relationshipText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Toggle autoAdvanceToggle;
        [SerializeField] private Button historyButton;
        [SerializeField] private CanvasGroup historyGroup;
        [SerializeField] private Text historyText;
        [SerializeField] private Button closeHistoryButton;
        [SerializeField] private Button[] choiceButtons = Array.Empty<Button>();
        [SerializeField] private FirstPersonPlayerController player;

        private readonly DialogueState state = new DialogueState();
        private DialogueSession session;

        public DialogueSession Session => session;
        public DialogueState State => state;
        public string CurrentSpeaker => speakerText != null ? speakerText.text : string.Empty;
        public string CurrentBody => bodyText != null ? bodyText.text : string.Empty;

        public void Configure(CanvasGroup dialogue, Text speaker, Text body, Text relationship,
            Button advance, Button skip, Toggle autoToggle, Button history, CanvasGroup historyPanel,
            Text historyLog, Button closeHistory, Button[] choices, FirstPersonPlayerController playerController)
        {
            dialogueGroup = dialogue;
            speakerText = speaker;
            bodyText = body;
            relationshipText = relationship;
            advanceButton = advance;
            skipButton = skip;
            autoAdvanceToggle = autoToggle;
            historyButton = history;
            historyGroup = historyPanel;
            historyText = historyLog;
            closeHistoryButton = closeHistory;
            choiceButtons = choices ?? Array.Empty<Button>();
            player = playerController;
            BindButtons();
            SetVisible(historyGroup, false);
        }

        private void Awake()
        {
            BindButtons();
            StartDialogue(Stage7DemoDialogue.Create());
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindSession();
        }

        private void Update()
        {
            if (session == null || session.IsComplete || autoAdvanceToggle == null || !autoAdvanceToggle.isOn) return;
            session.Tick(Time.unscaledDeltaTime);
        }

        public void StartDialogue(DialogueDefinition definition)
        {
            if (definition == null || !definition.CanShowInMenu(state)) return;
            UnbindSession();
            session = new DialogueSession(definition, state);
            session.FrameChanged += Render;
            session.Completed += Complete;
            if (player != null) player.enabled = false;
            SetVisible(dialogueGroup, true);
            SetVisible(historyGroup, false);
            Render(session.Current);
        }

        public void Advance()
        {
            if (session == null || session.IsComplete || session.Current.Choices.Count > 0) return;
            session.Advance();
        }

        public void Skip()
        {
            if (session == null || session.IsComplete) return;
            session.Skip();
            if (!session.IsComplete) Render(session.Current);
        }

        public void SelectChoiceAt(int index)
        {
            if (session == null || session.IsComplete || index < 0 || index >= session.Current.Choices.Count) return;
            session.SelectChoice(session.Current.Choices[index].Id);
        }

        public void ShowHistory()
        {
            if (session == null) return;
            var builder = new StringBuilder();
            foreach (DialogueHistoryEntry entry in session.History)
            {
                if (builder.Length > 0) builder.AppendLine().AppendLine();
                builder.Append(string.IsNullOrEmpty(entry.ChoiceId) ? entry.SpeakerName : "나")
                    .Append("  ").Append(entry.Text);
            }
            if (historyText != null) historyText.text = builder.ToString();
            SetVisible(historyGroup, true);
        }

        public void HideHistory() => SetVisible(historyGroup, false);

        private void Render(DialogueFrame frame)
        {
            if (frame == null) return;
            if (speakerText != null) speakerText.text = frame.SpeakerName;
            if (bodyText != null) bodyText.text = frame.Text;
            if (relationshipText != null)
                relationshipText.text = "미라와의 관계  " + FormatSigned(state.Get(Stage7DemoDialogue.RelationshipKey));
            if (advanceButton != null) advanceButton.gameObject.SetActive(frame.Choices.Count == 0);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                Button button = choiceButtons[i];
                if (button == null) continue;
                bool visible = i < frame.Choices.Count;
                button.gameObject.SetActive(visible);
                Text label = button.GetComponentInChildren<Text>(true);
                if (visible && label != null) label.text = frame.Choices[i].Text;
            }
        }

        private void Complete()
        {
            SetVisible(dialogueGroup, false);
            SetVisible(historyGroup, false);
            if (player != null) player.enabled = true;
        }

        private void BindButtons()
        {
            UnbindButtons();
            advanceButton?.onClick.AddListener(Advance);
            skipButton?.onClick.AddListener(Skip);
            historyButton?.onClick.AddListener(ShowHistory);
            closeHistoryButton?.onClick.AddListener(HideHistory);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int captured = i;
                choiceButtons[i]?.onClick.AddListener(() => SelectChoiceAt(captured));
            }
        }

        private void UnbindButtons()
        {
            advanceButton?.onClick.RemoveListener(Advance);
            skipButton?.onClick.RemoveListener(Skip);
            historyButton?.onClick.RemoveListener(ShowHistory);
            closeHistoryButton?.onClick.RemoveListener(HideHistory);
            for (int i = 0; i < choiceButtons.Length; i++) choiceButtons[i]?.onClick.RemoveAllListeners();
        }

        private void UnbindSession()
        {
            if (session == null) return;
            session.FrameChanged -= Render;
            session.Completed -= Complete;
        }

        private static string FormatSigned(int value) => value > 0 ? "+" + value : value.ToString();

        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
