using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage1To15UiBaselineAcceptanceTests
    {
        private static readonly object[] SceneInventory =
        {
            new object[] { "Assets/_SmallWorld/Scenes/00_Boot.unity", new[] { "Loading Canvas", "Loading Progress" } },
            new object[] { "Assets/_SmallWorld/Scenes/01_MainMenu.unity", new[] { "Main Menu Canvas", "Title Panel", "Menu Panel", "Settings Panel", "Loading Panel" } },
            new object[] { "Assets/_SmallWorld/Scenes/02_RealityRoom.unity", new[] { "Gameplay HUD", "Interaction Prompt", "Stage 7 Dialogue UI", "Stage 8 Record UI", "Stage 9 Photo Puzzle UI", "Stage 10 Save Integration", "Settings Panel", "Pause Panel" } },
            new object[] { "Assets/_SmallWorld/Scenes/03_FirstMemory.unity", new[] { "Player HUD", "Interaction UI", "Interaction Prompt" } },
            new object[] { "Assets/_SmallWorld/Scenes/04_StoryRoute.unity", new[] { "Player HUD", "Interaction UI", "Prompt", "Stage 15 Story Route" } }
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

        [TestCase("Assets/_SmallWorld/Scenes/00_Boot.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/03_FirstMemory.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/04_StoryRoute.unity")]
        public void SerializedLegacyTextSupportsKoreanAndLongCopyWithoutHorizontalClipping(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(texts, Is.Not.Empty);
            foreach (Text text in texts)
            {
                Assert.That(text.font, Is.Not.Null, text.name + " has no serialized fallback font.");
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12), text.name + " is unreadably small.");
                string value = text.text ?? string.Empty;
                Assert.That(value.IndexOf('\uFFFD'), Is.EqualTo(-1));
                int hangul = value.Count(character => character >= '\uAC00' && character <= '\uD7A3');
                if (hangul < 20 && !value.Contains("\n")) continue;
                Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap),
                    text.name + " can run outside its card at 1280x720.");
                float height = Mathf.Abs(text.rectTransform.rect.height) > 0.1f
                    ? Mathf.Abs(text.rectTransform.rect.height)
                    : Mathf.Abs(text.rectTransform.sizeDelta.y);
                Assert.That(height, Is.GreaterThanOrEqualTo(text.fontSize * 2f),
                    text.name + " has no room for 2-4 lines of long Korean copy.");
            }
        }

        [Test]
        public void ExistingSceneApplicatorCoversTheWholePlayableSceneSet()
        {
            Type applicator = Type.GetType("SmallWorld.Editor.ExistingSceneUiBaselineApplicator, Assembly-CSharp-Editor");
            Assert.That(applicator, Is.Not.Null);
            FieldInfo targets = applicator.GetField("TargetScenes", BindingFlags.Public | BindingFlags.Static);
            Assert.That(targets, Is.Not.Null);
            string[] paths = targets.GetValue(null) as string[];
            Assert.That(paths, Is.Not.Null);
            CollectionAssert.AreEquivalent(SceneInventory.Cast<object[]>().Select(item => (string)item[0]), paths);
            Assert.That(applicator.GetMethod("ApplyToLoadedScene", BindingFlags.Public | BindingFlags.Static), Is.Not.Null,
                "The UI baseline must remain idempotently applicable without regenerating gameplay scenes.");
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void MenuAndRealityRoomHaveNoPlaceholderOrBrokenUserText(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(texts.Length, Is.GreaterThan(5));
            string[] forbidden = { "Lorem", "Paused", "Press Esc", "No route records", "Button" };
            foreach (Text text in texts.Where(item => !string.IsNullOrWhiteSpace(item.text)))
            {
                Assert.That(text.font, Is.Not.Null, text.name + " has no Korean-capable project font.");
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(12), text.name + " is too small to read.");
                Assert.That(text.text.IndexOf('\uFFFD'), Is.EqualTo(-1),
                    text.name + " exposes a Unicode replacement character.");
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
                if (!IsRuntimePopulatedDialogueChoice(button))
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
        public void ButtonsInTheSamePanelHaveDistinctHitAreas(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (IGrouping<Transform, Button> group in buttons.GroupBy(button => button.transform.parent))
            {
                Button[] siblings = group.ToArray();
                for (int first = 0; first < siblings.Length; first++)
                for (int second = first + 1; second < siblings.Length; second++)
                {
                    RectTransform a = siblings[first].transform as RectTransform;
                    RectTransform b = siblings[second].transform as RectTransform;
                    if (a == null || b == null || !a.gameObject.activeSelf || !b.gameObject.activeSelf) continue;
                    if (AreMutuallyExclusiveDialogueControls(siblings[first], siblings[second])) continue;
                    if (IsRuntimeMutuallyExclusiveDialogueControl(siblings[first], siblings[second])) continue;
                    Rect aRect = AnchoredRect(a);
                    Rect bRect = AnchoredRect(b);
                    Assert.That(aRect.Overlaps(bRect), Is.False,
                        siblings[first].name + " overlaps " + siblings[second].name + " in " + scene + ".");
                }
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
                "Stage 8 Record UI", "Stage 9 Photo Puzzle UI"
            };
            foreach (string name in modalRoots)
            {
                GameObject item = GameObject.Find(name) ?? FindInactive(name);
                Assert.That(item, Is.Not.Null);
                Assert.That(item.GetComponent<CanvasGroup>() ?? item.GetComponentInChildren<CanvasGroup>(true),
                    Is.Not.Null, name + " needs explicit interactable/raycast ownership.");
            }

            Type panelType = Type.GetType(
                "SmallWorld.Save.Stage10.Integration.Stage10ManualSavePanel, SmallWorld.Save.Stage10.Integration");
            Assert.That(panelType, Is.Not.Null);
            Component savePanel = Object.FindFirstObjectByType(panelType, FindObjectsInactive.Include) as Component;
            Assert.That(savePanel, Is.Not.Null);
            SerializedProperty panel = new SerializedObject(savePanel).FindProperty("panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.objectReferenceValue, Is.TypeOf<CanvasGroup>(),
                "Stage 10 save input ownership belongs to its serialized panel CanvasGroup.");
        }

        private static bool IsRuntimePopulatedDialogueChoice(Button button)
        {
            if (!button.name.StartsWith("Choice ", StringComparison.Ordinal)) return false;
            Transform current = button.transform;
            while (current != null)
            {
                if (current.name == "Stage 7 Dialogue UI") return true;
                current = current.parent;
            }
            return false;
        }

        private static bool AreMutuallyExclusiveDialogueControls(Button first, Button second)
        {
            bool advanceAndChoice = first.name == "Advance Button" && second.name.StartsWith("Choice ", StringComparison.Ordinal) ||
                                    second.name == "Advance Button" && first.name.StartsWith("Choice ", StringComparison.Ordinal);
            return advanceAndChoice && IsRuntimePopulatedDialogueChoice(
                first.name.StartsWith("Choice ", StringComparison.Ordinal) ? first : second);
        }

        private static Rect AnchoredRect(RectTransform rect)
        {
            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f) size = rect.sizeDelta;
            Vector2 bottomLeft = rect.anchoredPosition - Vector2.Scale(size, rect.pivot);
            return new Rect(bottomLeft, size);
        }

        private static bool IsRuntimeMutuallyExclusiveDialogueControl(Button first, Button second)
        {
            bool advanceAndChoice = first.name == "Advance Button" && second.name.StartsWith("Choice ", StringComparison.Ordinal)
                                    || second.name == "Advance Button" && first.name.StartsWith("Choice ", StringComparison.Ordinal);
            return advanceAndChoice && IsUnder(first.transform, "Stage 7 Dialogue UI") &&
                   IsUnder(second.transform, "Stage 7 Dialogue UI");
        }

        private static bool IsUnder(Transform current, string ancestor)
        {
            while (current != null)
            {
                if (current.name == ancestor) return true;
                current = current.parent;
            }
            return false;
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
