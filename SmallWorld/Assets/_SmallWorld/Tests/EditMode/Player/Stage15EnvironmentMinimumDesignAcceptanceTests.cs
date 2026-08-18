using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15EnvironmentMinimumDesignAcceptanceTests
    {
        private const string ScenePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        private static readonly string[][] RoomLandmarks =
        {
            new[] { "Empty Dollhouse", "Placed Sofa Echo", "Character - MeetYuna", "Prologue Warm Light" },
            new[] { "Four Seat Dining Table", "The Empty Fourth Chair", "Manipulated Family Photo", "Rainy Apartment Light" },
            new[] { "Last Platform Concourse", "Arriving Last Train", "Faceless Passenger Shadow 1", "Broadcast Safe Light 1" },
            new[] { "Warm Village Cafe", "Park Bench", "Repeated Person And Dialogue Mark 1", "Perfect Day Warm Sun" },
            new[] { "Windowless Office West Wall", "Same Face Employee Desk 1", "Same Face Employee 1", "Faceless Office Fluorescent Light" },
            new[] { "Fog Cemetery Ground", "Small Funeral Hall", "Distant Faceless Figure 1", "Dense Fog Cemetery Light" },
            new[] { "Miniature City Building 1", "Developer Monitor Sequence 1", "Player Back Silhouette", "City In The Window Night Light" },
            new[] { "Living House Wall Of Faces", "First White Room Chair - Player", "Living House Victim Face 1", "Final Chapter White Room Light" }
        };

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        [Test]
        public void EveryRoomHasArchitectureFurnitureLifeAndAStorySilhouetteUnderItsOwnLighting()
        {
            for (int room = 0; room < RoomLandmarks.Length; room++)
            {
                GameObject hub = Require($"{room:00} ");
                foreach (string landmark in RoomLandmarks[room])
                {
                    GameObject item = Require(landmark);
                    Assert.That(item.transform.IsChildOf(hub.transform), Is.True,
                        landmark + " must belong to room " + room + ", not leak from another skeleton.");
                }

                Assert.That(Require($"Route Room {room} Left Sight Wall").GetComponent<Renderer>(), Is.Not.Null);
                Assert.That(Require($"Route Room {room} Ceiling").GetComponent<Renderer>(), Is.Not.Null);
                Assert.That(Require($"Route Room {room} Entry Light").GetComponent<Light>(), Is.Not.Null);
                Assert.That(Require($"Route Room {room} Objective Light").GetComponent<Light>(), Is.Not.Null);
            }
        }

        [Test]
        public void GuidanceGeometryStaysSubordinateAndNeverBecomesBlockingDebugScenery()
        {
            foreach (GameObject item in SceneObjects())
            {
                Assert.That(item.name, Does.Not.Contain("Debug"));
                Assert.That(item.name, Does.Not.Contain("Placeholder"));
                Assert.That(item.name, Does.Not.StartWith("Story Action"));
                Assert.That(item.name, Does.Not.Match("^Route Room [0-7] Path "),
                    "Removed floor path markers must not return as exposed debug arrows.");

                bool helper = item.name.Contains(" Beacon") || item.name.Contains(" Highlight");
                if (!helper) continue;
                Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
                Assert.That(colliders.All(collider => !collider.enabled), Is.True,
                    item.name + " is guidance decoration and may not block the player.");

                if (item.name.Contains(" Beacon"))
                    Assert.That(item.transform.localScale.magnitude, Is.LessThan(0.35f),
                        item.name + " is large enough to read as exposed debug geometry.");
            }

            foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                Assert.That(label.characterSize, Is.LessThanOrEqualTo(0.1f));
                Assert.That(label.GetComponent<Renderer>().bounds.size.x, Is.LessThan(24f),
                    label.name + " dominates the room instead of functioning as restrained wayfinding.");
            }
        }

        [Test]
        public void ActionPropsRemainReachableWithoutClosingTheCentralAisle()
        {
            foreach (MonoBehaviour action in Behaviours("SmallWorld.Flow.Stage15StoryActionInteractable"))
            {
                Collider collider = action.GetComponent<Collider>();
                Assert.That(collider, Is.Not.Null, action.name + " has no reachable interaction volume.");
                Assert.That(collider.enabled, Is.True, action.name + " interaction volume is disabled.");
                if (action.name != "Character - MeetYuna")
                    Assert.That(Mathf.Abs(action.transform.position.x), Is.GreaterThanOrEqualTo(10f),
                        action.name + " closes the central route through its room.");
            }

            for (int room = 0; room < 8; room++)
            {
                Assert.That(GameObject.Find($"Route Room {room} Path 1-1"), Is.Null,
                    "Exposed floor path cubes are debug geometry, not environmental navigation.");
                Transform left = Require($"Route Room {room} Entry Door Left").transform;
                Transform right = Require($"Route Room {room} Entry Door Right").transform;
                Assert.That(right.position.x - left.position.x, Is.GreaterThanOrEqualTo(4f),
                    $"Room {room} needs a readable, unobstructed central doorway.");
            }
        }

        [Test]
        public void RoomsUseDistinctSurfacesAndReadableLightHierarchy()
        {
            var floorColors = new HashSet<Color32>();
            for (int room = 0; room < 8; room++)
            {
                GameObject hub = Require($"{room:00} ");
                Renderer floor = hub.transform.Find("Hub Floor")?.GetComponent<Renderer>();
                Renderer wall = Require($"Route Room {room} Left Sight Wall").GetComponent<Renderer>();
                Assert.That(floor, Is.Not.Null, $"Room {room} is missing its local floor surface.");
                Assert.That(floor.sharedMaterial, Is.Not.Null);
                Assert.That(wall.sharedMaterial, Is.Not.Null);
                Color floorColor = floor.sharedMaterial.color;
                Color wallColor = wall.sharedMaterial.color;
                Assert.That(Vector4.Distance(floorColor, wallColor), Is.GreaterThan(0.04f),
                    $"Room {room} floor and walls collapse into one unshaped gray surface.");
                floorColors.Add((Color32)floorColor);

                Light entry = Require($"Route Room {room} Entry Light").GetComponent<Light>();
                Light objective = Require($"Route Room {room} Objective Light").GetComponent<Light>();
                Assert.That(objective.intensity, Is.GreaterThan(entry.intensity),
                    $"Room {room} objective needs stronger hierarchy than ambient entry lighting.");
            }
            Assert.That(floorColors.Count, Is.EqualTo(8), "All rooms must not reuse one debug-gray floor palette.");
        }

        private static IEnumerable<MonoBehaviour> Behaviours(string fullName)
        {
            return Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item != null && item.GetType().FullName == fullName);
        }

        private static IEnumerable<GameObject> SceneObjects()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item != null && item.scene.IsValid());
        }

        private static GameObject Require(string nameOrPrefix)
        {
            GameObject result = SceneObjects().FirstOrDefault(item => item.name == nameOrPrefix) ??
                                SceneObjects().FirstOrDefault(item => item.name.StartsWith(nameOrPrefix,
                                    StringComparison.Ordinal));
            Assert.That(result, Is.Not.Null, "Missing environment contract object: " + nameOrPrefix);
            return result;
        }
    }
}
