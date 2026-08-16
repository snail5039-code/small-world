using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15GuidanceHudPresentationTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void GuidanceLayoutKeepsReadableHierarchyInsideSafeArea(int width, int height)
        {
            Type routeType = RequireType("SmallWorld.Flow.StoryRouteController");
            object compact = CreateLayout(routeType, width, height, false);
            object arrival = CreateLayout(routeType, width, height, true);
            Rect compactPanel = Read<Rect>(compact, "Panel");
            Rect arrivalPanel = Read<Rect>(arrival, "Panel");

            AssertInsideScreen(compactPanel, width, height);
            AssertInsideScreen(arrivalPanel, width, height);
            Assert.That(compactPanel.xMin, Is.GreaterThanOrEqualTo(12f));
            Assert.That(compactPanel.yMin, Is.GreaterThanOrEqualTo(12f));
            Assert.That(compactPanel.width, Is.LessThan(width * 0.5f));
            Assert.That(compactPanel.height, Is.LessThanOrEqualTo(height * 0.18f));
            Assert.That(arrivalPanel.height, Is.GreaterThan(compactPanel.height));

            Rect title = Read<Rect>(arrival, "Title");
            Rect location = Read<Rect>(arrival, "Location");
            Rect objectiveLabel = Read<Rect>(arrival, "ObjectiveHeading");
            Rect objectiveText = Read<Rect>(arrival, "Objective");
            Rect dialogue = Read<Rect>(arrival, "Dialogue");
            AssertContained(arrivalPanel, title, "title");
            AssertContained(arrivalPanel, location, "location");
            AssertContained(arrivalPanel, objectiveLabel, "objective label");
            AssertContained(arrivalPanel, objectiveText, "objective text");
            AssertContained(arrivalPanel, dialogue, "arrival dialogue");
            Assert.That(location.xMin - title.xMax, Is.GreaterThanOrEqualTo(8f),
                "The location belongs beside the eyebrow and must not collide with it.");
            Assert.That(Mathf.Abs(location.yMin - title.yMin), Is.LessThanOrEqualTo(0.01f),
                "The eyebrow and location must share a stable top row.");
            AssertVerticalGap(location, objectiveLabel, 6f);
            AssertVerticalGap(objectiveLabel, objectiveText, 2f);
            AssertVerticalGap(objectiveText, dialogue, 6f);

            int titleFont = Read<int>(arrival, "TitleFont");
            int locationFont = Read<int>(arrival, "LocationFont");
            int objectiveFont = Read<int>(arrival, "ObjectiveFont");
            int dialogueFont = Read<int>(arrival, "DialogueFont");
            Assert.That(titleFont, Is.GreaterThanOrEqualTo(12));
            Assert.That(locationFont, Is.GreaterThanOrEqualTo(18));
            Assert.That(objectiveFont, Is.GreaterThanOrEqualTo(15));
            Assert.That(dialogueFont, Is.GreaterThanOrEqualTo(13));
            Assert.That(locationFont, Is.GreaterThan(objectiveFont));
            Assert.That(objectiveFont, Is.GreaterThanOrEqualTo(dialogueFont));
            Assert.That(objectiveText.height, Is.GreaterThanOrEqualTo(objectiveFont * 2f),
                "Long Korean objectives need at least two lines without clipping.");
            Assert.That(dialogue.height, Is.GreaterThanOrEqualTo(dialogueFont * 2f));
            Assert.That(Read<bool>(arrival, "HasDialogue"), Is.True);

            Rect hiddenDialogue = Read<Rect>(compact, "Dialogue");
            Assert.That(hiddenDialogue.height, Is.Zero,
                "The compact HUD must not reserve an empty arrival-dialogue row.");
            Assert.That(Read<bool>(compact, "HasDialogue"), Is.False);
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void GuidanceUsesHighContrastAndNeverOverlapsPause(int width, int height)
        {
            Type routeType = RequireType("SmallWorld.Flow.StoryRouteController");
            object layout = CreateLayout(routeType, width, height, true);
            Rect guidance = Read<Rect>(layout, "Panel");
            MethodInfo pauseMethod = routeType.GetMethod("RuntimeOverlayRect", BindingFlags.Static | BindingFlags.Public);
            Assert.That(pauseMethod, Is.Not.Null);
            Rect pause = (Rect)pauseMethod.Invoke(null, new object[] { width, height, true });
            Assert.That(guidance.Overlaps(pause), Is.False);

            Color panel = ReadStatic<Color>(routeType, "GuidanceBackgroundColor");
            Color title = ReadStatic<Color>(routeType, "GuidanceAccentColor");
            Color label = title;
            Color body = ReadStatic<Color>(routeType, "GuidancePrimaryTextColor");
            Assert.That(Contrast(title, panel), Is.GreaterThanOrEqualTo(3f));
            Assert.That(Contrast(label, panel), Is.GreaterThanOrEqualTo(3f));
            Assert.That(Contrast(body, panel), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(title, Is.Not.EqualTo(body), "Title and body need visible hierarchy, not one flat style.");
        }

        [Test]
        public void LayoutCalculationDoesNotMutateRoomTravelOrRealityReturnState()
        {
            GameObject routeObject = GameObject.Find("Stage 15 Story Route");
            Assert.That(routeObject, Is.Not.Null);
            Component route = routeObject.GetComponent("StoryRouteController");
            Type type = route.GetType();
            int activeBefore = (int)type.GetProperty("ActiveNodeIndex").GetValue(route);
            string locationBefore = (string)type.GetProperty("CurrentLocation").GetValue(route);
            string objectiveBefore = (string)type.GetProperty("CurrentObjective").GetValue(route);

            CreateLayout(type, 1280, 720, false);
            CreateLayout(type, 1920, 1080, true);

            Assert.That((int)type.GetProperty("ActiveNodeIndex").GetValue(route), Is.EqualTo(activeBefore));
            Assert.That((string)type.GetProperty("CurrentLocation").GetValue(route), Is.EqualTo(locationBefore));
            Assert.That((string)type.GetProperty("CurrentObjective").GetValue(route), Is.EqualTo(objectiveBefore));
            Assert.That(type.GetMethod("HandleRoomBrowse", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
            Assert.That(type.GetMethod("ReturnToRealityRoomAsync", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        }

        private static object CreateLayout(Type routeType, int width, int height, bool dialogue)
        {
            MethodInfo method = routeType.GetMethod("GuidanceLayout", BindingFlags.Static | BindingFlags.Public);
            Assert.That(method, Is.Not.Null,
                "Expose a pure GuidanceLayout(width, height, hasArrivalDialogue) contract for resolution QA.");
            return method.Invoke(null, new object[] { width, height, dialogue });
        }

        private static T Read<T>(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property != null) return (T)property.GetValue(target);
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, type.Name + "." + name + " layout member is missing.");
            return (T)field.GetValue(target);
        }

        private static T ReadStatic<T>(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, type.Name + "." + name + " static contract is missing.");
            return (T)property.GetValue(null);
        }

        private static Type RequireType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            Assert.Fail(fullName + " is not loaded.");
            return null;
        }

        private static void AssertInsideScreen(Rect rect, int width, int height)
        {
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(height));
        }

        private static void AssertContained(Rect panel, Rect child, string label)
        {
            Assert.That(panel.Contains(child.min), Is.True, label + " begins outside the HUD panel.");
            Assert.That(panel.Contains(child.max - Vector2.one * 0.01f), Is.True,
                label + " clips outside the HUD panel.");
        }

        private static void AssertVerticalGap(Rect upper, Rect lower, float minimum)
        {
            Assert.That(lower.yMin - upper.yMax, Is.GreaterThanOrEqualTo(minimum));
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
