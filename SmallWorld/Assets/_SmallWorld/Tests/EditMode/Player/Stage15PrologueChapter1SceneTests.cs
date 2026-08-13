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

            Assert.That(GameObject.Find("Find Reality Developer Room Among Thousands Of Windows").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Arrange Monitors And Match AI Girl Waveforms").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Choose Reality Link Carry City And Return Home").GetComponent("StoryRouteInteractable"), Is.Not.Null);
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

            Assert.That(GameObject.Find("Investigate Changing Death Causes And Faceless Mourner").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Prove All Funeral Memories False").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Confirm Blank Name Or Create Another Girl And Return Home").GetComponent("StoryRouteInteractable"), Is.Not.Null);
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

            Assert.That(GameObject.Find("Investigate Faceless Office Identities").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Exchange Badges Recover Logs And Match Mirror Seats").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Choose Developer Record Escape And Return Home").GetComponent("StoryRouteInteractable"), Is.Not.Null);
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

            Assert.That(GameObject.Find("Mina Perfect Day Loop").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Break The Perfect Day Rules").GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(GameObject.Find("Preserve Or Tear The Photo And Return Home").GetComponent("StoryRouteInteractable"), Is.Not.Null);
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

        private static void AssertNode(SerializedProperty node, string id, string displayFragment)
        {
            Assert.That(node.FindPropertyRelative("Id").stringValue, Is.EqualTo(id));
            Assert.That(node.FindPropertyRelative("DisplayName").stringValue, Does.Contain(displayFragment));
            Assert.That(node.FindPropertyRelative("Arrival").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("DialogueEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("PuzzleEntry").objectReferenceValue, Is.Not.Null);
            Assert.That(node.FindPropertyRelative("MemoryEntry").objectReferenceValue, Is.Not.Null);
        }
    }
}
