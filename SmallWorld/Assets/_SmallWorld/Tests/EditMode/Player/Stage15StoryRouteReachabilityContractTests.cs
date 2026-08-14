using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15StoryRouteReachabilityContractTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
        }

        [Test]
        public void EveryOpeningStoryAction_HasExactlyOneReachableSceneInteractable()
        {
            Type actionType = RequireType("SmallWorld.Flow.OpeningStoryAction");
            int[] expected = Enum.GetValues(actionType).Cast<object>().Select(Convert.ToInt32).ToArray();
            var connected = new Dictionary<int, List<string>>();

            foreach (MonoBehaviour behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour == null || !behaviour.gameObject.scene.IsValid() ||
                    behaviour.GetType().FullName != "SmallWorld.Flow.Stage15StoryActionInteractable")
                    continue;

                var serialized = new SerializedObject(behaviour);
                SerializedProperty action = serialized.FindProperty("action");
                SerializedProperty progress = serialized.FindProperty("progress");
                Assert.That(action, Is.Not.Null, behaviour.name + " has no serialized action contract.");
                Assert.That(progress, Is.Not.Null, behaviour.name + " has no progress adapter field.");
                Assert.That(progress.objectReferenceValue, Is.Not.Null,
                    behaviour.name + " cannot dispatch its action because the progress adapter is missing.");

                if (!connected.TryGetValue(action.intValue, out List<string> objects))
                    connected.Add(action.intValue, objects = new List<string>());
                objects.Add(GetHierarchyPath(behaviour.transform));
            }

            var missing = expected.Where(value => !connected.ContainsKey(value))
                .Select(value => Enum.GetName(actionType, value)).ToArray();
            var duplicates = connected.Where(pair => pair.Value.Count != 1)
                .Select(pair => $"{Enum.GetName(actionType, pair.Key)} => {string.Join(", ", pair.Value)}").ToArray();

            Assert.That(missing, Is.Empty,
                "Every runtime action must be physically reachable through one scene interactable. Missing: " +
                string.Join(", ", missing));
            Assert.That(duplicates, Is.Empty,
                "A story action must not be ambiguously serialized on multiple objects: " + string.Join(" | ", duplicates));
            Assert.That(connected.Count, Is.EqualTo(expected.Length));
        }

        [Test]
        public void GenericThreeStepMarkers_CannotCompleteOrAdvanceAChapter()
        {
            var bypasses = new List<string>();
            foreach (MonoBehaviour behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (behaviour == null || !behaviour.gameObject.scene.IsValid() ||
                    behaviour.GetType().FullName != "SmallWorld.Flow.StoryRouteInteractable")
                    continue;

                var serialized = new SerializedObject(behaviour);
                SerializedProperty nodeId = serialized.FindProperty("nodeId");
                SerializedProperty step = serialized.FindProperty("step");
                if (nodeId != null && !string.IsNullOrEmpty(nodeId.stringValue))
                    bypasses.Add($"{GetHierarchyPath(behaviour.transform)} ({nodeId.stringValue}/{step.enumDisplayNames[step.enumValueIndex]})");
            }

            Assert.That(bypasses, Is.Empty,
                "Dialogue/Puzzle/Memory summary markers bypass action order, retry, choice and save contracts. " +
                "Replace them with Stage15StoryActionInteractable objects: " + string.Join(" | ", bypasses));
        }

        [Test]
        public void FinalChapter_StopsAtPreparationAndExposesNoExecutableEndingChoice()
        {
            string[] forbidden =
            {
                "Program Exit Choice", "Connect Dollhouse Choice", "Remain With Girl Choice",
                "Become New Administrator Choice", "Send Girl To Reality Choice",
                "Restore Victims And Distribute Memories Choice"
            };

            Assert.That(GameObject.Find("Final Choice Readiness Inspector"), Is.Not.Null);
            Assert.That(GameObject.Find("No Ending Execution Boundary"), Is.Not.Null);
            foreach (string name in forbidden)
                Assert.That(GameObject.Find(name), Is.Null, name + " must remain unavailable before ending implementation.");
        }

        [Test]
        public void EveryRoom_HasDistinctPaletteLightingAndRepresentativeStorySilhouette()
        {
            string[] hubs =
            {
                "00 Prologue - The White Room", "01 Chapter 1 - The Fourth Place",
                "02 Chapter 2 - Last Platform", "03 Chapter 3 - A Perfect Day",
                "04 Chapter 4 - Faceless Office", "05 Chapter 5 - Cemetery Without a Funeral",
                "06 Chapter 6 - City in the Window", "07 Final Chapter - The White Room With Nothing Left"
            };
            string[] representatives =
            {
                "Empty Dollhouse", "The Empty Fourth Chair", "Arriving Last Train", "Warm Village Cafe",
                "Mirror Meeting Room", "Small Funeral Hall", "Scaled Reality City Basin", "Living House Wall Of Faces"
            };
            var floorColors = new HashSet<Color>();

            for (int i = 0; i < hubs.Length; i++)
            {
                GameObject hub = GameObject.Find(hubs[i]);
                GameObject representative = GameObject.Find(representatives[i]);
                GameObject objectiveLight = GameObject.Find($"Route Room {i} Objective Light");
                Assert.That(hub, Is.Not.Null, hubs[i] + " is missing.");
                Assert.That(representative, Is.Not.Null, representatives[i] + " is the room's minimum readable silhouette.");
                Assert.That(representative.GetComponentInChildren<Renderer>(), Is.Not.Null,
                    representatives[i] + " must be visibly rendered.");
                Assert.That(objectiveLight, Is.Not.Null);
                Assert.That(objectiveLight.GetComponent<Light>(), Is.Not.Null);

                Renderer floor = hub.transform.Find("Hub Floor")?.GetComponent<Renderer>();
                Renderer wall = hub.transform.Find($"Route Room {i} Left Sight Wall")?.GetComponent<Renderer>();
                Assert.That(floor, Is.Not.Null);
                Assert.That(wall, Is.Not.Null);
                Assert.That(floor.sharedMaterial.color, Is.Not.EqualTo(wall.sharedMaterial.color));
                floorColors.Add(floor.sharedMaterial.color);
            }

            Assert.That(floorColors.Count, Is.EqualTo(hubs.Length), "Each chapter needs a distinct readable palette.");
        }

        private static Type RequireType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }
            Assert.Fail(fullName + " runtime contract is not loaded.");
            return null;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
