using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class PlayerInteractionDetector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float range = 2f;
        [SerializeField, Min(0.1f)] private float focusRange = 2.75f;
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField] private InteractionPromptView promptView;
        private const float OriginContactRadius = 0.05f;
        private Transform view;
        private float currentTargetDistance;
        private FirstPersonPlayerController gameplayOwner;

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
            gameplayOwner = GetComponent<FirstPersonPlayerController>();
        }

        private void Update()
        {
            RefreshDetection();
        }

        public void RefreshDetection()
        {
            if (gameplayOwner == null) gameplayOwner = GetComponent<FirstPersonPlayerController>();
            bool suppressUi = gameplayOwner != null && !gameplayOwner.enabled;
            promptView?.SetSuppressed(suppressUi);
            if (suppressUi)
            {
                ClearDetection();
                return;
            }
            RaycastHit hit = default;
            IInteractable next = null;
            float targetDistance = float.PositiveInfinity;
            if (view != null)
            {
                bool originBlocked = TryGetOriginContact(out RaycastHit originHit,
                    out IInteractable originInteractable);
                if (originInteractable != null)
                {
                    hit = originHit;
                    next = originInteractable;
                    targetDistance = 0f;
                }
                else if (!originBlocked && Physics.Raycast(view.position, view.forward, out hit,
                             focusRange, layers, QueryTriggerInteraction.Ignore))
                {
                    next = hit.collider.GetComponentInParent<InteractableBase>();
                    targetDistance = hit.distance;
                }
            }

            if (!ReferenceEquals(CurrentInteractable, next))
            {
                CurrentInteractable?.SetFocused(false);
                CurrentInteractable = next;
                CurrentInteractable?.SetFocused(true);
            }

            HasTarget = next != null;
            CurrentHit = HasTarget ? hit : default;
            currentTargetDistance = HasTarget ? targetDistance : float.PositiveInfinity;
            if (promptView != null)
            {
                string prompt = HasTarget
                    ? (currentTargetDistance <= range && next.CanInteract ? next.Prompt : "가까이 가기")
                    : string.Empty;
                promptView.SetPrompt(prompt);
            }
        }

        private bool TryGetOriginContact(out RaycastHit hit, out IInteractable interactable)
        {
            hit = default;
            interactable = null;
            Collider[] contacts = Physics.OverlapSphere(view.position, OriginContactRadius, layers,
                QueryTriggerInteraction.Ignore);

            foreach (Collider contact in contacts)
            {
                if (contact.transform.IsChildOf(transform) || transform.IsChildOf(contact.transform))
                    continue;

                if (Vector3.Dot(contact.bounds.center - view.position, view.forward) < -OriginContactRadius)
                    continue;

                float reverseDistance = focusRange + contact.bounds.extents.magnitude
                    + Vector3.Distance(view.position, contact.bounds.center);
                Ray reverseRay = new Ray(view.position + view.forward * reverseDistance, -view.forward);
                if (!contact.Raycast(reverseRay, out RaycastHit contactHit, reverseDistance + OriginContactRadius))
                    continue;

                float forwardOffset = Vector3.Dot(contactHit.point - view.position, view.forward);
                if (forwardOffset < -OriginContactRadius) continue;

                IInteractable candidate = contact.GetComponentInParent<InteractableBase>();
                if (candidate == null) return true;

                if (interactable == null)
                {
                    hit = contactHit;
                    interactable = candidate;
                }
            }

            return false;
        }

        public bool TryInteract()
        {
            RefreshDetection();
            if (!HasTarget || currentTargetDistance > range || !CurrentInteractable.CanInteract) return false;
            return CurrentInteractable.TryInteract(new InteractionContext(gameObject, this));
        }

        public void ShowFeedback(string message)
        {
            promptView?.ShowFeedback(message);
        }

        private void OnDisable()
        {
            ClearDetection();
            if (promptView != null && promptView.isActiveAndEnabled)
                promptView.SetSuppressed(true);
        }

        private void ClearDetection()
        {
            CurrentInteractable?.SetFocused(false);
            CurrentInteractable = null;
            HasTarget = false;
            promptView?.SetPrompt(string.Empty);
        }
    }
}
