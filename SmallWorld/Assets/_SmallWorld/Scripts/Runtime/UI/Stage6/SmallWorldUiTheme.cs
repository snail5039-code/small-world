using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public enum SmallWorldTextRole { Eyebrow, Title, Body, Prompt, Feedback }
    public enum SmallWorldUiStatus { Neutral, Success, Warning, Locked }

    public static class SmallWorldUiTheme
    {
        public static Color Surface => new Color(0.035f, 0.045f, 0.06f, 0.9f);
        public static Color SurfaceRaised => new Color(0.08f, 0.11f, 0.15f, 0.94f);
        public static Color Accent => new Color(1f, 0.58f, 0.24f, 1f);
        public static Color PrimaryText => new Color(1f, 0.98f, 0.93f, 1f);
        public static Color SecondaryText => new Color(0.82f, 0.9f, 1f, 1f);
        public static Color Locked => new Color(1f, 0.48f, 0.4f, 1f);
        public static Color Success => new Color(0.36f, 0.86f, 0.62f, 1f);
        public static Color Warning => new Color(1f, 0.76f, 0.3f, 1f);
        public const string GameplayKeyHelp = "[E] 조사   [Tab] 기록·인벤토리   [Esc] 일시정지";
        public const string StoryRouteKeyHelp = "[E] 조사   [PageUp/PageDown] 방 이동   [Home] 현실방   [Tab] 기록   [Esc] 일시정지";
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

        public static Color StatusColor(SmallWorldUiStatus status) => status switch
        {
            SmallWorldUiStatus.Success => Success,
            SmallWorldUiStatus.Warning => Warning,
            SmallWorldUiStatus.Locked => Locked,
            _ => SecondaryText
        };

        public static void ApplyText(Text text, SmallWorldTextRole role)
        {
            if (text == null) return;
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(text.fontSize, MinimumFontSize(role));
            text.fontStyle = role == SmallWorldTextRole.Title || role == SmallWorldTextRole.Prompt
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.color = role == SmallWorldTextRole.Eyebrow ? Accent
                : role == SmallWorldTextRole.Feedback ? SecondaryText
                : PrimaryText;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // Long Korean labels need to adapt at 720p, but must never shrink below
            // the readable role minimum. Dynamic-height body copy can still grow.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = MinimumFontSize(role);
            text.resizeTextMaxSize = Mathf.Max(text.fontSize, MinimumFontSize(role));
            text.raycastTarget = false;

            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        public static int MinimumFontSize(SmallWorldTextRole role) => role switch
            {
                SmallWorldTextRole.Title => 22,
                SmallWorldTextRole.Prompt => 18,
                SmallWorldTextRole.Feedback => 16,
                SmallWorldTextRole.Eyebrow => 13,
                _ => 16
            };

        public static void ApplyLongText(Text text, bool dynamicHeight)
        {
            ApplyText(text, SmallWorldTextRole.Body);
            if (text == null) return;
            text.alignment = TextAnchor.UpperLeft;
            if (!dynamicHeight) return;
            ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>() ?? text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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

        public static void ApplyPanel(CanvasGroup group, bool modal)
        {
            if (group == null) return;
            bool visible = group.alpha > 0.01f && group.gameObject.activeInHierarchy;
            group.interactable = modal && visible;
            group.blocksRaycasts = modal && visible;
            Image image = group.GetComponent<Image>();
            if (image != null) image.color = modal ? SurfaceRaised : Surface;
        }

        public static void ApplyButton(Button button, string koreanLabel = null)
        {
            if (button == null) return;
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                if (Mathf.Abs(rect.rect.width) < 44f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 44f);
                if (Mathf.Abs(rect.rect.height) < 44f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44f);
            }
            Image background = button.GetComponent<Image>();
            if (background != null) background.color = SurfaceRaised;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            if (!string.IsNullOrWhiteSpace(koreanLabel)) label.text = koreanLabel;
            ApplyText(label, SmallWorldTextRole.Body);
            label.fontStyle = FontStyle.Bold;
            label.color = PrimaryText;
        }

        public static void ApplySlider(Slider slider)
        {
            if (slider == null) return;
            if (slider.fillRect != null)
            {
                Graphic fill = slider.fillRect.GetComponent<Graphic>();
                if (fill != null) fill.color = Accent;
            }
        }

        public static Rect KeyHelpRect(int screenWidth, int screenHeight)
        {
            float margin = screenHeight <= 720 ? SafeMargin720 : SafeMargin1080;
            float width = Mathf.Min(760f, screenWidth - margin * 2f);
            return new Rect(screenWidth - width - margin, screenHeight - 42f - margin, width, 42f);
        }

        public static void DrawKeyHelp(string text, int screenWidth, int screenHeight)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Rect rect = KeyHelpRect(screenWidth, screenHeight);
            Color previous = GUI.color;
            GUI.color = Surface;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = screenHeight <= 720 ? 13 : 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = SecondaryText;
            GUI.Label(rect, text, style);
        }
    }
}
