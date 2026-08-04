using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Player
{
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text feedbackText;
        private Coroutine feedbackRoutine;

        public string CurrentPrompt => promptText != null ? promptText.text : string.Empty;

        public void Configure(Text prompt, Text feedback)
        {
            promptText = prompt;
            feedbackText = feedback;
            SetPrompt(string.Empty);
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        public void SetPrompt(string prompt)
        {
            if (promptText == null) return;
            promptText.text = string.IsNullOrWhiteSpace(prompt) ? string.Empty : $"[E] {prompt}";
            promptText.gameObject.SetActive(promptText.text.Length > 0);
        }

        public void ShowFeedback(string message, float duration = 2.5f)
        {
            if (feedbackText == null) return;
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, duration));
        }

        private IEnumerator ShowFeedbackRoutine(string message, float duration)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            yield return new WaitForSecondsRealtime(duration);
            feedbackText.gameObject.SetActive(false);
            feedbackRoutine = null;
        }
    }
}
