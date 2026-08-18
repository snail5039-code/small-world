using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15ProloguePlayerJourneyAcceptanceTests
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Physics.SyncTransforms();
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void ArrivalCompositionReadsAsAnOccupiedRoomBeforeAnyDebugGuidance(int width, int height)
        {
            Transform room = Require("00 Prologue - The White Room").transform;
            Transform arrival = room.Find("Arrival");
            Camera camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            camera.aspect = width / (float)height;
            camera.transform.SetPositionAndRotation(arrival.position, arrival.rotation);

            string[] roomSilhouettes =
            {
                "Route Room 0 Entry Door Left", "Route Room 0 Entry Door Right", "Route Room 0 Entry Door Lintel",
                "Furniture - PlaceSofa", "Prologue Coffee Table", "Prologue Bookshelf", "Character - MeetYuna"
            };
            int visible = 0;
            foreach (string name in roomSilhouettes)
            {
                Renderer renderer = Require(name).GetComponent<Renderer>();
                Vector3 point = camera.WorldToViewportPoint(renderer.bounds.center);
                if (point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f) visible++;
            }
            Assert.That(visible, Is.GreaterThanOrEqualTo(5),
                "The first frame must read as a furnished doorway and room, not an empty gray test space.");

            Assert.That(room.GetComponentsInChildren<TextMesh>(true).Length, Is.LessThanOrEqualTo(1),
                "World text must not replace the physical room composition.");
            Assert.That(SceneObjects().Count(item => item.name.Contains(" Highlight") ||
                                                     item.name.Contains(" Beacon") ||
                                                     item.name.StartsWith("Route Room 0 Path ")), Is.Zero,
                "Debug guidance geometry is visible in the player's prologue room.");
        }

        [Test]
        public void ArrivalCanReachAndFocusYunaThroughTheEntryDoorOpening()
        {
            Transform arrival = Require("00 Prologue - The White Room").transform.Find("Arrival");
            GameObject yuna = Require("Character - MeetYuna");
            Collider yunaCollider = yuna.GetComponent<Collider>();
            Assert.That(yunaCollider, Is.Not.Null);
            Assert.That(Vector3.Distance(arrival.position, yuna.transform.position), Is.InRange(7f, 9f));

            Vector3 target = yunaCollider.bounds.center;
            Vector3 origin = arrival.position + Vector3.up * 1.62f;
            Vector3 direction = target - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, direction.magnitude,
                ~0, QueryTriggerInteraction.Ignore);
            Collider firstBlocking = hits.OrderBy(hit => hit.distance).Select(hit => hit.collider)
                .FirstOrDefault(collider => collider != yunaCollider &&
                                            !collider.GetComponentsInParent<MonoBehaviour>(true).Any(component =>
                                                component != null && component.GetType().Name ==
                                                "Stage15StoryActionInteractable"));
            Assert.That(firstBlocking, Is.Null,
                "A wall/furnishing blocks the first sight and interaction route to Yuna: " + firstBlocking?.name);
        }

        [Test]
        public void FirstSuccessNamesThePhysicalNextActionAndKeepsItReachable()
        {
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            Type chapter = RequireType("SmallWorld.Save.Story.StoryChapterId");
            Type action = RequireType("SmallWorld.Flow.OpeningStoryAction");
            MethodInfo next = guidance.GetMethod("NextObjective", BindingFlags.Public | BindingFlags.Static);
            string message = (string)next.Invoke(null, new[]
            {
                Enum.Parse(chapter, "Prologue"), Enum.Parse(action, "MeetYuna"), (object)true
            });
            Assert.That(message, Does.Contain("소파"));

            GameObject yuna = Require("Character - MeetYuna");
            GameObject sofaAction = Require("Furniture - PlaceSofa");
            Assert.That(sofaAction.GetComponent<Collider>()?.enabled, Is.True);
            Assert.That(Vector3.Distance(yuna.transform.position, sofaAction.transform.position), Is.LessThan(9f),
                "The named next action is outside a readable one-room search radius.");
            string prompt = new SerializedObject(sofaAction.GetComponent("Stage15StoryActionInteractable"))
                .FindProperty("prompt")?.stringValue;
            Assert.That(prompt, Does.Contain("소파"));
        }

        [TestCase("QuestionMemoryDoor")]
        [TestCase("LeaveMemoryDoorClosed")]
        public void FinalPrologueActionPointsToTheUnlockedNextRoomDoor(string actionName)
        {
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            Type chapter = RequireType("SmallWorld.Save.Story.StoryChapterId");
            Type action = RequireType("SmallWorld.Flow.OpeningStoryAction");
            MethodInfo next = guidance.GetMethod("NextObjective", BindingFlags.Public | BindingFlags.Static);
            string message = (string)next.Invoke(null, new[]
            {
                Enum.Parse(chapter, "Prologue"), Enum.Parse(action, actionName), (object)true
            });
            Assert.That(message, Does.Match("(다음 방|이동|문)"),
                actionName + " currently sends the player back to the initial objective instead of the exit.");
            Assert.That(message, Does.Not.Contain("유나와 대화"));
        }

        [Test]
        public void InteriorDoorOpeningHasCharacterWidthAndAReachableNextRoomGate()
        {
            Transform left = Require("Prologue Interior Wall Left").transform;
            Transform right = Require("Prologue Interior Wall Right").transform;
            Transform lintel = Require("Prologue Interior Door Lintel").transform;
            Renderer leftRenderer = left.GetComponent<Renderer>();
            Renderer rightRenderer = right.GetComponent<Renderer>();
            float openingWidth = rightRenderer.bounds.min.x - leftRenderer.bounds.max.x;
            Assert.That(openingWidth, Is.GreaterThanOrEqualTo(1.2f));
            Assert.That(lintel.position.x, Is.EqualTo(0f).Within(0.02f));

            Vector3 openingCenter = new Vector3(0f, 0.9f, left.position.z);
            Collider[] blockers = Physics.OverlapBox(openingCenter, new Vector3(0.55f, 0.85f, 0.7f),
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                .Where(collider => collider.enabled && collider.bounds.size.y > 0.2f).ToArray();
            Assert.That(blockers, Is.Empty,
                "The visible interior doorway cannot be crossed by the player: " +
                string.Join(", ", blockers.Select(blocker => blocker.name)));

            GameObject gate = Require("Route Room 0 Next Room Gate");
            Assert.That(gate.GetComponent<Collider>()?.enabled, Is.True);
            Assert.That(gate.GetComponent("StoryRouteInteractable"), Is.Not.Null);
            Assert.That(gate.transform.position.z, Is.GreaterThan(left.position.z),
                "The next-room interaction must sit beyond the visible doorway.");
        }

        private static IEnumerable<GameObject> SceneObjects() =>
            Resources.FindObjectsOfTypeAll<GameObject>().Where(item => item != null && item.scene.IsValid());

        private static GameObject Require(string name)
        {
            GameObject item = SceneObjects().FirstOrDefault(candidate => candidate.name == name);
            Assert.That(item, Is.Not.Null, "Missing player journey object: " + name);
            return item;
        }

        private static Type RequireType(string fullName)
        {
            Type result = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
            Assert.That(result, Is.Not.Null, "Missing player journey type: " + fullName);
            return result;
        }
    }
}
