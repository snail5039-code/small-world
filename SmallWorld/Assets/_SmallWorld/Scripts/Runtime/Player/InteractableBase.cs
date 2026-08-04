using System;
using UnityEngine;

namespace SmallWorld.Player
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "조사하기";
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Color highlightColor = new Color(0.25f, 0.9f, 0.85f, 1f);

        private MaterialPropertyBlock propertyBlock;
        private bool busy;
        private bool focused;

        public string Prompt => CanInteract ? prompt : string.Empty;
        public virtual bool CanInteract => isActiveAndEnabled && !busy;
        public bool IsBusy => busy;
        public int InteractionCount { get; private set; }
        public event Action<InteractableBase> InteractionCompleted;

        public void Configure(string interactionPrompt, params Renderer[] renderers)
        {
            prompt = interactionPrompt;
            highlightRenderers = renderers;
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            ApplyHighlight(value);
        }

        public bool TryInteract(InteractionContext context)
        {
            if (!CanInteract) return false;
            busy = true;
            InteractionCount++;
            BeginInteraction(context);
            return true;
        }

        protected abstract void BeginInteraction(InteractionContext context);

        protected void CompleteInteraction()
        {
            busy = false;
            InteractionCompleted?.Invoke(this);
        }

        protected void SetPrompt(string value)
        {
            prompt = value;
        }

        protected virtual void OnDisable()
        {
            SetFocused(false);
            busy = false;
        }

        private void ApplyHighlight(bool enabled)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            if (highlightRenderers == null || highlightRenderers.Length == 0)
                highlightRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer target in highlightRenderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", enabled ? highlightColor * 0.65f : Color.black);
                target.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
