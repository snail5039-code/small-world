using System.Collections;
using UnityEngine;

namespace SmallWorld.Player
{
    public sealed class DoorInteractable : InteractableBase
    {
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 95f;
        [SerializeField, Min(0f)] private float duration = 0.45f;
        private Quaternion closedRotation;

        public bool IsOpen { get; private set; }

        public void ConfigureDoor(string interactionPrompt, Transform pivot, float angle = 95f, float seconds = 0.45f)
        {
            Configure(interactionPrompt, GetComponentsInChildren<Renderer>(true));
            doorPivot = pivot;
            openAngle = angle;
            duration = Mathf.Max(0f, seconds);
            closedRotation = pivot != null ? pivot.localRotation : Quaternion.identity;
        }

        private void Awake()
        {
            if (doorPivot == null) doorPivot = transform;
            closedRotation = doorPivot.localRotation;
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            IsOpen = !IsOpen;
            SetPrompt(IsOpen ? "문 닫기" : "문 열기");
            context.ShowFeedback(IsOpen ? "문이 조용히 열렸다." : "문을 닫았다.");
            Quaternion target = closedRotation * Quaternion.Euler(0f, IsOpen ? openAngle : 0f, 0f);
            if (duration <= 0f)
            {
                doorPivot.localRotation = target;
                CompleteInteraction();
                return;
            }
            StartCoroutine(AnimateDoor(target));
        }

        private IEnumerator AnimateDoor(Quaternion target)
        {
            Quaternion start = doorPivot.localRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                doorPivot.localRotation = Quaternion.Slerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            doorPivot.localRotation = target;
            CompleteInteraction();
        }
    }
}
