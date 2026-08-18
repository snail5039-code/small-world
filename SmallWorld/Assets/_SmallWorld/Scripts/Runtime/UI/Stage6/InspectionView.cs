using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class InspectionView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;

        public event Action CloseRequested;

        public void Configure(CanvasGroup canvasGroup, Text title, Text body, Button close)
        {
            closeButton?.onClick.RemoveListener(RequestClose);
            group = canvasGroup;
            titleText = title;
            bodyText = body;
            closeButton = close;
            ApplyTheme();
            closeButton?.onClick.AddListener(RequestClose);
            Hide();
        }

        private void Awake()
        {
            ApplyTheme();
            closeButton?.onClick.AddListener(RequestClose);
            Hide();
        }

        private void ApplyTheme()
        {
            SmallWorldUiTheme.ApplyPanel(group, true);
            SmallWorldUiTheme.ApplyText(titleText, SmallWorldTextRole.Title);
            SmallWorldUiTheme.ApplyText(bodyText, SmallWorldTextRole.Body);
            SmallWorldUiTheme.ApplyButton(closeButton, "닫기");
        }

        private void OnDestroy()
        {
            closeButton?.onClick.RemoveListener(RequestClose);
        }

        public void Show(string title, string body)
        {
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (bodyText != null) bodyText.text = body ?? string.Empty;
            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        private void RequestClose()
        {
            Hide();
            CloseRequested?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
