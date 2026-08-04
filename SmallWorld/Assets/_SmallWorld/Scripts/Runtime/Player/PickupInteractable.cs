using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class PickupInteractable : InteractableBase
    {
        [SerializeField] private string itemId = "unknown_item";
        [SerializeField] private string pickupMessage = "물건을 주웠다.";
        private bool collected;

        public string ItemId => itemId;
        public bool Collected => collected;
        public override bool CanInteract => base.CanInteract && !collected;

        public void ConfigurePickup(string interactionPrompt, string id, string message)
        {
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
            itemId = id;
            pickupMessage = message;
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            collected = true;
            context.ShowFeedback(pickupMessage);
            foreach (Renderer target in GetComponentsInChildren<Renderer>(true)) target.enabled = false;
            foreach (Collider target in GetComponentsInChildren<Collider>(true)) target.enabled = false;
            CompleteInteraction();
        }
    }
}
