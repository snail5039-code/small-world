using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class NotificationQueueView : MonoBehaviour
    {
        private readonly Queue<Entry> queue = new Queue<Entry>();

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text messageText;
        [SerializeField, Min(0.1f)] private float defaultDuration = 2.5f;
        private Coroutine worker;

        private struct Entry
        {
            public string message;
            public float duration;
        }

        public int PendingCount => queue.Count + (worker == null ? 0 : 1);

        public void Configure(CanvasGroup canvasGroup, Text message)
        {
            Clear();
            group = canvasGroup;
            messageText = message;
            SmallWorldUiTheme.ApplyPanel(group, false);
            SmallWorldUiTheme.ApplyText(messageText, SmallWorldTextRole.Feedback);
            if (messageText != null) messageText.text = string.Empty;
            SetVisible(false);
        }

        public void Enqueue(string message, float duration = -1f)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            queue.Enqueue(new Entry
            {
                message = message,
                duration = duration > 0f ? duration : defaultDuration
            });
            if (worker == null) worker = StartCoroutine(Process());
        }

        public void Clear()
        {
            queue.Clear();
            if (worker != null) StopCoroutine(worker);
            worker = null;
            SetVisible(false);
        }

        private IEnumerator Process()
        {
            while (queue.Count > 0)
            {
                Entry entry = queue.Dequeue();
                if (messageText != null)
                {
                    messageText.text = entry.message;
                    messageText.color = SmallWorldUiTheme.FeedbackColor(entry.message);
                }
                SetVisible(true);
                yield return new WaitForSecondsRealtime(entry.duration);
                SetVisible(false);
            }
            worker = null;
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
