using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class ToggleUseInteractable : InteractableBase
    {
        [SerializeField] private Light controlledLight;
        [SerializeField] private string enabledMessage = "전원을 켰다.";
        [SerializeField] private string disabledMessage = "전원을 껐다.";

        public bool IsUsed { get; private set; }

        public void ConfigureUse(string interactionPrompt, Light target, string onMessage, string offMessage)
        {
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
            controlledLight = target;
            enabledMessage = onMessage;
            disabledMessage = offMessage;
            IsUsed = controlledLight != null && controlledLight.enabled;
            SetPrompt(IsUsed ? "끄기" : interactionPrompt);
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            IsUsed = !IsUsed;
            if (controlledLight != null) controlledLight.enabled = IsUsed;
            SetPrompt(IsUsed ? "끄기" : "사용하기");
            context.ShowFeedback(IsUsed ? enabledMessage : disabledMessage);
            CompleteInteraction();
        }
    }
}
