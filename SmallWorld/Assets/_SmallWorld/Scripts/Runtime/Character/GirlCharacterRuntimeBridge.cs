using System.Collections;
using SmallWorld.Character.Stage11;
using SmallWorld.UI.Stage7;
using UnityEngine;

namespace SmallWorld.Character
{
    public sealed class GirlCharacterRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private GirlCharacterController controller;
        [SerializeField] private Stage7DialogueView dialogue;
        [SerializeField] private string characterId = "girl";

        private GirlCharacterState state;
        private bool observedWatching;
        private bool observedApproach;
        private bool dialogueWasActive;
        private int dialogueRelationshipBefore;

        public GirlCharacterState State => state;

        public void Configure(GirlCharacterController characterController, Stage7DialogueView dialogueView)
        {
            controller = characterController;
            dialogue = dialogueView;
            EnsureState();
        }

        private void Awake()
        {
            state = new GirlCharacterState(characterId);
        }

        private IEnumerator Start()
        {
            if (controller == null) controller = GetComponent<GirlCharacterController>();
            if (dialogue == null) dialogue = FindFirstObjectByType<Stage7DialogueView>();
            yield return null;
            RestoreFromDialogueState();
            if (dialogue != null)
            {
                dialogue.DialogueActivityChanged += OnDialogueActivityChanged;
                dialogueWasActive = dialogue.IsDialogueActive;
                dialogueRelationshipBefore = dialogue.State.Get(Stage7DemoDialogue.RelationshipKey);
            }
            ApplyAndPublish();
        }

        private void OnDestroy()
        {
            if (dialogue != null) dialogue.DialogueActivityChanged -= OnDialogueActivityChanged;
        }

        private void Update()
        {
            if (state == null || controller == null) return;
            PullExternalRestore();

            if (controller.BeingWatched && !observedWatching)
                React(PlayerAction.Greet);
            observedWatching = controller.BeingWatched;

            bool approached = controller.PlayerDistance <= 2.1f;
            if (approached && !observedApproach)
                React(PlayerAction.Help);
            observedApproach = approached;
        }

        private void OnDialogueActivityChanged(bool active)
        {
            if (dialogue == null) return;
            if (active)
            {
                dialogueRelationshipBefore = dialogue.State.Get(Stage7DemoDialogue.RelationshipKey);
                dialogueWasActive = true;
                return;
            }
            if (!dialogueWasActive) return;
            dialogueWasActive = false;
            int delta = dialogue.State.Get(Stage7DemoDialogue.RelationshipKey) - dialogueRelationshipBefore;
            React(delta > 0 ? PlayerAction.Listen : delta < 0 ? PlayerAction.Ignore : PlayerAction.Greet);
        }

        public void React(PlayerAction action)
        {
            EnsureState();
            GirlBehavior next = state.React(action);
            controller.ApplyCharacterState(state.Mood, next);
            PublishToDialogueState();
        }

        public void RestoreNow()
        {
            EnsureState();
            RestoreFromDialogueState();
            ApplyAndPublish();
        }

        private void RestoreFromDialogueState()
        {
            if (dialogue == null || !dialogue.State.Variables.ContainsKey(GirlCharacterKeys.Relationship(characterId))) return;
            GirlDialogueBinding.ReadFrom(dialogue.State, state);
        }

        private void PullExternalRestore()
        {
            if (dialogue == null) return;
            string key = GirlCharacterKeys.Relationship(characterId);
            if (dialogue.State.Variables.ContainsKey(key) &&
                (dialogue.State.Get(key) != state.Relationship ||
                 dialogue.State.Get(GirlCharacterKeys.Mood(characterId)) != (int)state.Mood ||
                 dialogue.State.Get(GirlCharacterKeys.LastAction(characterId)) != (int)state.LastPlayerAction ||
                 dialogue.State.Get(GirlCharacterKeys.InteractionCount(characterId)) != state.InteractionCount ||
                 dialogue.State.Get(GirlCharacterKeys.SharedMemory(characterId)) != (state.SharedPrivateMemory ? 1 : 0)))
            {
                GirlDialogueBinding.ReadFrom(dialogue.State, state);
                ApplyAndPublish();
            }
        }

        private void ApplyAndPublish()
        {
            GirlBehavior behavior = GirlBehaviorPolicy.Select(state.Relationship, state.LastPlayerAction, state.SharedPrivateMemory);
            controller?.ApplyCharacterState(state.Mood, behavior);
            PublishToDialogueState();
        }

        private void PublishToDialogueState()
        {
            if (dialogue != null) GirlDialogueBinding.WriteTo(state, dialogue.State);
        }

        private void EnsureState()
        {
            if (state == null) state = new GirlCharacterState(characterId);
        }
    }
}
