using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class PlayerInteractionDetector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float range = 2f;
        [SerializeField, Min(0.1f)] private float focusRange = 2.75f;
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField] private InteractionPromptView promptView;
        private Transform view;

        public bool HasTarget { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
        public IInteractable CurrentInteractable { get; private set; }
        public float Range => range;

        public bool TryGetTarget(out RaycastHit hit)
        {
            hit = CurrentHit;
            return HasTarget;
        }

        public void Configure(Transform viewTransform, float detectionRange = 2f)
        {
            view = viewTransform;
            range = Mathf.Max(0.1f, detectionRange);
            focusRange = Mathf.Max(range, range + 0.75f);
        }

        public void ConfigureView(InteractionPromptView viewComponent)
        {
            promptView = viewComponent;
        }

        private void Awake()
        {
            if (view == null && Camera.main != null) view = Camera.main.transform;
        }

        private void Update()
        {
            RefreshDetection();
        }

        public void RefreshDetection()
        {
            RaycastHit hit = default;
            IInteractable next = null;
            if (view != null && Physics.Raycast(view.position, view.forward, out hit,
                    focusRange, layers, QueryTriggerInteraction.Ignore))
                next = hit.collider.GetComponentInParent<InteractableBase>();

            if (!ReferenceEquals(CurrentInteractable, next))
            {
                CurrentInteractable?.SetFocused(false);
                CurrentInteractable = next;
                CurrentInteractable?.SetFocused(true);
            }

            HasTarget = next != null;
            CurrentHit = HasTarget ? hit : default;
            if (promptView != null)
            {
                string prompt = HasTarget
                    ? (hit.distance <= range && next.CanInteract ? next.Prompt : "가까이 가기")
                    : string.Empty;
                promptView.SetPrompt(prompt);
            }
        }

        public bool TryInteract()
        {
            RefreshDetection();
            if (!HasTarget || CurrentHit.distance > range || !CurrentInteractable.CanInteract) return false;
            return CurrentInteractable.TryInteract(new InteractionContext(gameObject, this));
        }

        public void ShowFeedback(string message)
        {
            promptView?.ShowFeedback(message);
        }

        private void OnDisable()
        {
            CurrentInteractable?.SetFocused(false);
            CurrentInteractable = null;
            HasTarget = false;
            promptView?.SetPrompt(string.Empty);
        }
    }
}
