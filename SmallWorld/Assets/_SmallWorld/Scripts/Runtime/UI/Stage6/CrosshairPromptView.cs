using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class CrosshairPromptView : MonoBehaviour
    {
        [SerializeField] private Graphic crosshair;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private Text promptText;

        public bool IsPromptVisible { get; private set; }

        public void Configure(Graphic crosshairGraphic, CanvasGroup group, Text text)
        {
            crosshair = crosshairGraphic;
            promptGroup = group;
            promptText = text;
            HidePrompt();
        }

        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair != null) crosshair.enabled = visible;
        }

        public void ShowPrompt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                HidePrompt();
                return;
            }

            if (promptText != null) promptText.text = text;
            SetPromptVisible(true);
        }

        public void HidePrompt()
        {
            if (promptText != null) promptText.text = string.Empty;
            SetPromptVisible(false);
        }

        private void SetPromptVisible(bool visible)
        {
            IsPromptVisible = visible;
            if (promptGroup == null) return;
            promptGroup.alpha = visible ? 1f : 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
        }
    }
}
