using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class PlayerInteractionDetector : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float range = 2f;
        [SerializeField] private LayerMask layers = ~0;
        private Transform view;

        public bool HasTarget { get; private set; }
        public RaycastHit CurrentHit { get; private set; }
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
        }

        private void Awake()
        {
            if (view == null && Camera.main != null) view = Camera.main.transform;
        }

        private void Update()
        {
            RaycastHit hit = default;
            HasTarget = view != null && Physics.Raycast(view.position, view.forward, out hit,
                range, layers, QueryTriggerInteraction.Ignore);
            CurrentHit = HasTarget ? hit : default;
        }
    }
}
