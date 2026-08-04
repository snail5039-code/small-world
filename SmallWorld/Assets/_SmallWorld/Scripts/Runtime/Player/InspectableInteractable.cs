using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class InspectableInteractable : InteractableBase
    {
        [SerializeField, TextArea] private string description = "특별한 것은 없어 보인다.";
        [SerializeField] private Transform rotateTarget;
        [SerializeField] private float rotationStep = 20f;

        public string Description => description;

        public void ConfigureInspection(string interactionPrompt, string inspectionText, Transform target = null,
            float degreesPerInteraction = 20f)
        {
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
            description = inspectionText;
            rotateTarget = target;
            rotationStep = degreesPerInteraction;
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            if (rotateTarget != null) rotateTarget.Rotate(Vector3.up, rotationStep, Space.Self);
            context.ShowFeedback(description);
            CompleteInteraction();
        }
    }
}
