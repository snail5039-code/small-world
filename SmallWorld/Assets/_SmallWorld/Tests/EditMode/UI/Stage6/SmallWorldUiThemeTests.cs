using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI.Tests
{
    public sealed class SmallWorldUiThemeTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [TestCase(SmallWorldTextRole.Eyebrow, 13)]
        [TestCase(SmallWorldTextRole.Body, 16)]
        [TestCase(SmallWorldTextRole.Feedback, 16)]
        [TestCase(SmallWorldTextRole.Prompt, 18)]
        [TestCase(SmallWorldTextRole.Title, 22)]
        public void TextRoles_KeepReadableMinimumAndKoreanFallback(SmallWorldTextRole role, int minimum)
        {
            Text text = CreateText();
            text.font = null;
            text.fontSize = 1;

            SmallWorldUiTheme.ApplyText(text, role);

            Assert.That(text.font, Is.Not.Null, "Built-in fallback font must be assigned when a scene font is missing.");
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(text.resizeTextForBestFit, Is.True);
            Assert.That(text.resizeTextMinSize, Is.GreaterThanOrEqualTo(minimum));
            Assert.That(text.resizeTextMaxSize, Is.GreaterThanOrEqualTo(text.resizeTextMinSize));
            Assert.That(text.GetComponent<Outline>(), Is.Not.Null);
        }

        [Test]
        public void LongKoreanCopy_WrapsAndUsesDynamicPreferredHeight()
        {
            Text text = CreateText();
            text.text = "현재 목표를 확인하고 빛나는 조사 지점으로 이동한 뒤 상호작용하세요. 문장이 길어져도 잘리지 않아야 합니다.";

            SmallWorldUiTheme.ApplyLongText(text, true);

            ContentSizeFitter fitter = text.GetComponent<ContentSizeFitter>();
            Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(text.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void KeyHelpLayout_RemainsInsideSafeScreenArea(int width, int height)
        {
            Rect rect = SmallWorldUiTheme.KeyHelpRect(width, height);

            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(42f));
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(720f));
        }

        [Test]
        public void ModalPanel_HiddenStateNeverCapturesInput()
        {
            root = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            SmallWorldUiTheme.ApplyPanel(group, true);

            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
        }

        [Test]
        public void Button_UsesKoreanLabelAndMinimumHitArea()
        {
            root = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(10f, 10f);
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(root.transform, false);

            SmallWorldUiTheme.ApplyButton(root.GetComponent<Button>(), "계속하기");

            Rect rect = root.GetComponent<RectTransform>().rect;
            Assert.That(rect.width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(rect.height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(labelObject.GetComponent<Text>().text, Is.EqualTo("계속하기"));
        }

        private Text CreateText()
        {
            root = new GameObject("Text", typeof(RectTransform), typeof(Text));
            return root.GetComponent<Text>();
        }
    }
}
