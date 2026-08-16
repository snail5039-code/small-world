using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmallWorld.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage1To15UiBaselineAcceptanceTests
    {
        private static readonly object[] SceneInventory =
        {
            new object[] { "Assets/_SmallWorld/Scenes/00_Boot.unity", new[] { "Loading Canvas", "Loading Panel" } },
            new object[] { "Assets/_SmallWorld/Scenes/01_MainMenu.unity", new[] { "Main Menu Canvas", "Title Panel", "Menu Panel", "Settings Panel", "Loading Panel" } },
            new object[] { "Assets/_SmallWorld/Scenes/02_RealityRoom.unity", new[] { "Gameplay HUD", "Interaction Prompt", "Stage 7 Dialogue UI", "Stage 8 Record UI", "Stage 9 Photo Puzzle UI", "Stage 10 Save Integration", "Settings Panel", "Pause Panel" } },
            new object[] { "Assets/_SmallWorld/Scenes/03_FirstMemory.unity", new[] { "Player HUD", "Interaction UI", "Interaction Prompt" } },
            new object[] { "Assets/_SmallWorld/Scenes/04_StoryRoute.unity", new[] { "Player HUD", "Interaction UI", "Interaction Prompt", "Stage 15 Story Route" } }
        };

        [TestCaseSource(nameof(SceneInventory))]
        public void SceneExposesItsRequiredPlayerUiInventory(string scene, string[] required)
        {
            EditorSceneManager.OpenScene(scene);
            foreach (string name in required)
                Assert.That(GameObject.Find(name) ?? FindInactive(name), Is.Not.Null,
                    scene + " is missing required UI: " + name);
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/03_FirstMemory.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/04_StoryRoute.unity")]
        public void PlayerFacingScenesUseResponsiveCanvasAndExplicitSafeArea(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(canvases, Is.Not.Empty);
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(scalers, Is.Not.Empty, scene + " has no responsive CanvasScaler.");
            foreach (CanvasScaler scaler in scalers)
            {
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));
            }
            Assert.That(Object.FindFirstObjectByType<SafeAreaFitter>(FindObjectsInactive.Include), Is.Not.Null,
                scene + " needs an explicit safe-area root for 1280x720 and 1920x1080.");
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void MenuAndRealityRoomHaveNoPlaceholderOrBrokenUserText(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(texts.Length, Is.GreaterThan(5));
            string[] forbidden = { "Lorem", "Paused", "Press Esc", "No route records", "Button", "�" };
            foreach (Text text in texts.Where(item => !string.IsNullOrWhiteSpace(item.text)))
            {
                Assert.That(text.font, Is.Not.Null, text.name + " has no Korean-capable project font.");
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12), text.name + " is too small to read.");
                foreach (string value in forbidden)
                    Assert.That(text.text, Does.Not.Contain(value), text.name + " exposes placeholder English or broken text.");
            }
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void EveryButtonHasReadableLabelAndMinimumSerializedHitArea(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(buttons, Is.Not.Empty);
            foreach (Button button in buttons)
            {
                Text label = button.GetComponentInChildren<Text>(true);
                Assert.That(label, Is.Not.Null, button.name + " has no visible label.");
                Assert.That(label.text, Is.Not.Empty, button.name + " has an empty label.");
                RectTransform rect = button.transform as RectTransform;
                Assert.That(rect, Is.Not.Null);
                Vector2 size = rect.rect.size;
                if (size.x <= 0f || size.y <= 0f) size = rect.sizeDelta;
                Assert.That(Mathf.Abs(size.x), Is.GreaterThanOrEqualTo(44f), button.name + " hit width is too small.");
                Assert.That(Mathf.Abs(size.y), Is.GreaterThanOrEqualTo(44f), button.name + " hit height is too small.");
            }
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void HiddenOverlayCanvasGroupsDoNotInterceptPointerInput(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            CanvasGroup[] groups = Object.FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(groups, Is.Not.Empty);
            foreach (CanvasGroup group in groups)
            {
                bool hidden = !group.gameObject.activeInHierarchy || group.alpha <= 0.01f;
                if (!hidden) continue;
                Assert.That(group.interactable, Is.False, group.name + " is hidden but interactable.");
                Assert.That(group.blocksRaycasts, Is.False, group.name + " invisibly blocks pointer input.");
            }
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void TextOverOpaqueCardsMeetsMinimumContrast(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            int checkedCount = 0;
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.75f) continue;
                Image background = FindOpaqueBackground(text.transform.parent);
                if (background == null) continue;
                float required = text.fontSize >= 18 ? 3f : 4.5f;
                Assert.That(Contrast(text.color, background.color), Is.GreaterThanOrEqualTo(required),
                    text.name + " does not contrast with " + background.name + ".");
                checkedCount++;
            }
            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(3), scene + " has no auditable text/card contrast.");
        }

        [Test]
        public void RealityRoomContainsTheExpectedInputOwnershipLayers()
        {
            EditorSceneManager.OpenScene("Assets/_SmallWorld/Scenes/02_RealityRoom.unity");
            string[] modalRoots =
            {
                "Pause Panel", "Settings Panel", "Inspection Panel", "Stage 7 Dialogue UI",
                "Stage 8 Record UI", "Stage 9 Photo Puzzle UI", "Stage 10 Save Integration"
            };
            foreach (string name in modalRoots)
            {
                GameObject item = GameObject.Find(name) ?? FindInactive(name);
                Assert.That(item, Is.Not.Null);
                Assert.That(item.GetComponent<CanvasGroup>() ?? item.GetComponentInChildren<CanvasGroup>(true),
                    Is.Not.Null, name + " needs explicit interactable/raycast ownership.");
            }
        }

        private static GameObject FindInactive(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(item =>
                item != null && item.scene.IsValid() && item.name == name);
        }

        private static Image FindOpaqueBackground(Transform current)
        {
            while (current != null)
            {
                Image image = current.GetComponent<Image>();
                if (image != null && image.color.a >= 0.75f) return image;
                current = current.parent;
            }
            return null;
        }

        private static float Contrast(Color first, Color second)
        {
            float bright = Mathf.Max(Luminance(first), Luminance(second));
            float dark = Mathf.Min(Luminance(first), Luminance(second));
            return (bright + 0.05f) / (dark + 0.05f);
        }

        private static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        }

        private static float Linear(float value)
        {
            return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }
    }
}
