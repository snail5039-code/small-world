using System;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Character.Stage11
{
    public readonly struct GirlPresentationState
    {
        public GirlPresentationState(GirlMood mood, GirlBehavior behavior, int relationship,
            bool reactToDeath, int deathCount, DeathMemoryHandling lastDeathHandling)
        {
            Mood = mood;
            Behavior = behavior;
            Relationship = relationship;
            ReactToDeath = reactToDeath;
            DeathCount = deathCount;
            LastDeathHandling = lastDeathHandling;
        }

        public GirlMood Mood { get; }
        public GirlBehavior Behavior { get; }
        public int Relationship { get; }
        public bool ReactToDeath { get; }
        public int DeathCount { get; }
        public DeathMemoryHandling LastDeathHandling { get; }
    }

    public interface IGirlCharacterPresentation
    {
        void ApplyGirlState(GirlPresentationState state);
    }

    public sealed class GirlCharacterStateBridge
    {
        private readonly IGirlCharacterPresentation presentation;
        private GirlBehavior behavior;

        public GirlCharacterStateBridge(GirlCharacterState state, IGirlCharacterPresentation presentation)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            behavior = GirlBehaviorPolicy.Select(State.Relationship, State.LastPlayerAction, State.SharedPrivateMemory);
        }

        public GirlCharacterState State { get; }
        public GirlBehavior Behavior => behavior;

        public GirlBehavior React(PlayerAction action)
        {
            behavior = State.React(action);
            Publish(false);
            return behavior;
        }

        public void RememberDeath(DeathMemoryHandling handling)
        {
            State.RememberDeath(handling);
            Publish(true);
        }

        public void SynchronizeFromDialogue(DialogueState dialogue)
        {
            GirlDialogueBinding.ReadFrom(dialogue, State);
            RefreshBehaviorAndPublish(true);
        }

        public void SynchronizeToDialogue(DialogueState dialogue) => GirlDialogueBinding.WriteTo(State, dialogue);

        public bool Restore(SaveData save)
        {
            if (!GirlSaveBinding.Restore(save, State)) return false;
            RefreshBehaviorAndPublish(true);
            return true;
        }

        public void Capture(SaveData save) => GirlSaveBinding.Capture(State, save);

        public void PublishCurrentState() => Publish(true);

        private void RefreshBehaviorAndPublish(bool includeDeathReaction)
        {
            behavior = GirlBehaviorPolicy.Select(State.Relationship, State.LastPlayerAction, State.SharedPrivateMemory);
            Publish(includeDeathReaction);
        }

        private void Publish(bool includeDeathReaction)
        {
            bool reactToDeath = includeDeathReaction && State.HasPendingDeathReaction;
            presentation.ApplyGirlState(new GirlPresentationState(State.Mood, behavior, State.Relationship,
                reactToDeath, State.DeathCount, State.LastDeathHandling));
            if (reactToDeath) State.ConsumeDeathReaction();
        }
    }
}
