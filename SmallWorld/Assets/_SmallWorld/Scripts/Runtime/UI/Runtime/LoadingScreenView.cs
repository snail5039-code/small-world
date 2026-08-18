using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;

        private void Awake()
        {
            SmallWorldUiTheme.ApplyPanel(canvasGroup, true);
            SmallWorldUiTheme.ApplySlider(progressBar);
        }

        public void Configure(CanvasGroup group, Slider slider)
        {
            canvasGroup = group;
            progressBar = slider;
            SmallWorldUiTheme.ApplyPanel(canvasGroup, true);
            SmallWorldUiTheme.ApplySlider(progressBar);
            HideImmediate();
        }

        public void Show()
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning("[SmallWorld] Loading screen has no CanvasGroup.", this);
                return;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }

        public void HideImmediate()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
