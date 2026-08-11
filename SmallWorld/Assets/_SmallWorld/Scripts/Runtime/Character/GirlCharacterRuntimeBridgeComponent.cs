using System;
using SmallWorld.Character.Stage11;
using SmallWorld.Dialogue.Stage7;
using SmallWorld.Save.Stage10;
using UnityEngine;

namespace SmallWorld.Character
{
    public sealed class GirlCharacterRuntimeBridgeComponent : MonoBehaviour
    {
        [SerializeField] private GirlCharacterController presentation;
        [SerializeField] private string characterId = "girl";
        [SerializeField, Range(GirlCharacterState.MinimumRelationship, GirlCharacterState.MaximumRelationship)]
        private int initialRelationship;

        private GirlCharacterStateBridge bridge;

        public GirlCharacterState State { get { EnsureInitialized(); return bridge.State; } }
        public GirlBehavior CurrentBehavior { get { EnsureInitialized(); return bridge.Behavior; } }

        public void Configure(GirlCharacterController controller, string id = "girl", int relationship = 0)
        {
            presentation = controller != null ? controller : throw new ArgumentNullException(nameof(controller));
            characterId = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Value cannot be empty.", nameof(id)) : id;
            initialRelationship = relationship;
            bridge = null;
            EnsureInitialized();
        }

        public GirlBehavior React(PlayerAction action)
        {
            EnsureInitialized();
            return bridge.React(action);
        }

        public void RememberDeath(DeathMemoryHandling handling)
        {
            EnsureInitialized();
            bridge.RememberDeath(handling);
        }

        public void SynchronizeFromDialogue(DialogueState dialogue)
        {
            EnsureInitialized();
            bridge.SynchronizeFromDialogue(dialogue);
        }

        public void SynchronizeToDialogue(DialogueState dialogue)
        {
            EnsureInitialized();
            bridge.SynchronizeToDialogue(dialogue);
        }

        public bool Restore(SaveData save)
        {
            EnsureInitialized();
            return bridge.Restore(save);
        }

        public void Capture(SaveData save)
        {
            EnsureInitialized();
            bridge.Capture(save);
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (bridge != null) return;
            if (presentation == null) presentation = GetComponent<GirlCharacterController>();
            if (presentation == null)
                throw new InvalidOperationException("A GirlCharacterController presentation is required.");
            bridge = new GirlCharacterStateBridge(
                new GirlCharacterState(characterId, initialRelationship), presentation);
            bridge.PublishCurrentState();
        }
    }
}
