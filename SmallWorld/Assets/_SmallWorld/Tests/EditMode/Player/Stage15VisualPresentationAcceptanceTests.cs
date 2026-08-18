using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
            Light keyLight = RequireObject("Prologue Yuna Key Light").GetComponent<Light>();

            Assert.That(yuna.GetComponent<CapsuleCollider>(), Is.Not.Null,
                "The first objective must look like a character, not another cube.");
            Assert.That(face.GetComponent<Renderer>(), Is.Not.Null);
            Assert.That(GameObject.Find("Prologue First Objective Label"), Is.Null,
                "Yuna guidance belongs in the compact HUD and proximity prompt.");
            Assert.That(keyLight, Is.Not.Null);
            Assert.That(keyLight.intensity, Is.GreaterThan(2.5f));
            Assert.That(Vector3.Distance(arrival.transform.position, yuna.transform.position), Is.LessThan(9f),
                "Yuna must be immediately discoverable from the prologue spawn.");
            Assert.That(Vector3.Distance(arrival.transform.position, yuna.transform.position), Is.GreaterThan(7f));
            Assert.That(Mathf.Abs(yuna.transform.position.x - arrival.transform.position.x), Is.GreaterThan(4f),
                "Yuna belongs beside the initial sight line and must not cover the camera.");
            Assert.That(yuna.transform.localScale.x, Is.LessThan(0.7f));
            Assert.That(yuna.transform.localScale.y, Is.LessThan(1.4f));

            foreach (TextMesh worldText in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                Assert.That(worldText.transform.rotation, Is.EqualTo(Quaternion.identity),
                    worldText.name + " is mirrored, backwards or tilted away from room arrivals.");
            string[] bodyParts = { "Yuna Face", "Yuna Left Arm", "Yuna Right Arm", "Yuna Left Eye", "Yuna Right Eye" };
            foreach (string part in bodyParts)
                Assert.That(RequireObject(part).GetComponent<Renderer>(), Is.Not.Null, part + " must be visible.");
            Assert.That(RequireObject("Yuna Left Eye").transform.position.x,
                Is.LessThan(RequireObject("Yuna Right Eye").transform.position.x));
            Assert.That(RequireObject("Yuna Left Eye").transform.position.z,
                Is.LessThan(yuna.transform.position.z), "Eyes must sit on Yuna's front face toward the arrival.");
        }

        [Test]
        public void EnvironmentalNameplatesStayInsideWallMarginsAndRemainSmall()
        {
            TextMesh entrance = RequireObject("Route Room 0 Entrance Sign").GetComponent<TextMesh>();
            Assert.That(entrance.characterSize, Is.LessThanOrEqualTo(0.04f));

            for (int room = 0; room < 8; room++)
            {
                TextMesh sign = RequireObject($"Route Room {room} Entrance Sign").GetComponent<TextMesh>();
                Assert.That(Mathf.Abs(sign.transform.position.x), Is.LessThanOrEqualTo(11f));
                Assert.That(sign.characterSize, Is.LessThanOrEqualTo(0.04f),
                    sign.name + " dominates the room instead of reading as a nameplate.");
            }
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
            Transform prologueRoom = GameObject.Find("00 Prologue - The White Room").transform;
            foreach (MonoBehaviour behaviour in SceneBehaviours("SmallWorld.Flow.Stage15StoryActionInteractable"))
            {
                if (behaviour.name == "Character - MeetYuna") continue;
                float minimumAisleClearance = behaviour.transform.IsChildOf(prologueRoom) ? 2.5f : 10f;
                Assert.That(Mathf.Abs(behaviour.transform.position.x), Is.GreaterThanOrEqualTo(minimumAisleClearance),
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

        [Test]
        public void BrowseMovesOnlyAcrossUnlockedRoomsWithoutMutatingStoryOrSaveState()
        {
            Component route = RequireObject("Stage 15 Story Route").GetComponent("StoryRouteController");
            Component adapter = RequireObject("Stage 15 Story Route").GetComponent("StoryRouteProgressAdapter");
            Type progressType = RequireType("SmallWorld.Save.Story.StoryProgress");
            Type chapterType = RequireType("SmallWorld.Save.Story.StoryChapterId");
            Type saveType = RequireType("SmallWorld.Save.Stage10.SaveData");
            object progress = Activator.CreateInstance(progressType);
            object save = saveType.GetMethod("CreateNew", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            progressType.GetField("CurrentChapter").SetValue(progress, Enum.ToObject(chapterType, 3));
            SetField(adapter, "progress", progress);
            SetField(adapter, "save", save);
            route.GetType().GetMethod("BindProgressSource").Invoke(route, new object[] { adapter });
            route.GetType().GetMethod("RestoreToNodeOrPrologue").Invoke(route, new object[] { 3 });

            string progressBefore = JsonUtility.ToJson(progress);
            string saveBefore = JsonUtility.ToJson(save);
            Assert.That(InvokeBrowse(route, -1), Is.True);
            Assert.That(ReadProperty<int>(route, "ActiveNodeIndex"), Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(saveBefore));
            Assert.That(Convert.ToInt32(progressType.GetField("CurrentChapter").GetValue(progress)), Is.EqualTo(3));

            Type actionType = RequireType("SmallWorld.Flow.OpeningStoryAction");
            object pastRoomAction = Enum.Parse(actionType, "HearDohyeon");
            object result = adapter.GetType().GetMethod("PerformOpeningAction").Invoke(adapter, new[] { pastRoomAction });
            Assert.That((bool)result.GetType().GetProperty("Accepted").GetValue(result), Is.False,
                "Past-room props are review-only and cannot execute unfinished actions.");
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(progressBefore));
            Assert.That(JsonUtility.ToJson(save), Is.EqualTo(saveBefore));

            Assert.That(InvokeBrowse(route, 1), Is.True);
            Assert.That(ReadProperty<int>(route, "ActiveNodeIndex"), Is.EqualTo(3));
            Assert.That(InvokeBrowse(route, 1), Is.False, "The player cannot browse beyond the live chapter.");
        }

        [Test]
        public void RoomBrowseIsBlockedByOverlayAndSavePanelInputOwnership()
        {
            Component route = RequireObject("Stage 15 Story Route").GetComponent("StoryRouteController");
            route.GetType().GetMethod("RestoreToNodeOrPrologue").Invoke(route, new object[] { 2 });
            route.GetType().GetMethod("HandleEscapePressed").Invoke(route, null);
            Assert.That(InvokeBrowse(route, -1), Is.False);
            Assert.That(InvokeTravel(route, 1), Is.False,
                "A physical gate must not bypass pause-overlay input ownership.");
            route.GetType().GetMethod("HandleEscapePressed").Invoke(route, null);

            Type savePanelType = RequireType("SmallWorld.Save.Stage10.Integration.Stage10ManualSavePanel");
            var owner = new GameObject("visual-qa-save-owner");
            try
            {
                CanvasGroup canvas = owner.AddComponent<CanvasGroup>();
                Component panel = owner.AddComponent(savePanelType);
                MethodInfo configure = savePanelType.GetMethods().Single(method =>
                    method.Name == "Configure" && method.GetParameters().Length == 4);
                configure.Invoke(panel, new object[] { canvas, null, null, null });
                savePanelType.GetMethod("Open").Invoke(panel, null);
                SetField(route, "savePanel", panel);
                Assert.That((bool)savePanelType.GetProperty("IsOpen").GetValue(panel), Is.True);
                Assert.That(InvokeBrowse(route, -1), Is.False);
                Assert.That(InvokeTravel(route, 1), Is.False,
                    "A physical gate must not bypass save-panel input ownership.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EveryUnlockedRoomHasPhysicalPreviousAndNextTravelGates()
        {
            for (int room = 0; room < 8; room++)
            {
                GameObject hub = RequireObject($"{room:00} " + RoomName(room));
                if (room > 0)
                    Assert.That(hub.transform.Find($"Route Room {room} Previous Room Gate"), Is.Not.Null,
                        hub.name + " needs a visible physical return gate in addition to PageUp.");
                if (room < 7)
                    Assert.That(hub.transform.Find($"Route Room {room} Next Room Gate"), Is.Not.Null,
                        hub.name + " needs a visible forward gate.");
            }
        }

        [Test]
        public void PrologueHasExactlyOneInteractiveRealityRoomReturnGate()
        {
            GameObject room = RequireObject("00 Prologue - The White Room");
            GameObject gate = RequireObject("Route Room 0 Reality Return Gate");
            Type returnType = RequireType("SmallWorld.Flow.StoryRouteRealityReturnInteractable");

            Assert.That(gate.transform.parent, Is.EqualTo(room.transform));
            Assert.That(gate.GetComponent(returnType), Is.Not.Null);
            Assert.That(GameObject.Find("Route Room 0 Reality Return Sign"), Is.Null);
            int count = Resources.FindObjectsOfTypeAll(returnType).Cast<Component>()
                .Count(component => component != null && component.gameObject.scene.IsValid());
            Assert.That(count, Is.EqualTo(1), "Duplicate return gates can start overlapping scene transitions.");
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void GuidanceHudUsesCompactSafeHighContrastHierarchy(int width, int height)
        {
            Type controller = RequireType("SmallWorld.Flow.StoryRouteController");
            MethodInfo calculate = controller.GetMethod("GuidanceLayout", BindingFlags.Static | BindingFlags.Public);
            Assert.That(calculate, Is.Not.Null);
            object compact = calculate.Invoke(null, new object[] { width, height, false });
            object withDialogue = calculate.Invoke(null, new object[] { width, height, true });

            Rect compactPanel = ReadLayoutRect(compact, "Panel");
            Rect dialoguePanel = ReadLayoutRect(withDialogue, "Panel");
            Rect title = ReadLayoutRect(compact, "Title");
            Rect location = ReadLayoutRect(compact, "Location");
            Rect objectiveHeading = ReadLayoutRect(compact, "ObjectiveHeading");
            Rect objective = ReadLayoutRect(compact, "Objective");
            Rect dialogue = ReadLayoutRect(withDialogue, "Dialogue");

            Assert.That(compactPanel.xMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(compactPanel.yMin, Is.GreaterThanOrEqualTo(16f));
            Assert.That(compactPanel.xMax, Is.LessThanOrEqualTo(width - 16f));
            Assert.That(compactPanel.yMax, Is.LessThanOrEqualTo(height - 16f));
            Assert.That(compactPanel.width, Is.LessThanOrEqualTo(480f));
            Assert.That(compactPanel.width / width, Is.LessThan(0.38f), "The objective card must not dominate the view.");
            Assert.That(dialoguePanel.height, Is.GreaterThan(compactPanel.height));
            Assert.That(ReadLayout<bool>(compact, "HasDialogue"), Is.False);
            Assert.That(ReadLayout<bool>(withDialogue, "HasDialogue"), Is.True);
            Assert.That(ReadLayoutRect(compact, "Dialogue"), Is.EqualTo(Rect.zero));

            Assert.That(title.xMax, Is.LessThanOrEqualTo(location.xMin));
            Assert.That(title.yMin, Is.EqualTo(location.yMin).Within(0.01f));
            Assert.That(location.yMax, Is.LessThan(objectiveHeading.yMin));
            Assert.That(objectiveHeading.yMax, Is.LessThanOrEqualTo(objective.yMin));
            Assert.That(objective.yMax, Is.LessThan(dialogue.yMin));
            Assert.That(ReadLayout<int>(compact, "LocationFont"), Is.GreaterThan(ReadLayout<int>(compact, "TitleFont")));
            Assert.That(ReadLayout<int>(compact, "ObjectiveFont"), Is.GreaterThanOrEqualTo(15));
            Assert.That(ReadStatic<string>(controller, "GuidanceTitle"), Is.EqualTo("이야기 안내"));
            Assert.That(ReadStatic<string>(controller, "GuidanceObjectiveTitle"), Is.EqualTo("현재 목표"));

            Color background = ReadStatic<Color>(controller, "GuidanceBackgroundColor");
            Color accent = ReadStatic<Color>(controller, "GuidanceAccentColor");
            Color text = ReadStatic<Color>(controller, "GuidancePrimaryTextColor");
            Assert.That(background.a, Is.GreaterThanOrEqualTo(0.85f));
            Assert.That(accent.maxColorComponent, Is.GreaterThan(0.9f));
            Assert.That(text.grayscale, Is.GreaterThan(0.85f));
        }

        private static T ReadLayout<T>(object layout, string property)
        {
            PropertyInfo info = layout.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, property + " is missing from the guidance layout contract.");
            return (T)info.GetValue(layout);
        }

        private static Rect ReadLayoutRect(object layout, string property) => ReadLayout<Rect>(layout, property);

        [Test]
        public void SharedUiThemeKeepsInteractionPromptAndFeedbackDistinctWithoutDuplicateInputHints()
        {
            Type theme = RequireType("SmallWorld.UI.SmallWorldUiTheme");
            MethodInfo formatPrompt = theme.GetMethod("FormatInteractionPrompt", BindingFlags.Static | BindingFlags.Public);
            Assert.That(formatPrompt.Invoke(null, new object[] { "조사하기" }), Is.EqualTo("[E] 조사하기"));
            Assert.That(formatPrompt.Invoke(null, new object[] { "[E] 다음 방으로 이동하기" }),
                Is.EqualTo("[E] 다음 방으로 이동하기"));

            var owner = new GameObject("Shared UI Theme Contract");
            try
            {
                Text prompt = new GameObject("Prompt", typeof(RectTransform)).AddComponent<Text>();
                Text feedback = new GameObject("Feedback", typeof(RectTransform)).AddComponent<Text>();
                prompt.transform.SetParent(owner.transform, false);
                feedback.transform.SetParent(owner.transform, false);
                Type promptViewType = RequireType("SmallWorld.Player.InteractionPromptView");
                Component view = owner.AddComponent(promptViewType);
                promptViewType.GetMethod("Configure").Invoke(view, new object[] { prompt, feedback });

                Assert.That(prompt.fontSize, Is.GreaterThan(feedback.fontSize));
                Assert.That(prompt.fontStyle, Is.EqualTo(FontStyle.Bold));
                Assert.That(prompt.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(feedback.GetComponent<Outline>(), Is.Not.Null);
                Assert.That(prompt.rectTransform.anchorMin.y, Is.EqualTo(0.18f).Within(0.001f));
                Assert.That(feedback.rectTransform.anchorMin.y, Is.EqualTo(0.27f).Within(0.001f));
                Assert.That(feedback.rectTransform.anchorMin.y - prompt.rectTransform.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.08f), "Prompt and result feedback need separate bottom-center rows.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
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

        private static bool InvokeTravel(Component route, int index)
        {
            object[] arguments = { index, null };
            return (bool)route.GetType().GetMethod("TryTravelTo").Invoke(route, arguments);
        }

        private static T ReadProperty<T>(Component component, string property)
        {
            return (T)component.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)
                .GetValue(component);
        }

        private static void SetField(Component component, string field, object value)
        {
            FieldInfo info = component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null);
            info.SetValue(component, value);
        }

        private static bool InvokeBrowse(Component route, int direction)
        {
            object[] arguments = { direction, null };
            return (bool)route.GetType().GetMethod("HandleRoomBrowse").Invoke(route, arguments);
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
