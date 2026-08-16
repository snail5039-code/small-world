using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15ScreenshotVisualRegressionTests
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private static readonly string[] KoreanRoomNames =
        {
            "프롤로그", "네 번째 자리", "마지막 승강장", "완벽한 하루", "얼굴 없는 사무실",
            "장례식 없는 묘지", "창문 안의 도시", "아무것도 남지 않은 하얀 방"
        };

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void StoryRouteContainsNoLegacyHudFingerprintAndUsesTheHighContrastTheme()
        {
            Assert.That(GameObject.Find("Gameplay HUD"), Is.Null,
                "StoryRoute must not retain the old duplicate Gameplay HUD.");
            Assert.That(SceneObjects("Player HUD").Count, Is.EqualTo(1));
            Assert.That(SceneObjects("Interaction UI").Count, Is.EqualTo(1));

            Text prompt = Require("Prompt").GetComponent<Text>();
            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.fontSize, Is.GreaterThanOrEqualTo(17));
            Assert.That(prompt.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(prompt.raycastTarget, Is.False);
            Assert.That(prompt.GetComponent<Outline>(), Is.Not.Null,
                "The prompt needs an outline instead of the old unbacked gray text.");

            Type controller = RequireType("SmallWorld.Flow.StoryRouteController");
            Assert.That(ReadStatic<string>(controller, "GuidanceTitle"), Is.EqualTo("이야기 안내"));
            Assert.That(ReadStatic<string>(controller, "GuidanceObjectiveTitle"), Is.EqualTo("현재 목표"));
            Color surface = ReadStatic<Color>(controller, "GuidanceBackgroundColor");
            Color accent = ReadStatic<Color>(controller, "GuidanceAccentColor");
            Color body = ReadStatic<Color>(controller, "GuidancePrimaryTextColor");
            Assert.That(surface.a, Is.GreaterThanOrEqualTo(0.88f));
            Assert.That(Contrast(accent, surface), Is.GreaterThanOrEqualTo(3f));
            Assert.That(Contrast(body, surface), Is.GreaterThanOrEqualTo(4.5f));

            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Assert.That(text.text, Does.Not.Contain("Paused"));
                Assert.That(text.text, Does.Not.Contain("Press Esc"));
                Assert.That(text.text, Does.Not.Contain("No route records"));
                Assert.That(text.text, Does.Not.Contain("�"));
            }
        }

        [Test]
        public void PrologueMaintainsMinimumAndAverageNavigableIllumination()
        {
            GameObject hub = Require("00 Prologue - The White Room");
            Light[] roomLights = hub.GetComponentsInChildren<Light>(true)
                .Where(light => light.type == LightType.Point && light.enabled).ToArray();
            Assert.That(roomLights.Length, Is.GreaterThanOrEqualTo(3));

            var values = new List<float>();
            foreach (float x in new[] { -6f, 0f, 6f })
            foreach (float zOffset in new[] { -10f, 0f, 10f })
                values.Add(IlluminationProxy(roomLights, new Vector3(x, 1.5f, zOffset)));

            Assert.That(values.Min(), Is.GreaterThanOrEqualTo(0.25f),
                "A traversable part of the prologue falls into unreadable darkness.");
            Assert.That(values.Average(), Is.GreaterThanOrEqualTo(0.65f));
            Assert.That(RenderSettings.ambientIntensity, Is.GreaterThanOrEqualTo(0.7f));
            Assert.That(Luminance(RenderSettings.ambientLight), Is.GreaterThanOrEqualTo(0.16f));

            Vector3 yuna = Require("Character - MeetYuna").transform.position + Vector3.up;
            Vector3 objective = Require("Prologue First Objective Label").transform.position;
            Assert.That(IlluminationProxy(roomLights, yuna), Is.GreaterThanOrEqualTo(1.2f));
            Assert.That(IlluminationProxy(roomLights, objective), Is.GreaterThanOrEqualTo(1.2f));
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void RoomSignsAreKoreanReadableFrontFacingAndInsideTheArrivalView(int width, int height)
        {
            Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            Assert.That(camera, Is.Not.Null);
            camera.aspect = width / (float)height;

            for (int room = 0; room < 8; room++)
            {
                GameObject hub = Require($"{room:00} " + EnglishRoomName(room));
                Transform arrival = hub.transform.Find("Arrival");
                TextMesh sign = Require($"Route Room {room} Entrance Sign").GetComponent<TextMesh>();
                Assert.That(arrival, Is.Not.Null);
                Assert.That(sign, Is.Not.Null);
                Assert.That(sign.text, Does.Contain(KoreanRoomNames[room]));
                Assert.That(sign.text, Does.Not.Contain("Chapter"));
                Assert.That(sign.text, Does.Not.Contain("Prologue"));
                Assert.That(sign.fontSize, Is.GreaterThanOrEqualTo(64));
                Assert.That(sign.characterSize, Is.GreaterThanOrEqualTo(0.09f));
                Assert.That(Quaternion.Angle(sign.transform.rotation, Quaternion.identity), Is.LessThan(0.1f));

                camera.transform.SetPositionAndRotation(arrival.position, arrival.rotation);
                Vector3 viewport = camera.WorldToViewportPoint(sign.GetComponent<Renderer>().bounds.center);
                Assert.That(viewport.z, Is.GreaterThan(0f), sign.name + " is behind the arrival camera.");
                Assert.That(viewport.x, Is.InRange(0.08f, 0.92f));
                Assert.That(viewport.y, Is.InRange(0.08f, 0.92f));

                Renderer wall = hub.transform.Find($"Route Room {room} Forward Occlusion Wall")
                    ?.GetComponent<Renderer>();
                Assert.That(wall, Is.Not.Null);
                Assert.That(Contrast(sign.color, wall.sharedMaterial.color), Is.GreaterThanOrEqualTo(3f));
            }
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void PrologueYunaAndObjectiveAreVisibleWithoutWorldTextOverlap(int width, int height)
        {
            GameObject hub = Require("00 Prologue - The White Room");
            Transform arrival = hub.transform.Find("Arrival");
            Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            camera.aspect = width / (float)height;
            camera.transform.SetPositionAndRotation(arrival.position, arrival.rotation);

            Renderer yuna = Require("Character - MeetYuna").GetComponent<Renderer>();
            Renderer objective = Require("Prologue First Objective Label").GetComponent<Renderer>();
            Vector3 yunaView = camera.WorldToViewportPoint(yuna.bounds.center);
            Vector3 objectiveView = camera.WorldToViewportPoint(objective.bounds.center);
            Assert.That(yunaView.z, Is.GreaterThan(0f));
            Assert.That(yunaView.x, Is.InRange(0.08f, 0.92f));
            Assert.That(yunaView.y, Is.InRange(0.08f, 0.92f));
            Assert.That(objectiveView.z, Is.GreaterThan(0f));
            Assert.That(objectiveView.x, Is.InRange(0.08f, 0.92f));
            Assert.That(objectiveView.y, Is.InRange(0.08f, 0.92f));

            TextMesh[] labels = hub.GetComponentsInChildren<TextMesh>(true);
            for (int first = 0; first < labels.Length; first++)
            for (int second = first + 1; second < labels.Length; second++)
                Assert.That(labels[first].GetComponent<Renderer>().bounds.Intersects(
                    labels[second].GetComponent<Renderer>().bounds), Is.False,
                    labels[first].name + " overlaps " + labels[second].name + ".");
        }

        private static float IlluminationProxy(IEnumerable<Light> lights, Vector3 point)
        {
            float result = 0f;
            foreach (Light light in lights)
            {
                float normalized = Vector3.Distance(light.transform.position, point) / light.range;
                float attenuation = Mathf.Clamp01(1f - normalized);
                result += light.intensity * attenuation * attenuation * Mathf.Max(0.25f, Luminance(light.color));
            }
            return result;
        }

        private static List<GameObject> SceneObjects(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>().Where(item =>
                item != null && item.scene.IsValid() && item.name == name).ToList();
        }

        private static GameObject Require(string name)
        {
            GameObject result = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(item =>
                item != null && item.scene.IsValid() && item.name == name);
            Assert.That(result, Is.Not.Null, name + " is missing.");
            return result;
        }

        private static Type RequireType(string fullName)
        {
            Type result = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
            Assert.That(result, Is.Not.Null);
            return result;
        }

        private static T ReadStatic<T>(Type type, string property)
        {
            PropertyInfo info = type.GetProperty(property, BindingFlags.Static | BindingFlags.Public);
            Assert.That(info, Is.Not.Null);
            return (T)info.GetValue(null);
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

        private static string EnglishRoomName(int room)
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
