using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15VisualPresentationAcceptanceTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void Prologue_FirstObjectiveMakesYunaVisibleAndReachableFromArrival()
        {
            GameObject room = RequireObject("00 Prologue - The White Room");
            GameObject arrival = RequireChild(room.transform, "Arrival").gameObject;
            GameObject yuna = RequireObject("Character - MeetYuna");
            GameObject face = RequireObject("Yuna Face");
            GameObject label = RequireObject("Prologue First Objective Label");
            Light keyLight = RequireObject("Prologue Yuna Key Light").GetComponent<Light>();

            Assert.That(yuna.GetComponent<CapsuleCollider>(), Is.Not.Null,
                "The first objective must look like a character, not another cube.");
            Assert.That(face.GetComponent<Renderer>(), Is.Not.Null);
            Assert.That(label.GetComponent<TextMesh>()?.text, Is.EqualTo("YUNA"));
            Assert.That(keyLight, Is.Not.Null);
            Assert.That(keyLight.intensity, Is.GreaterThan(2.5f));
            Assert.That(Vector3.Distance(arrival.transform.position, yuna.transform.position), Is.LessThan(9f),
                "Yuna must be immediately discoverable from the prologue spawn.");
        }

        [Test]
        public void EveryActionUsesOneSemanticPropWithoutRecreatingABoxGallery()
        {
            Type actionType = RequireType("SmallWorld.Flow.OpeningStoryAction");
            int expectedCount = Enum.GetValues(actionType).Length;
            var actions = new HashSet<int>();
            var semanticPrefixes = new HashSet<string>();

            foreach (MonoBehaviour behaviour in SceneBehaviours("SmallWorld.Flow.Stage15StoryActionInteractable"))
            {
                var serialized = new SerializedObject(behaviour);
                int action = serialized.FindProperty("action").intValue;
                Assert.That(actions.Add(action), Is.True,
                    Enum.GetName(actionType, action) + " is connected more than once.");
                Assert.That(behaviour.name, Does.Not.StartWith("Story Action"));
                Assert.That(behaviour.name, Does.Not.Match("^[0-9]{2} "),
                    behaviour.name + " exposes an implementation index instead of a meaningful prop.");
                Assert.That(behaviour.GetComponent<Renderer>(), Is.Not.Null);

                string prefix = behaviour.name.Split(new[] { " - " }, StringSplitOptions.None)[0];
                Assert.That(prefix, Is.Not.Empty);
                semanticPrefixes.Add(prefix);
            }

            Assert.That(actions.Count, Is.EqualTo(expectedCount));
            Assert.That(expectedCount, Is.EqualTo(150), "Adding actions requires updating the visible-scene contract.");
            Assert.That(semanticPrefixes.Count, Is.GreaterThanOrEqualTo(10),
                "All actions collapsing into a few repeated boxes is not an acceptable visual language.");
            for (int room = 0; room < 8; room++)
                Assert.That(GameObject.Find($"Route Room {room} Interaction Gallery Floor"), Is.Null);
        }

        [Test]
        public void ActionPropsKeepCentralAislesOpenAndSummaryMarkersCannotBypassProgress()
        {
            foreach (MonoBehaviour behaviour in SceneBehaviours("SmallWorld.Flow.Stage15StoryActionInteractable"))
            {
                if (behaviour.name == "Character - MeetYuna") continue;
                Assert.That(Mathf.Abs(behaviour.transform.position.x), Is.GreaterThanOrEqualTo(10f),
                    behaviour.name + " blocks the player's central route.");
            }

            foreach (MonoBehaviour behaviour in SceneBehaviours("SmallWorld.Flow.StoryRouteInteractable"))
            {
                var serialized = new SerializedObject(behaviour);
                SerializedProperty nodeId = serialized.FindProperty("nodeId");
                Assert.That(nodeId == null || string.IsNullOrEmpty(nodeId.stringValue), Is.True,
                    behaviour.name + " bypasses ordered actions through a summary marker.");
            }
        }

        [Test]
        public void EveryRoomHasReadableLightContrastAndAVisibleObjective()
        {
            var floorColors = new HashSet<Color>();
            for (int room = 0; room < 8; room++)
            {
                GameObject hub = GameObject.Find($"{room:00} " + RoomName(room));
                Assert.That(hub, Is.Not.Null);
                Renderer floor = RequireChild(hub.transform, "Hub Floor").GetComponent<Renderer>();
                Renderer wall = RequireChild(hub.transform, $"Route Room {room} Left Sight Wall").GetComponent<Renderer>();
                Light objective = RequireObject($"Route Room {room} Objective Light").GetComponent<Light>();

                Assert.That(floor.sharedMaterial.color, Is.Not.EqualTo(wall.sharedMaterial.color));
                Assert.That(objective, Is.Not.Null);
                Assert.That(objective.intensity, Is.GreaterThan(0f));
                floorColors.Add(floor.sharedMaterial.color);
            }
            Assert.That(floorColors.Count, Is.EqualTo(8));
        }

        [Test]
        public void PauseOverlayIsKoreanCompactAndEscapeRestoresTheStory()
        {
            Component route = RequireObject("Stage 15 Story Route").GetComponent("StoryRouteController");
            Type type = route.GetType();
            Assert.That(ReadStatic<string>(type, "PauseTitle"), Is.EqualTo("일시정지"));
            Assert.That(ReadStatic<string>(type, "PauseMessage"), Does.Contain("Esc"));
            Assert.That(ReadStatic<string>(type, "PauseMessage"), Does.Contain("이야기"));

            MethodInfo rectMethod = type.GetMethod("RuntimeOverlayRect", BindingFlags.Static | BindingFlags.Public);
            Assert.That(rectMethod, Is.Not.Null);
            Rect rect = (Rect)rectMethod.Invoke(null, new object[] { 1920, 1080, true });
            Assert.That(rect.width, Is.LessThanOrEqualTo(460f));
            Assert.That(rect.height, Is.LessThanOrEqualTo(120f));
            Assert.That(rect.xMin, Is.GreaterThan(960f));

            MethodInfo escape = type.GetMethod("HandleEscapePressed", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(escape, Is.Not.Null);
            escape.Invoke(route, null);
            Assert.That((bool)type.GetProperty("IsRuntimePaused").GetValue(route), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            escape.Invoke(route, null);
            Assert.That((bool)type.GetProperty("IsRuntimePaused").GetValue(route), Is.False);
            Assert.That(Time.timeScale, Is.GreaterThan(0f));
        }

        private static IEnumerable<MonoBehaviour> SceneBehaviours(string fullName)
        {
            return Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(item => item != null &&
                item.gameObject.scene.IsValid() && item.GetType().FullName == fullName);
        }

        private static GameObject RequireObject(string name)
        {
            GameObject result = GameObject.Find(name);
            Assert.That(result, Is.Not.Null, name + " is missing.");
            return result;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform result = parent.Find(name);
            Assert.That(result, Is.Not.Null, name + " is missing under " + parent.name + ".");
            return result;
        }

        private static Type RequireType(string fullName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(candidate => candidate != null);
            Assert.That(type, Is.Not.Null, fullName + " is not loaded.");
            return type;
        }

        private static T ReadStatic<T>(Type type, string property)
        {
            PropertyInfo info = type.GetProperty(property, BindingFlags.Static | BindingFlags.Public);
            Assert.That(info, Is.Not.Null);
            return (T)info.GetValue(null);
        }

        private static string RoomName(int room)
        {
            string[] names =
            {
                "Prologue - The White Room", "Chapter 1 - The Fourth Place", "Chapter 2 - Last Platform",
                "Chapter 3 - A Perfect Day", "Chapter 4 - Faceless Office",
                "Chapter 5 - Cemetery Without a Funeral", "Chapter 6 - City in the Window",
                "Final Chapter - The White Room With Nothing Left"
            };
            return names[room];
        }
    }
}
