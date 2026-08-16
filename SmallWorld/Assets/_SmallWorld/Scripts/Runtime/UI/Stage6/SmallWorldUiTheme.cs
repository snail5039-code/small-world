using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public enum SmallWorldTextRole { Eyebrow, Title, Body, Prompt, Feedback }

    public static class SmallWorldUiTheme
    {
        public static Color Surface => new Color(0.035f, 0.045f, 0.06f, 0.9f);
        public static Color SurfaceRaised => new Color(0.08f, 0.11f, 0.15f, 0.94f);
        public static Color Accent => new Color(1f, 0.58f, 0.24f, 1f);
        public static Color PrimaryText => new Color(1f, 0.98f, 0.93f, 1f);
        public static Color SecondaryText => new Color(0.82f, 0.9f, 1f, 1f);
        public static Color Locked => new Color(1f, 0.48f, 0.4f, 1f);
        public const float SafeMargin720 = 16f;
        public const float SafeMargin1080 = 28f;

        public static string FormatInteractionPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return string.Empty;
            string trimmed = prompt.Trim();
            return trimmed.StartsWith("[E]") ? trimmed : "[E] " + trimmed;
        }

        public static Color FeedbackColor(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return SecondaryText;
            return message.Contains("잠김") || message.Contains("실패") || message.Contains("불가") || message.Contains("닫")
                ? Locked
                : SecondaryText;
        }

        public static void ApplyText(Text text, SmallWorldTextRole role)
        {
            if (text == null) return;
            text.fontSize = role switch
            {
                SmallWorldTextRole.Title => 18,
                SmallWorldTextRole.Prompt => 17,
                SmallWorldTextRole.Feedback => 15,
                SmallWorldTextRole.Eyebrow => 12,
                _ => 15
            };
            text.fontStyle = role == SmallWorldTextRole.Title || role == SmallWorldTextRole.Prompt
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.color = role == SmallWorldTextRole.Eyebrow ? Accent
                : role == SmallWorldTextRole.Feedback ? SecondaryText
                : PrimaryText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        public static void ApplyBottomCenterLayout(Text text, float anchorY, float width, float height)
        {
            if (text == null) return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
