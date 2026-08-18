using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.Save.Stage10.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15ScreenshotUsageAcceptanceTests
    {
        private const string StoryScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private const string RealityScene = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";

        [Test]
        public void EntryDoorsAndInteractiveDoorLeavesShareAReadablePhysicalAlignment()
        {
            EditorSceneManager.OpenScene(StoryScene);
            for (int room = 0; room < 8; room++)
            {
                Transform left = Require($"Route Room {room} Entry Door Left").transform;
                Transform right = Require($"Route Room {room} Entry Door Right").transform;
                Transform lintel = Require($"Route Room {room} Entry Door Lintel").transform;
                Assert.That(left.position.x, Is.EqualTo(-right.position.x).Within(0.02f));
                Assert.That(lintel.position.x, Is.EqualTo(0f).Within(0.02f));
                Assert.That(left.position.z, Is.EqualTo(right.position.z).Within(0.02f));
                Assert.That(lintel.position.z, Is.EqualTo(left.position.z).Within(0.02f));
                Assert.That(right.position.x - left.position.x, Is.InRange(3.5f, 5.5f));
                Assert.That(lintel.position.y, Is.GreaterThan(left.position.y));
            }

            MonoBehaviour[] doors = Behaviours("SmallWorld.Flow.Stage15StoryActionInteractable")
                .Where(item => item.name.StartsWith("Doorway -", StringComparison.Ordinal)).ToArray();
            Assert.That(doors, Is.Not.Empty);
            foreach (MonoBehaviour action in doors)
            {
                Renderer renderer = action.GetComponent<Renderer>();
                Collider collider = action.GetComponent<Collider>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(collider, Is.Not.Null);
                Assert.That(Vector3.Distance(renderer.bounds.center, collider.bounds.center), Is.LessThan(0.05f),
                    action.name + " interaction is detached from its visible door leaf.");
                Assert.That(collider.bounds.size.y, Is.EqualTo(renderer.bounds.size.y).Within(0.08f));
            }
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void EnvironmentalNameplatesNeverDominateTheArrivalView(int width, int height)
        {
            EditorSceneManager.OpenScene(StoryScene);
            Camera camera = Object.FindFirstObjectByType<Camera>();
            Assert.That(camera, Is.Not.Null);
            camera.aspect = width / (float)height;
            for (int room = 0; room < 8; room++)
            {
                Transform hub = RequirePrefix($"{room:00} ").transform;
                Transform arrival = hub.GetComponentsInChildren<Transform>(true)
                    .First(item => item.name == "Arrival");
                camera.transform.SetPositionAndRotation(arrival.position, arrival.rotation);

                TextMesh sign = Require($"Route Room {room} Entrance Sign").GetComponent<TextMesh>();
                AssertViewportShare(camera, sign.GetComponent<Renderer>().bounds, 0.28f, 0.12f, sign.name);
                foreach (string step in new[] { "Dialogue", "Puzzle", "Memory" })
                    Assert.That(GameObject.Find($"Route Room {room} {step} Highlight"), Is.Null,
                        "Debug highlight frames must be replaced by lighting and proximity prompts.");
            }
        }

        [Test]
        public void ManualSaveOwnsAFullScreenModalLayerAndExposesOutcomeAndSlotMetadata()
        {
            EditorSceneManager.OpenScene(RealityScene);
            Stage10ManualSavePanel save = Object.FindFirstObjectByType<Stage10ManualSavePanel>(FindObjectsInactive.Include);
            Assert.That(save, Is.Not.Null);
            var serialized = new SerializedObject(save);
            CanvasGroup panel = serialized.FindProperty("panel")?.objectReferenceValue as CanvasGroup;
            Assert.That(panel, Is.Not.Null);
            RectTransform root = panel.transform as RectTransform;
            Assert.That(root.anchorMin, Is.EqualTo(Vector2.zero),
                "Save modal must dim and own the full screen, not float over visible gameplay/puzzle UI.");
            Assert.That(root.anchorMax, Is.EqualTo(Vector2.one));
            Image blocker = panel.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.raycastTarget && image.color.a >= 0.65f &&
                                         image.rectTransform.anchorMin == Vector2.zero &&
                                         image.rectTransform.anchorMax == Vector2.one);
            Assert.That(blocker, Is.Not.Null, "Save requires a full-screen opaque raycast blocker.");

            SerializedProperty feedbackProperty = serialized.FindProperty("feedbackText");
            Assert.That(feedbackProperty, Is.Not.Null,
                "Save UI must show explicit success/failure feedback.");
            Text feedback = feedbackProperty.objectReferenceValue as Text;
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.rectTransform.rect.height, Is.GreaterThanOrEqualTo(40f));
            SerializedProperty metadata = serialized.FindProperty("slotMetadataTexts");
            Assert.That(metadata, Is.Not.Null, "Each save slot must expose time, location and progress metadata.");
            Assert.That(metadata.arraySize, Is.EqualTo(3));
            for (int i = 0; i < metadata.arraySize; i++)
            {
                Text slot = metadata.GetArrayElementAtIndex(i).objectReferenceValue as Text;
                Assert.That(slot, Is.Not.Null, $"Save slot {i + 1} has no visible metadata row.");
                Assert.That(slot.fontSize, Is.GreaterThanOrEqualTo(16));
                Assert.That(slot.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            }
        }

        [Test]
        public void StoryRouteRecordsHaveAProgressSummaryInsteadOfAPermanentEmptyPanel()
        {
            Type controller = RequireType("SmallWorld.Flow.StoryRouteController");
            PropertyInfo summary = controller.GetProperty("CurrentRecordsMessage",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(summary, Is.Not.Null,
                "Tab records need a runtime progress summary API instead of always drawing EmptyRecordsMessage.");

            GameObject owner = new GameObject("Story Route Records Contract");
            try
            {
                Component instance = owner.AddComponent(controller);
                controller.GetMethod("UpdateGuidance")?.Invoke(instance,
                    new object[] { "프롤로그 · 하얀 방", "유나와 대화한다", string.Empty });
                string message = summary.GetValue(instance) as string;
                Assert.That(message, Does.Contain("프롤로그"));
                Assert.That(message, Does.Contain("유나"));
                Assert.That(message, Does.Contain("다음"),
                    "Records must show chapter, completed/current action and next objective after progress.");
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EveryStoryActionProvidesKoreanInteractionAndNextActionGuidance()
        {
            EditorSceneManager.OpenScene(StoryScene);
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            MethodInfo next = guidance.GetMethod("NextObjective", BindingFlags.Public | BindingFlags.Static);
            Assert.That(next, Is.Not.Null);
            int checkedActions = 0;
            foreach (MonoBehaviour action in Behaviours("SmallWorld.Flow.Stage15StoryActionInteractable"))
            {
                var serialized = new SerializedObject(action);
                string prompt = serialized.FindProperty("prompt")?.stringValue;
                Assert.That(prompt, Is.Not.Empty, action.name + " has no discoverable interaction copy.");
                Assert.That(ContainsHangul(prompt), Is.True);
                checkedActions++;
            }
            Assert.That(checkedActions, Is.EqualTo(150));
        }

        private static void AssertViewportShare(Camera camera, Bounds bounds, float maxWidth, float maxHeight, string name)
        {
            var points = new List<Vector3>();
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 point = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3(x, y, z)));
                if (point.z > 0f) points.Add(point);
            }
            if (points.Count == 0) return;
            float occupiedWidth = points.Max(point => point.x) - points.Min(point => point.x);
            float occupiedHeight = points.Max(point => point.y) - points.Min(point => point.y);
            Assert.That(occupiedWidth, Is.LessThanOrEqualTo(maxWidth), name + " occupies too much screen width.");
            Assert.That(occupiedHeight, Is.LessThanOrEqualTo(maxHeight), name + " occupies too much screen height.");
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            Assert.That(renderers, Is.Not.Empty);
            Bounds result = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) result.Encapsulate(renderers[i].bounds);
            return result;
        }

        private static bool ContainsHangul(string value) =>
            !string.IsNullOrEmpty(value) && value.Any(character => character >= '\uAC00' && character <= '\uD7A3');

        private static IEnumerable<MonoBehaviour> Behaviours(string fullName) =>
            Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item != null && item.GetType().FullName == fullName);

        private static IEnumerable<GameObject> SceneObjects() =>
            Resources.FindObjectsOfTypeAll<GameObject>().Where(item => item != null && item.scene.IsValid());

        private static GameObject Require(string name)
        {
            GameObject result = SceneObjects().FirstOrDefault(item => item.name == name);
            Assert.That(result, Is.Not.Null, "Missing screenshot usage contract object: " + name);
            return result;
        }

        private static GameObject RequirePrefix(string prefix)
        {
            GameObject result = SceneObjects().FirstOrDefault(item => item.name.StartsWith(prefix, StringComparison.Ordinal));
            Assert.That(result, Is.Not.Null);
            return result;
        }

        private static Type RequireType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            return type;
        }
    }
}
