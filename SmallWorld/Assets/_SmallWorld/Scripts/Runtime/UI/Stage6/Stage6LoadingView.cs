using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class Stage6LoadingView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Slider progress;
        [SerializeField] private Text statusText;

        public void Configure(CanvasGroup canvasGroup, Slider progressSlider, Text status)
        {
            group = canvasGroup;
            progress = progressSlider;
            statusText = status;
            SetProgress(0f);
            Hide();
        }

        public void Show(string status = null)
        {
            if (statusText != null) statusText.text = status ?? string.Empty;
            SetVisible(true);
        }

        public void SetProgress(float value)
        {
            if (progress != null) progress.value = Mathf.Clamp01(value);
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
