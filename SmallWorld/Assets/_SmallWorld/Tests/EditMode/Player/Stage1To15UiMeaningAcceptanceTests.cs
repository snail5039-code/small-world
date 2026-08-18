using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SmallWorld.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage1To15UiMeaningAcceptanceTests
    {
        private static readonly string[] Scenes =
        {
            "Assets/_SmallWorld/Scenes/00_Boot.unity",
            "Assets/_SmallWorld/Scenes/01_MainMenu.unity",
            "Assets/_SmallWorld/Scenes/02_RealityRoom.unity",
            "Assets/_SmallWorld/Scenes/03_FirstMemory.unity",
            "Assets/_SmallWorld/Scenes/04_StoryRoute.unity"
        };

        private static readonly object[] ModalBackdrops =
        {
            new object[] { Scenes[1], new[] { "Settings Backdrop", "Loading Backdrop" } },
            new object[] { Scenes[2], new[]
            {
                "Inspection Backdrop", "History Backdrop", "Pause Backdrop", "Record Backdrop",
                "Settings Backdrop", "Photo Puzzle Backdrop", "Loading Backdrop"
            } }
        };

        [TestCaseSource(nameof(Scenes))]
        public void PlayerFacingTextContainsNoLegacyEnglishOrReplacementGlyph(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            string[] forbidden =
            {
                "LOADING", "NEW GAME", "QUIT", "ESC  -  RETURN TO MENU", "Paused", "Press Esc",
                "No route records", "Lorem"
            };
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                string value = text.text ?? string.Empty;
                Assert.That(value.IndexOf('\uFFFD'), Is.EqualTo(-1), text.name + " contains U+FFFD.");
                foreach (string legacy in forbidden)
                    Assert.That(value.Trim(), Is.Not.EqualTo(legacy),
                        scene + " retains player-facing legacy/debug text: " + legacy);
            }
        }

        [TestCaseSource(nameof(Scenes))]
        public void SerializedUserTextMeetsMinimumTypeAndLongKoreanWrapContract(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                string value = text.text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(value)) continue;
                Assert.That(text.font, Is.Not.Null, text.name + " has no glyph source.");
                Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(14),
                    text.name + " is below the minimum body size at 720p.");
                Assert.That(value.IndexOf('\uFFFD'), Is.EqualTo(-1), text.name + " contains a missing glyph.");

                int hangulCount = value.Count(character => character >= '\uAC00' && character <= '\uD7A3');
                bool longKorean = hangulCount >= 20 || value.Count(character => character == '\n') >= 1;
                if (!longKorean) continue;
                Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap),
                    text.name + " truncates long Korean instead of wrapping it.");
                RectTransform rect = text.rectTransform;
                float height = Mathf.Abs(rect.rect.height) > 0.1f ? Mathf.Abs(rect.rect.height) : Mathf.Abs(rect.sizeDelta.y);
                Assert.That(height, Is.GreaterThanOrEqualTo(text.fontSize * 2f),
                    text.name + " has no room for the required 2-4 line Korean copy.");
            }
        }

        [TestCase("Assets/_SmallWorld/Scenes/01_MainMenu.unity")]
        [TestCase("Assets/_SmallWorld/Scenes/02_RealityRoom.unity")]
        public void ButtonLabelsUseButtonTypeScaleAndStayInsideTheirHitTargets(string scene)
        {
            EditorSceneManager.OpenScene(scene);
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Text label = button.GetComponentInChildren<Text>(true);
                Assert.That(label, Is.Not.Null, button.name + " has no label object.");
                if (string.IsNullOrWhiteSpace(label.text)) continue;
                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(16),
                    button.name + " label is below the 720p button minimum.");

                Bounds labelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                    button.transform, label.rectTransform);
                Rect buttonRect = ((RectTransform)button.transform).rect;
                Assert.That(labelBounds.min.x, Is.GreaterThanOrEqualTo(buttonRect.xMin - 0.5f));
                Assert.That(labelBounds.max.x, Is.LessThanOrEqualTo(buttonRect.xMax + 0.5f));
                Assert.That(labelBounds.min.y, Is.GreaterThanOrEqualTo(buttonRect.yMin - 0.5f));
                Assert.That(labelBounds.max.y, Is.LessThanOrEqualTo(buttonRect.yMax + 0.5f),
                    button.name + " label escapes its clickable background.");
            }
        }

        [TestCaseSource(nameof(ModalBackdrops))]
        public void ModalSurfacesHaveOpaqueDimBackgroundsThatOwnPointerInput(string scene, string[] names)
        {
            EditorSceneManager.OpenScene(scene);
            foreach (string name in names)
            {
                GameObject backdrop = Require(name);
                Image image = backdrop.GetComponent<Image>();
                Assert.That(image, Is.Not.Null, name + " has no visual dim surface.");
                Assert.That(image.color.a, Is.GreaterThanOrEqualTo(0.65f),
                    name + " does not separate modal information from gameplay.");
                Assert.That(image.raycastTarget, Is.True,
                    name + " permits click-through into the lower input layer.");
            }
        }

        [Test]
        public void RealityRoomUiExposesACompleteKoreanInformationHierarchy()
        {
            EditorSceneManager.OpenScene(Scenes[2]);
            string[] titles = { "Pause Title", "Settings Title", "History Title", "Tab Title" };
            foreach (string name in titles)
            {
                Text title = Require(name).GetComponent<Text>();
                Assert.That(title, Is.Not.Null);
                Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(20), name + " is not visually a title.");
                Assert.That(ContainsHangul(title.text), Is.True, name + " is not meaningful Korean UI.");
            }

            Text inspectionTitle = Require("Inspection Title").GetComponent<Text>();
            InspectionView inspection = Object.FindFirstObjectByType<InspectionView>(FindObjectsInactive.Include);
            Assert.That(inspection, Is.Not.Null);
            SerializedProperty runtimeTitle = new SerializedObject(inspection).FindProperty("titleText");
            Assert.That(runtimeTitle?.objectReferenceValue, Is.SameAs(inspectionTitle),
                "The empty inspection title is only valid as a runtime-populated binding.");
            Assert.That(inspectionTitle.fontSize, Is.GreaterThanOrEqualTo(20));

            string[] functionalLabels =
            {
                "Close Save Panel", "Resume Button", "Pause Settings Button", "Apply Button", "Cancel Button",
                "Close Record Button", "Close Photo Puzzle Button", "Inspection Close Button"
            };
            foreach (string name in functionalLabels)
            {
                Button button = Require(name).GetComponent<Button>();
                Assert.That(button, Is.Not.Null);
                Text label = button.GetComponentInChildren<Text>(true);
                Assert.That(label, Is.Not.Null);
                Assert.That(ContainsHangul(label.text), Is.True, name + " has no Korean action label.");
                Assert.That(MinimumSize(button.transform as RectTransform), Is.GreaterThanOrEqualTo(44f));
            }
        }

        [Test]
        public void InactiveRuntimeTemplatesAreTheOnlyPermittedEmptyOrPlaceholderUi()
        {
            EditorSceneManager.OpenScene(Scenes[2]);
            foreach (GameObject item in SceneObjects())
            {
                if (!IsVisuallyExposed(item)) continue;
                Assert.That(item.name, Does.Not.Contain("Placeholder"),
                    item.name + " exposes an implementation placeholder in the active UI hierarchy.");
                Assert.That(item.name, Does.Not.Contain("Debug"));
            }

            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null || !string.IsNullOrWhiteSpace(label.text)) continue;
                Assert.That(button.name, Does.StartWith("Choice "));
                Assert.That(IsUnder(button.transform, "Stage 7 Dialogue UI"), Is.True,
                    button.name + " is an unexplained empty button rather than a runtime dialogue template.");
            }
        }

        private static float MinimumSize(RectTransform rect)
        {
            Assert.That(rect, Is.Not.Null);
            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f) size = rect.sizeDelta;
            return Mathf.Min(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static bool ContainsHangul(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Any(character => character >= '\uAC00' && character <= '\uD7A3');
        }

        private static bool IsVisuallyExposed(GameObject item)
        {
            if (!item.activeInHierarchy) return false;
            Graphic graphic = item.GetComponent<Graphic>();
            if (graphic != null && graphic.enabled) return true;
            Renderer renderer = item.GetComponent<Renderer>();
            return renderer != null && renderer.enabled;
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

        private static GameObject Require(string name)
        {
            GameObject item = SceneObjects().FirstOrDefault(candidate => candidate.name == name);
            Assert.That(item, Is.Not.Null, "Missing UI meaning contract object: " + name);
            return item;
        }

        private static IEnumerable<GameObject> SceneObjects()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item != null && item.scene.IsValid());
        }
    }
}
