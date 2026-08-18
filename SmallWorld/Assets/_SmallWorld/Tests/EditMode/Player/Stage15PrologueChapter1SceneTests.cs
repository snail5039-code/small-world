using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI.Stage7;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15PrologueChapter1SceneTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
            Time.timeScale = 1f;
            DialogueCursorMode.RequestGameplay();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            DialogueCursorMode.RequestUi();
        }

        [Test]
        public void StoryRoute_IntegratesLastPlatformLandmarksBetweenChaptersOneAndThree()
        {
            GameObject route = GameObject.Find("Stage 15 Story Route");

            Assert.That(route, Is.Not.Null);
            Component controller = route.GetComponent("StoryRouteController");
            Assert.That(controller, Is.Not.Null);
            Assert.That(route.GetComponent("StoryRouteProgressAdapter"), Is.Not.Null);

            SerializedProperty nodes = new SerializedObject(controller).FindProperty("nodes");
            Assert.That(nodes, Is.Not.Null);
            Assert.That(nodes.arraySize, Is.GreaterThanOrEqualTo(4));
            AssertNode(nodes.GetArrayElementAtIndex(0), "prologue", "Prologue");
            AssertNode(nodes.GetArrayElementAtIndex(1), "chapter-1", "Fourth Place");
            AssertNode(nodes.GetArrayElementAtIndex(2), "chapter-2", "Last Platform");
            AssertNode(nodes.GetArrayElementAtIndex(3), "chapter-3", "Perfect Day");
            AssertNode(nodes.GetArrayElementAtIndex(4), "chapter-4", "Faceless Office");
            AssertNode(nodes.GetArrayElementAtIndex(5), "chapter-5", "Cemetery Without a Funeral");
            AssertNode(nodes.GetArrayElementAtIndex(6), "chapter-6", "City in the Window");
            AssertNode(nodes.GetArrayElementAtIndex(7), "final-chapter", "White Room With Nothing Left");
        }

        [Test]
        public void StoryRoute_FinalChapterStopsAtChoicePreparationBoundary()
        {
            string[] requiredObjects =
            {
                "Living House Floor", "Living House Wall Of Faces", "Living House Victim Face 1",
                "Living House Victim Face 6", "Living House Reaching Hand 1", "Living House Reaching Hand 6",
                "Living Memory Furniture", "Memory Furniture Preserve Marker", "Memory Furniture Destroy Marker",
                "Management AI Core - Fourth Place", "Management AI Core - Last Platform",
                "Management AI Core - Perfect Day", "Management AI Core - Faceless Office",
                "Management AI Core - Cemetery Without A Funeral", "Management AI Core - City In The Window",
                "Reality Developer Body Silhouette", "Reality Connection Cable 1", "Reality Connection Cable 3",
                "Cable State Maintained", "Cable State Partially Cut", "Cable State City Power Cut",
                "Original White Room Floor", "First White Room Chair - Player",
                "First White Room Chair - Opposite", "Old Computer",
                "Dialogue Transformation Stage 1 - Girl Form",
                "Dialogue Transformation Stage 2 - Girl Developer Overlap",
                "Dialogue Transformation Stage 3 - Reality Developer Form",
                "Final Choice Readiness Conditions UI", "Final Choice Preparation Gate - Locked",
                "Final Choice Readiness Inspector", "No Ending Execution Boundary", "Survive Living House And Review Memory Preservation",
                "Review Management Cores And Reality Cable State", "Complete White Room Transformation Dialogue"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from the final chapter.");

            Assert.That(GameObject.Find("Final Choice Readiness Conditions UI").GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(GameObject.Find("Final Choice Preparation Gate - Locked").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("No Ending Execution Boundary").GetComponent("StoryRouteInteractable"), Is.Null);

            string[] forbiddenExecutableChoices =
            {
                "Program Exit Choice", "Connect Dollhouse Choice", "Remain With Girl Choice",
                "Become New Administrator Choice", "Send Girl To Reality Choice",
                "Restore Victims And Distribute Memories Choice"
            };
            foreach (string choiceName in forbiddenExecutableChoices)
                Assert.That(GameObject.Find(choiceName), Is.Null, choiceName + " must not exist before ending implementation.");
        }

        [Test]
        public void StoryRoute_NewGameSpawnFacesPrologueInsideOccludedRoom()
        {
            GameObject player = GameObject.Find("First Person Player");
            GameObject cameraObject = GameObject.Find("Player Camera");
            Assert.That(player, Is.Not.Null);
            Assert.That(cameraObject, Is.Not.Null);
            Assert.That(player.transform.position.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(player.transform.position.y, Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(player.transform.position.z, Is.EqualTo(-13f).Within(0.001f));
            Assert.That(Quaternion.Angle(player.transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(cameraObject.transform.forward.z, Is.GreaterThan(0.99f));
            Camera camera = cameraObject.GetComponent<Camera>();
            Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));

            string[] occlusionObjects =
            {
                "Route Room 0 Left Sight Wall", "Route Room 0 Right Sight Wall",
                "Route Room 0 Arrival Back Wall", "Route Room 0 Forward Occlusion Wall",
                "Route Room 0 Ceiling", "Route Room 0 Entry Light",
                "Route Room 7 Left Sight Wall", "Route Room 7 Right Sight Wall",
                "Route Room 7 Arrival Back Wall", "Route Room 7 Forward Occlusion Wall",
                "Route Room 7 Ceiling", "Route Room 7 Entry Light"
            };
            foreach (string objectName in occlusionObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is required to hide adjacent chapter skeletons.");
        }

        [Test]
        public void StoryRoute_AllChapterArrivalsRemainInsideTheirOwnOccludedRooms()
        {
            Component controller = GameObject.Find("Stage 15 Story Route").GetComponent("StoryRouteController");
            SerializedProperty nodes = new SerializedObject(controller).FindProperty("nodes");
            Assert.That(nodes.arraySize, Is.EqualTo(8));
            for (int i = 0; i < nodes.arraySize; i++)
            {
                Transform arrival = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("Arrival").objectReferenceValue as Transform;
                Assert.That(arrival, Is.Not.Null);
                Assert.That(arrival.position.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(arrival.position.y, Is.EqualTo(0.05f).Within(0.001f));
                Assert.That(arrival.position.z, Is.EqualTo(i * 36f - 13f).Within(0.001f));
                Assert.That(GameObject.Find($"Route Room {i} Ceiling"), Is.Not.Null);
            }
        }

        [Test]
        public void StoryRoute_AllRoomsProvideDistinctWayfindingAndNonBlockingPathMarkers()
        {
            var floorColors = new System.Collections.Generic.HashSet<Color>();
            for (int i = 0; i < 8; i++)
            {
                GameObject hub = GameObject.Find($"{i:00} " + new[]
                {
                    "Prologue - The White Room", "Chapter 1 - The Fourth Place", "Chapter 2 - Last Platform",
                    "Chapter 3 - A Perfect Day", "Chapter 4 - Faceless Office", "Chapter 5 - Cemetery Without a Funeral",
                    "Chapter 6 - City in the Window", "Final Chapter - The White Room With Nothing Left"
                }[i]);
                Assert.That(hub, Is.Not.Null);
                Renderer floor = hub.transform.Find("Hub Floor").GetComponent<Renderer>();
                Renderer wall = hub.transform.Find($"Route Room {i} Left Sight Wall").GetComponent<Renderer>();
                Assert.That(floor.sharedMaterial.color, Is.Not.EqualTo(wall.sharedMaterial.color));
                floorColors.Add(floor.sharedMaterial.color);

                Assert.That(GameObject.Find($"Route Room {i} Entrance Sign").GetComponent<TextMesh>(), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {i} Objective Light").GetComponent<Light>(), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {i} Dialogue Highlight"), Is.Null);
                Assert.That(GameObject.Find($"Route Room {i} Puzzle Highlight"), Is.Null);
                Assert.That(GameObject.Find($"Route Room {i} Memory Highlight"), Is.Null);
                Assert.That(GameObject.Find($"Route Room {i} Path 1-1"), Is.Null);
            }
            Assert.That(floorColors.Count, Is.EqualTo(8));
        }

        [Test]
        public void StoryRoute_EveryOpeningActionHasExactlyOnePlayableSceneStation()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var actions = new System.Collections.Generic.HashSet<string>();
            string[] expected = null;
            int stationCount = 0;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetType().Name != "Stage15StoryActionInteractable") continue;
                stationCount++;
                SerializedObject serialized = new SerializedObject(behaviour);
                SerializedProperty progress = serialized.FindProperty("progress");
                SerializedProperty action = serialized.FindProperty("action");
                SerializedProperty prompt = serialized.FindProperty("prompt");
                Assert.That(progress.objectReferenceValue, Is.Not.Null, behaviour.name + " has no progress adapter.");
                Assert.That(action, Is.Not.Null, behaviour.name + " has no serialized story action.");
                Assert.That(prompt, Is.Not.Null);
                expected ??= action.enumNames;
                Assert.That(prompt.stringValue, Does.Not.Contain(action.enumNames[action.enumValueIndex]),
                    behaviour.name + " exposes an internal enum name instead of a story-facing Korean prompt.");
                Assert.That(actions.Add(action.enumNames[action.enumValueIndex]), Is.True,
                    action.enumNames[action.enumValueIndex] + " is connected more than once.");
            }

            Assert.That(expected, Is.Not.Null);
            Assert.That(stationCount, Is.EqualTo(expected.Length),
                "Every OpeningStoryAction must have exactly one reachable scene station.");
            foreach (string actionName in expected)
                Assert.That(actions, Does.Contain(actionName), actionName + " is not connected to the scene.");

            string[] decorativeChapterSummaries =
            {
                "Dohyeon And Route Map", "Mina Perfect Day Loop", "Investigate Faceless Office Identities",
                "Investigate Changing Death Causes And Faceless Mourner",
                "Find Reality Developer Room Among Thousands Of Windows",
                "Survive Living House And Review Memory Preservation"
            };
            foreach (string summary in decorativeChapterSummaries)
                Assert.That(GameObject.Find(summary).GetComponent("StoryRouteInteractable"), Is.Null,
                    summary + " must not bypass the ordered story actions.");

            Assert.That(GameObject.Find("Route Room 2 Interaction Gallery Floor"), Is.Null,
                "Ordered actions must be represented by story props, not a station gallery.");
            Assert.That(GameObject.Find("Route Room 7 Interaction Gallery Floor"), Is.Null,
                "The final chapter must preserve a readable room instead of a station gallery.");
        }

        [Test]
        public void StoryRoute_EveryRoomReadsAsAPlaceInsteadOfADebugPropGallery()
        {
            string[][] roomEnvironment =
            {
                new[] { "Prologue Family Sofa", "Prologue Coffee Table", "Prologue Bookshelf", "Prologue Living Rug" },
                new[] { "Chapter 1 Kitchen Counter", "Chapter 1 Wall Cabinet 1", "Chapter 1 Hall Divider" },
                new[] { "Chapter 2 Platform Bench 1", "Chapter 2 Station Column 1", "Last Platform Concourse" },
                new[] { "Chapter 3 Cafe Counter", "Chapter 3 Cafe Table 1", "Chapter 3 Cafe Seat 1" },
                new[] { "Chapter 4 Work Desk 1", "Chapter 4 Desk Monitor 1", "Chapter 4 Filing Wall" },
                new[] { "Chapter 5 Procession Stone 1", "Chapter 5 Chapel Bench Left", "Nameless Grave 1" },
                new[] { "Chapter 6 Observation Console", "Chapter 6 Interior Window 1", "Scaled Reality City Basin" },
                new[] { "Final Living House Rib 1", "Final White Room Carpet", "First White Room Chair - Player" }
            };

            for (int room = 0; room < roomEnvironment.Length; room++)
            {
                Assert.That(GameObject.Find($"Route Room {room} Entry Door Left"), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Entry Door Right"), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Entry Door Lintel"), Is.Not.Null);
                foreach (string environmentObject in roomEnvironment[room])
                    Assert.That(GameObject.Find(environmentObject), Is.Not.Null,
                        $"Room {room} lacks its contextual environment object {environmentObject}.");

                Assert.That(GameObject.Find($"Route Room {room} Path 1-1"), Is.Null,
                    "Floor arrows expose debug routing instead of environmental navigation.");
                Assert.That(GameObject.Find($"Route Room {room} Dialogue Highlight"), Is.Null,
                    "Giant glowing frames must not replace natural light and proximity prompts.");
            }

            GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            Assert.That(sceneObjects.Count(item => item.scene.IsValid() && item.name.EndsWith(" Beacon")), Is.Zero,
                "Per-action glowing beacon poles expose the implementation grid instead of the story space.");
        }

        [Test]
        public void StoryRoute_UsesSemanticPropsAndKeepsThePrologueObjectiveReadable()
        {
            GameObject yuna = GameObject.Find("Character - MeetYuna");
            GameObject yunaFace = GameObject.Find("Yuna Face");
            GameObject yunaLight = GameObject.Find("Prologue Yuna Key Light");
            GameObject arrival = GameObject.Find("00 Prologue - The White Room").transform.Find("Arrival").gameObject;

            Assert.That(yuna, Is.Not.Null);
            Assert.That(yuna.GetComponent<CapsuleCollider>(), Is.Not.Null);
            Assert.That(yunaFace, Is.Not.Null);
            Assert.That(GameObject.Find("Yuna Left Arm"), Is.Not.Null);
            Assert.That(GameObject.Find("Yuna Right Arm"), Is.Not.Null);
            Assert.That(GameObject.Find("Yuna Left Eye"), Is.Not.Null);
            Assert.That(GameObject.Find("Yuna Right Eye"), Is.Not.Null);
            Assert.That(GameObject.Find("Prologue First Objective Label"), Is.Null,
                "The objective belongs in the HUD and proximity prompt, not in giant world text.");
            Assert.That(yunaLight.GetComponent<Light>().intensity, Is.GreaterThan(2.5f));
            Assert.That(Vector3.Distance(arrival.transform.position, yuna.transform.position), Is.LessThan(9f));
            Assert.That(Vector3.Distance(arrival.transform.position, yuna.transform.position), Is.GreaterThan(7f),
                "Yuna must not spawn close enough to cover the first-person camera.");
            Assert.That(yuna.transform.localScale.x, Is.LessThan(0.7f));
            Assert.That(yuna.transform.localScale.y, Is.LessThan(1.4f));
            Assert.That(Mathf.Abs(yuna.transform.position.x - arrival.transform.position.x), Is.GreaterThan(4f),
                "The first objective must remain beside, not directly across, the central sight line.");
            Assert.That(yuna.transform.position.x, Is.GreaterThan(3.5f),
                "Yuna stays on the right side so the upper-left objective HUD never covers her silhouette.");
            Assert.That(yuna.transform.localScale.y, Is.InRange(0.75f, 1f),
                "Yuna needs a restrained human silhouette rather than an oversized capsule.");
            Assert.That(GameObject.Find("Prologue Interior Wall Left"), Is.Not.Null);
            Assert.That(GameObject.Find("Prologue Interior Wall Right"), Is.Not.Null);
            Assert.That(GameObject.Find("Prologue Interior Door Lintel"), Is.Not.Null);

            TextMesh prologueSign = GameObject.Find("Route Room 0 Entrance Sign").GetComponent<TextMesh>();
            Assert.That(prologueSign.transform.position.x, Is.Zero.Within(0.01f));
            Assert.That(prologueSign.transform.position.z, Is.InRange(-3f, 0f),
                "The prologue title must be readable from Arrival instead of shrinking on the far wall.");
            Assert.That(Mathf.Abs(prologueSign.transform.position.x), Is.LessThan(12f),
                "The room title needs safe horizontal margins and must not be clipped by a side wall.");

            foreach (TextMesh worldText in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                Assert.That(worldText.transform.rotation, Is.EqualTo(Quaternion.identity),
                    worldText.name + " is mirrored or tilted away from the route arrival.");
                Assert.That(worldText.fontSize, Is.EqualTo(48));
                Assert.That(worldText.characterSize, Is.LessThanOrEqualTo(0.04f),
                    worldText.name + " is too large for an environmental nameplate.");
                GameObject plate = GameObject.Find(worldText.name + " Backplate");
                Assert.That(plate, Is.Not.Null, worldText.name + " needs a dark contrast backplate.");
                Assert.That(plate.GetComponent<Collider>().enabled, Is.False,
                    worldText.name + " backplate must not obstruct the route.");
                Assert.That(Contrast(worldText.color, plate.GetComponent<Renderer>().sharedMaterial.color),
                    Is.GreaterThanOrEqualTo(3f), worldText.name + " needs readable contrast against its plate.");
            }

            string[] koreanWorldTitles =
            {
                "프롤로그 · 하얀 방", "1장 · 네 번째 자리", "2장 · 마지막 승강장", "3장 · 완벽한 하루",
                "4장 · 얼굴 없는 사무실", "5장 · 장례식 없는 묘지", "6장 · 창문 안의 도시",
                "최종장 · 아무것도 남지 않은 하얀 방"
            };
            for (int room = 0; room < koreanWorldTitles.Length; room++)
            {
                TextMesh title = GameObject.Find($"Route Room {room} Entrance Sign").GetComponent<TextMesh>();
                Assert.That(title.text, Is.EqualTo(koreanWorldTitles[room]));
                Assert.That(title.text, Does.Not.Contain("Chapter"));
                Assert.That(title.text, Does.Not.Contain("Prologue"));
            }

            Light fill = GameObject.Find("Prologue Route Fill Light").GetComponent<Light>();
            Assert.That(fill.intensity, Is.GreaterThanOrEqualTo(2.2f));
            Assert.That(fill.range, Is.GreaterThanOrEqualTo(18f));
            Assert.That(GameObject.Find("Prologue Warm Light").GetComponent<Light>().intensity,
                Is.GreaterThanOrEqualTo(2.5f));
            Assert.That(RenderSettings.ambientIntensity, Is.GreaterThanOrEqualTo(0.85f));
            Assert.That(RenderSettings.ambientLight.grayscale, Is.GreaterThanOrEqualTo(0.2f));
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(UnityEngine.Rendering.AmbientMode.Flat));

            Material yunaMaterial = yuna.GetComponent<Renderer>().sharedMaterial;
            Assert.That(yunaMaterial.IsKeywordEnabled("_EMISSION"), Is.True);
            Assert.That(yunaMaterial.GetColor("_EmissionColor").maxColorComponent, Is.GreaterThan(0.3f));
            Assert.That(GameObject.Find("Route Room 0 Path 1-1"), Is.Null);

            string[] representativeProps =
            {
                "Furniture - PlaceSofa", "Identity Item - FindKey", "Tableware - FindTeacup",
                "Document - FindPhotoFragment", "Clock - MatchDeveloperRoomTime",
                "Terminal - ArrangeMonitorLoop1", "Gravestone - InspectGravestoneBack",
                "Control Console - CutSomeRealityCables", "Management Core - DestroyManagementCore1",
                "Furniture - SitInFirstChair", "Terminal - ActivateOldComputer"
            };
            foreach (string prop in representativeProps)
                Assert.That(GameObject.Find(prop), Is.Not.Null, prop + " must be a readable semantic story prop.");

            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.GetType().Name != "Stage15StoryActionInteractable") continue;
                if (behaviour.name == "Character - MeetYuna") continue;
                int chapter = Mathf.RoundToInt(behaviour.transform.position.z / 36f);
                if (chapter >= 0)
                    Assert.That(Mathf.Abs(behaviour.transform.position.x), Is.GreaterThanOrEqualTo(10f),
                        behaviour.name + " blocks the central navigation aisle.");
            }
        }

        [Test]
        public void StoryRoute_PrologueArrivalShowsFirstObjectiveAndExitAsDifferentSignals()
        {
            GameObject objective = GameObject.Find("Character - MeetYuna");
            GameObject exit = GameObject.Find("Route Room 0 Next Room Gate");
            GameObject arrival = GameObject.Find("00 Prologue - The White Room").transform.Find("Arrival").gameObject;

            Assert.That(objective, Is.Not.Null);
            Assert.That(exit, Is.Not.Null);
            Assert.That(Vector3.Dot((objective.transform.position - arrival.transform.position).normalized, Vector3.forward), Is.GreaterThan(0.25f));
            Assert.That(Vector3.Distance(objective.transform.position, exit.transform.position), Is.GreaterThan(4f));
            Assert.That(GameObject.Find("Route Room 0 Objective Light").transform.position.z, Is.LessThan(exit.transform.position.z));
        }

        [Test]
        public void StoryRoute_ExposesSevenPreviousAndSevenNextRoomGatesWithClearKoreanPrompts()
        {
            GameObject realityReturn = GameObject.Find("Route Room 0 Reality Return Gate");
            Assert.That(realityReturn, Is.Not.Null);
            Component returnInteractable = realityReturn.GetComponent("StoryRouteRealityReturnInteractable");
            Assert.That(returnInteractable, Is.Not.Null);
            SerializedObject serializedReturn = new SerializedObject(returnInteractable);
            Assert.That(serializedReturn.FindProperty("route").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedReturn.FindProperty("prompt").stringValue, Does.Contain("현실방으로 돌아가기"));
            Assert.That(realityReturn.transform.position.x, Is.GreaterThan(8f));
            Assert.That(GameObject.Find("Route Room 0 Reality Return Sign"), Is.Null,
                "The return instruction is provided by the gate proximity prompt, not giant world text.");

            int previousCount = 0;
            int nextCount = 0;
            for (int room = 0; room < 8; room++)
            {
                GameObject previous = GameObject.Find($"Route Room {room} Previous Room Gate");
                GameObject next = GameObject.Find($"Route Room {room} Next Room Gate");

                if (room == 0)
                    Assert.That(previous, Is.Null, "The prologue has no earlier room.");
                else
                {
                    previousCount++;
                    AssertTravelGate(previous, room - 1, "이전 방으로 돌아가기");
                    Assert.That(previous.transform.position.x, Is.LessThan(-10f));
                }

                if (room == 7)
                    Assert.That(next, Is.Null, "The final chapter has no later room before endings are implemented.");
                else
                {
                    nextCount++;
                    AssertTravelGate(next, room + 1, "다음 방으로 이동하기");
                    Assert.That(next.transform.position.x, Is.GreaterThan(10f));
                }
            }

            Assert.That(previousCount, Is.EqualTo(7));
            Assert.That(nextCount, Is.EqualTo(7));
        }

        private static void AssertTravelGate(GameObject gate, int expectedTarget, string expectedPrompt)
        {
            Assert.That(gate, Is.Not.Null);
            Component interactable = gate.GetComponent("StoryRouteInteractable");
            Assert.That(interactable, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(interactable);
            Assert.That(serialized.FindProperty("targetNodeIndex").intValue, Is.EqualTo(expectedTarget));
            Assert.That(serialized.FindProperty("prompt").stringValue, Does.Contain(expectedPrompt));
        }

        [Test]
        public void StoryRoute_RestoresEveryCurrentChapterAndFallsBackSafely()
        {
            GameObject routeObject = GameObject.Find("Stage 15 Story Route");
            Component controller = routeObject.GetComponent("StoryRouteController");
            Component adapter = routeObject.GetComponent("StoryRouteProgressAdapter");
            MethodInfo map = adapter.GetType().GetMethod("CurrentChapterNodeIndex",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo restore = controller.GetType().GetMethod("RestoreToNodeOrPrologue",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(map, Is.Not.Null);
            Assert.That(restore, Is.Not.Null);

            System.Type chapterType = map.GetParameters()[0].ParameterType;
            GameObject player = GameObject.Find("First Person Player");
            for (int chapterValue = 0; chapterValue <= 7; chapterValue++)
            {
                object chapter = System.Enum.ToObject(chapterType, chapterValue);
                int mappedIndex = (int)map.Invoke(null, new[] { chapter });
                int restoredIndex = (int)restore.Invoke(controller, new object[] { mappedIndex });
                Assert.That(mappedIndex, Is.EqualTo(chapterValue));
                Assert.That(restoredIndex, Is.EqualTo(chapterValue));
                Assert.That(player.transform.position.z, Is.EqualTo(chapterValue * 36f - 13f).Within(0.001f));
            }

            object invalidChapter = System.Enum.ToObject(chapterType, 999);
            int fallbackIndex = (int)map.Invoke(null, new[] { invalidChapter });
            int restoredFallback = (int)restore.Invoke(controller, new object[] { fallbackIndex });
            Assert.That(fallbackIndex, Is.Zero);
            Assert.That(restoredFallback, Is.Zero);
            Assert.That(player.transform.position.z, Is.EqualTo(-13f).Within(0.001f));
        }

        [Test]
        public void StoryRoute_IntegratesCityWindowPuzzlesRealityBranchesChaseRewardsAndFinalChapterConnection()
        {
            string[] requiredObjects =
            {
                "Almost Complete Dollhouse Final Room", "Scaled Reality City Basin",
                "Miniature City Building 1", "Miniature City Building 12",
                "Thousands Of Running Program Windows 1-1", "Thousands Of Running Program Windows 12-4",
                "Repeated Time Clue", "Furniture Layout Clue", "Reverse Rain Direction Clue",
                "Reality Developer Room Candidate 1", "Reality Developer Room Candidate 2",
                "Reality Developer Room Correct", "Developer Monitor Sequence 1", "Developer Monitor Sequence 4",
                "Live Player Back View On Final Monitor", "Player Back Silhouette",
                "Management AI Voice Waveform", "Girl Previous Dialogue Waveform",
                "Perfectly Matching Future Girl Segment", "Future Girl Management AI Revelation",
                "Reality Link Maintain Developer Body", "Reality Link Cut Some Cables",
                "Reality Link Cut Entire City Power", "All City Windows Open Simultaneously",
                "All Miniature People Stare At Player", "Folding Buildings Form Giant House",
                "Carry Collapsing City Chase", "Return To Original House Door",
                "Reward Completed Miniature City", "Reward Reality Developer Stopped Wristwatch",
                "Reward Final Room Front Door", "Final Chapter Living House Connection",
                "Final Chapter Management AI Core Connection",
                "Find Reality Developer Room Among Thousands Of Windows",
                "Arrange Monitors And Match AI Girl Waveforms",
                "Choose Reality Link Carry City And Return Home"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from chapter 6.");

            Assert.That(GameObject.Find("Find Reality Developer Room Among Thousands Of Windows").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Arrange Monitors And Match AI Girl Waveforms").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Choose Reality Link Carry City And Return Home").GetComponent("StoryRouteInteractable"), Is.Null);
        }

        [Test]
        public void StoryRoute_IntegratesCemeteryContradictionsBlankNameBranchesAndChapterSixConnection()
        {
            string[] requiredObjects =
            {
                "Fog Cemetery Ground", "Small Funeral Hall", "Changing Cause Room 1 - Traffic Accident",
                "Changing Cause Room 2 - Hospital Experiment", "Changing Cause Room 3 - Suicide",
                "Changing Cause Room 4 - Program Deletion", "Funeral Photo 1", "Distant Faceless Figure 4",
                "Death Certificate 1", "Death Certificate 4", "Matching Letter Spacing And Print Error",
                "RESTORE HER Overlay Command", "Funeral Guestbook", "Guestbook Signature 1",
                "Cemetery Shadow Match 4", "Same Hand Movement Proof", "Empty Gravestone Front",
                "Carve A Name Instruction", "Empty Gravestone Back", "Memory Installation Date",
                "Final Empty Name Input", "Confirm Blank Name Truth Branch",
                "Entered Name Creates New Girl Loop Branch", "Return Home From Cemetery",
                "Chapter 6 City In The Window Connection", "Reward Empty Picture Frame",
                "Reward Nameless Gravestone Fragment", "Reward White Flower Vase",
                "Investigate Changing Death Causes And Faceless Mourner", "Prove All Funeral Memories False",
                "Confirm Blank Name Or Create Another Girl And Return Home"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from chapter 5.");

            Assert.That(GameObject.Find("Investigate Changing Death Causes And Faceless Mourner").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Prove All Funeral Memories False").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Confirm Blank Name Or Create Another Girl And Return Home").GetComponent("StoryRouteInteractable"), Is.Null);
        }

        [Test]
        public void StoryRoute_IntegratesFacelessOfficePuzzlesChoiceChaseAndReturnHome()
        {
            string[] requiredObjects =
            {
                "Windowless Developer Office", "Same Face Employee Desk 1", "Same Face Employee 4",
                "Girl Version Computer 1 - Prototype Girl", "Girl Version Computer 4 - Deleted Girl",
                "Employee Badge Authority Exchange", "Original Developer Badge", "Memory Researcher Badge",
                "System Administrator Badge", "Identity And Face Change Door", "Permission Locked Record Cabinet",
                "Contradictory Deleted Log Fragment 1", "Contradictory Deleted Log Fragment 4",
                "Invariant System Command", "Girl Deletion Record", "Girl Saved Into Developer Memory Record",
                "Mirror Meeting Room", "Mirror Showing Real Faces", "Reality Employee Seat 1",
                "Mirror Real Face Seat 4", "Composite Identity Revelation", "Trust Original Developer Record",
                "Trust Altered Developer Record", "Check Original Server Autonomous Choice", "End Of Shift Broadcast",
                "Erased Employee Faces Chase", "Badge Theft Chase Corridor", "Office Escape Door",
                "Return Home Interaction", "Reward Study Desk", "Reward Development Computer",
                "Reward Locked File Cabinet", "Investigate Faceless Office Identities",
                "Exchange Badges Recover Logs And Match Mirror Seats", "Choose Developer Record Escape And Return Home"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from chapter 4.");

            Assert.That(GameObject.Find("Investigate Faceless Office Identities").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Exchange Badges Recover Logs And Match Mirror Seats").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Choose Developer Record Escape And Return Home").GetComponent("StoryRouteInteractable"), Is.Null);
        }

        [Test]
        public void StoryRoute_IntegratesPerfectDayVillagePuzzlesAndFinalChoice()
        {
            string[] requiredObjects =
            {
                "Warm Village Cafe", "Sunny Village Park", "Perfect Day Arcade", "Riverside Walk",
                "Repeated Person And Dialogue Mark 1", "Menu Showing Her Favorites", "Flipped Menu Bitter Coffee",
                "Mina Bitter Coffee Cup", "Choice Graffiti", "Fourth Choice I Do Not Know What You Like",
                "Movable Park Shadow Stage 1", "Movable Park Shadow Stage 2", "Movable Park Shadow Stage 3",
                "Sunset Stage Light 3", "Yuna Previous Loop Appearance 3", "Evening Unlocked",
                "Perfect Date Photo", "Preserve Photo Choice", "Tear Photo Choice", "Mina Original Memory",
                "Return Home Door", "Mina Perfect Day Loop", "Break The Perfect Day Rules",
                "Preserve Or Tear The Photo And Return Home"
            };

            foreach (string objectName in requiredObjects)
                Assert.That(GameObject.Find(objectName), Is.Not.Null, objectName + " is missing from chapter 3.");

            Assert.That(GameObject.Find("Mina Perfect Day Loop").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Break The Perfect Day Rules").GetComponent("StoryRouteInteractable"), Is.Null);
            Assert.That(GameObject.Find("Preserve Or Tear The Photo And Return Home").GetComponent("StoryRouteInteractable"), Is.Null);
        }

        [Test]
        public void StoryRoute_TabOwnsAndRestoresPlayerInputAndCursor()
        {
            Component route = FindStoryRouteController();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.True);
            Assert.That(player.enabled, Is.False);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.True);

            Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
            Assert.That(player.enabled, Is.True);
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(DialogueCursorMode.RequestedVisible, Is.False);
        }

        [Test]
        public void StoryRoute_EscapePausesAndRestoresRuntimeState()
        {
            Component route = FindStoryRouteController();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();

            Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimePaused"), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.enabled, Is.False);

            Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.True);
            Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(player.enabled, Is.True);
        }

        [Test]
        public void StoryRoute_PausePresentationIsLocalizedCompactAndResponsive()
        {
            Component route = FindStoryRouteController();
            System.Type type = route.GetType();

            Assert.That(ReadStaticProperty<string>(type, "PauseTitle"), Is.EqualTo("일시정지"));
            Assert.That(ReadStaticProperty<string>(type, "PauseMessage"), Does.Contain("Esc"));
            Assert.That(ReadStaticProperty<string>(type, "PauseMessage"), Does.Contain("이야기"));
            Assert.That(ReadStaticProperty<string>(type, "RecordsTitle"), Is.EqualTo("기록"));

            MethodInfo layout = type.GetMethod("RuntimeOverlayRect",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(layout, Is.Not.Null);
            Rect fullHd = (Rect)layout.Invoke(null, new object[] { 1920, 1080, true });
            Rect compact = (Rect)layout.Invoke(null, new object[] { 640, 360, true });

            Assert.That(fullHd.width, Is.LessThanOrEqualTo(460f));
            Assert.That(fullHd.height, Is.LessThanOrEqualTo(120f));
            Assert.That(fullHd.xMin, Is.GreaterThan(1920f * 0.5f), "Pause must not cover the center view.");
            Assert.That(fullHd.xMax, Is.LessThanOrEqualTo(1920f));
            Assert.That(compact.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(compact.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(compact.xMax, Is.LessThanOrEqualTo(640f));
            Assert.That(compact.yMax, Is.LessThanOrEqualTo(360f));
        }

        [Test]
        public void StoryRoute_DoesNotStealInputWhileSavePanelOwnsIt()
        {
            Component route = FindStoryRouteController();
            FirstPersonPlayerController player = Object.FindFirstObjectByType<FirstPersonPlayerController>();
            var saveObject = new GameObject("Story Route Save Input Owner");
            CanvasGroup panel = saveObject.AddComponent<CanvasGroup>();
            Stage10ManualSavePanel savePanel = saveObject.AddComponent<Stage10ManualSavePanel>();
            savePanel.Configure(panel, null, null, null);
            savePanel.Configure(player);
            savePanel.Open();

            try
            {
                Assert.That(savePanel.IsOpen, Is.True);
                Assert.That(Invoke<bool>(route, "HandleTabPressed"), Is.False);
                Assert.That(Invoke<bool>(route, "HandleEscapePressed"), Is.False);
                Assert.That(ReadProperty<bool>(route, "IsRuntimeOverlayOpen"), Is.False);
                Assert.That(player.enabled, Is.False);
                Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            }
            finally
            {
                Object.DestroyImmediate(saveObject);
            }
        }

        private static Component FindStoryRouteController()
        {
            GameObject route = GameObject.Find("Stage 15 Story Route");
            Assert.That(route, Is.Not.Null);
            Component controller = route.GetComponent("StoryRouteController");
            Assert.That(controller, Is.Not.Null);
            return controller;
        }

        private static T Invoke<T>(Component target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName + " method is missing.");
            return (T)method.Invoke(target, null);
        }

        private static T ReadProperty<T>(Component target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName + " property is missing.");
            return (T)property.GetValue(target);
        }

        private static T ReadStaticProperty<T>(System.Type target, string propertyName)
        {
            PropertyInfo property = target.GetProperty(propertyName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, propertyName + " property is missing.");
            return (T)property.GetValue(null);
        }

        private static void AssertNode(SerializedProperty node, string id, string displayFragment)
        {
            Assert.That(node.FindPropertyRelative("Id").stringValue, Is.EqualTo(id));
            Assert.That(node.FindPropertyRelative("DisplayName").stringValue, Does.Contain(displayFragment));
            Assert.That(node.FindPropertyRelative("Arrival").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("DialogueEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("PuzzleEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("MemoryEntry").objectReferenceValue, Is.Not.Null);
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
