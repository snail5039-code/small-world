using UnityEngine;
using SmallWorld.Character.Stage11;

namespace SmallWorld.Character
{
    public enum GirlExpression
    {
        Calm,
        Curious,
        Happy,
        Surprised
    }

    public sealed class GirlCharacterController : MonoBehaviour, IGirlCharacterPresentation
    {
        [Header("Replaceable presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform headTarget;
        [SerializeField] private Renderer faceRenderer;

        [Header("Room movement")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField, Min(0.1f)] private float walkSpeed = 0.75f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 5f;
        [SerializeField, Min(0f)] private float pauseSeconds = 2.2f;

        [Header("Player response")]
        [SerializeField] private Transform playerView;
        [SerializeField, Min(0.1f)] private float noticeDistance = 4.5f;
        [SerializeField, Range(-1f, 1f)] private float noticeViewDot = 0.45f;

        private int waypointIndex;
        private float pauseRemaining;
        private float blinkTimer;
        private Quaternion headRestRotation;
        private Vector3 visualRestPosition;
        private Material faceMaterial;
        private GirlExpression expression;
        private bool playerNoticed;
        private bool beingWatched;
        private float playerDistance = float.PositiveInfinity;
        private GirlBehavior behavior = GirlBehavior.Observe;
        private GirlMood mood = GirlMood.Guarded;

        public GirlExpression Expression => expression;
        public bool PlayerNoticed => playerNoticed;
        public bool BeingWatched => beingWatched;
        public float PlayerDistance => playerDistance;
        public GirlBehavior Behavior => behavior;
        public Transform VisualRoot => visualRoot;

        public void Configure(Transform presentationRoot, Transform head, Renderer face, Transform[] patrolPoints, Transform view)
        {
            visualRoot = presentationRoot;
            headTarget = head;
            faceRenderer = face;
            waypoints = patrolPoints;
            playerView = view;
            CachePresentationState();
        }

        private void Awake()
        {
            if (playerView == null && Camera.main != null) playerView = Camera.main.transform;
            CachePresentationState();
            SetExpression(GirlExpression.Calm);
        }

        private void CachePresentationState()
        {
            if (headTarget != null) headRestRotation = headTarget.localRotation;
            if (visualRoot != null) visualRestPosition = visualRoot.localPosition;
            if (faceRenderer != null)
            {
                faceMaterial = new Material(faceRenderer.sharedMaterial);
                faceRenderer.material = faceMaterial;
            }
        }

        private void Update()
        {
            UpdatePlayerResponse();
            UpdateMovement();
            UpdatePresentation();
        }

        private void UpdatePlayerResponse()
        {
            if (playerView == null) return;
            Vector3 toGirl = transform.position + Vector3.up * 1.35f - playerView.position;
            playerDistance = toGirl.magnitude;
            beingWatched = playerDistance <= noticeDistance && Vector3.Dot(playerView.forward, toGirl.normalized) >= noticeViewDot;
            bool close = playerDistance <= PreferredDistance();
            playerNoticed = beingWatched || close;
        }

        private void UpdateMovement()
        {
            if (UpdateRelationshipMovement()) return;
            if (waypoints == null || waypoints.Length == 0 || playerNoticed) return;
            if (pauseRemaining > 0f)
            {
                pauseRemaining -= Time.deltaTime;
                SetMoving(false);
                return;
            }

            Transform target = waypoints[waypointIndex];
            if (target == null) return;
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.04f)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                pauseRemaining = pauseSeconds;
                SetMoving(false);
                return;
            }

            Vector3 direction = offset.normalized;
            transform.position += direction * Mathf.Min(walkSpeed * Time.deltaTime, offset.magnitude);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            SetMoving(true);
        }

        private bool UpdateRelationshipMovement()
        {
            if (playerView == null) return false;
            Vector3 flat = playerView.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return false;

            float preferred = PreferredDistance();
            bool approach = behavior == GirlBehavior.Approach && flat.magnitude > preferred;
            bool retreat = (behavior == GirlBehavior.KeepDistance || behavior == GirlBehavior.Withdraw) && flat.magnitude < preferred;
            if (!approach && !retreat) return behavior == GirlBehavior.ShareMemory;

            Vector3 direction = flat.normalized * (retreat ? -1f : 1f);
            transform.position += direction * walkSpeed * (retreat ? 1.25f : 0.8f) * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(retreat ? -direction : direction), turnSpeed * Time.deltaTime);
            SetMoving(true);
            return true;
        }

        private float PreferredDistance()
        {
            switch (behavior)
            {
                case GirlBehavior.Withdraw: return 5f;
                case GirlBehavior.KeepDistance: return 3.6f;
                case GirlBehavior.Observe: return 2.8f;
                default: return 1.65f;
            }
        }

        private void UpdatePresentation()
        {
            float gait = animator == null && visualRoot != null ? Mathf.Sin(Time.time * 7f) * 0.025f : 0f;
            if (visualRoot != null) visualRoot.localPosition = visualRestPosition + Vector3.up * gait;

            if (headTarget != null)
            {
                Quaternion desired = headRestRotation;
                if (playerNoticed && playerView != null)
                {
                    Vector3 localDirection = headTarget.parent.InverseTransformDirection(playerView.position - headTarget.position);
                    desired = Quaternion.LookRotation(localDirection, Vector3.up);
                }
                headTarget.localRotation = Quaternion.Slerp(headTarget.localRotation, desired, Time.deltaTime * 4f);
            }

            blinkTimer += Time.deltaTime;
            if (faceRenderer != null) faceRenderer.transform.localScale = new Vector3(1f, blinkTimer % 4.2f > 4.05f ? 0.15f : 1f, 1f);
        }

        private void SetMoving(bool moving)
        {
            if (animator != null) animator.SetBool("Moving", moving);
        }

        public void SetExpression(GirlExpression next)
        {
            if (expression == next && faceMaterial != null) return;
            expression = next;
            if (animator != null) animator.SetInteger("Expression", (int)next);
            if (faceMaterial == null) return;
            faceMaterial.color = next switch
            {
                GirlExpression.Curious => new Color(0.45f, 0.72f, 0.95f),
                GirlExpression.Happy => new Color(1f, 0.58f, 0.65f),
                GirlExpression.Surprised => new Color(1f, 0.82f, 0.42f),
                _ => new Color(0.62f, 0.48f, 0.42f)
            };
        }

        public void ApplyCharacterState(GirlMood nextMood, GirlBehavior nextBehavior)
        {
            mood = nextMood;
            behavior = nextBehavior;
            noticeDistance = nextMood == GirlMood.Hurt ? 3f : nextMood == GirlMood.Warm ? 6f : 4.5f;
            SetExpression(nextMood == GirlMood.Warm ? GirlExpression.Happy :
                nextMood == GirlMood.Hurt ? GirlExpression.Surprised :
                nextMood == GirlMood.Calm ? GirlExpression.Calm : GirlExpression.Curious);
        }

        public void ApplyGirlState(GirlPresentationState state)
        {
            ApplyCharacterState(state.Mood, state.Behavior);
            if (animator == null) return;
            animator.SetInteger("RelationshipBehavior", (int)state.Behavior);
            animator.SetInteger("DeathCount", state.DeathCount);
            if (state.ReactToDeath) animator.SetTrigger("DeathMemoryReaction");
        }
    }
}
