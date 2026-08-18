using System.Collections;
using SmallWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Player
{
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text feedbackText;
        private Coroutine feedbackRoutine;
        private int feedbackVersion;
        private bool suppressed;

        public string CurrentPrompt => promptText != null ? promptText.text : string.Empty;
        public bool IsSuppressed => suppressed;

        private void Awake() => ApplyTheme();

        public void Configure(Text prompt, Text feedback)
        {
            promptText = prompt;
            feedbackText = feedback;
            ApplyTheme();
            SetPrompt(string.Empty);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void ApplyTheme()
        {
            SmallWorldUiTheme.ApplyText(promptText, SmallWorldTextRole.Prompt);
            SmallWorldUiTheme.ApplyBottomCenterLayout(promptText, 0.18f, 760f, 52f);
            SmallWorldUiTheme.ApplyText(feedbackText, SmallWorldTextRole.Feedback);
            SmallWorldUiTheme.ApplyBottomCenterLayout(feedbackText, 0.27f, 760f, 64f);
        }

        public void SetPrompt(string prompt)
        {
            if (promptText == null) return;
            promptText.text = suppressed ? string.Empty : SmallWorldUiTheme.FormatInteractionPrompt(prompt);
            promptText.gameObject.SetActive(promptText.text.Length > 0);
        }

        public void ShowFeedback(string message, float duration = 2.5f)
        {
            if (feedbackText == null || suppressed) return;
            CancelFeedbackRoutine();
            int version = feedbackVersion;
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, duration, version));
        }

        private IEnumerator ShowFeedbackRoutine(string message, float duration, int version)
        {
            feedbackText.text = message;
            feedbackText.color = SmallWorldUiTheme.FeedbackColor(message);
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            yield return new WaitForSecondsRealtime(duration);
            if (version == feedbackVersion && feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
                feedbackRoutine = null;
            }
        }

        public void SetSuppressed(bool value)
        {
            if (suppressed == value) return;
            suppressed = value;
            if (!suppressed) return;
            CancelFeedbackRoutine();
            if (promptText != null)
            {
                promptText.text = string.Empty;
                promptText.gameObject.SetActive(false);
            }
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void CancelFeedbackRoutine()
        {
            // Never call a native coroutine stop API from a sibling's teardown path. Scene
            // unload can destroy this MonoBehaviour before the detector receives OnDisable.
            feedbackVersion++;
            feedbackRoutine = null;
        }

        private void OnDisable()
        {
            feedbackVersion++;
            feedbackRoutine = null;
        }

        private void OnDestroy()
        {
            feedbackVersion++;
            feedbackRoutine = null;
        }
    }
}
