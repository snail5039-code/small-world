using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Player.Tests
{
    public sealed class Stage15RealityRoomStoryRouteExitAcceptanceTests
    {
        private const string RealityRoomPath = "Assets/_SmallWorld/Scenes/02_RealityRoom.unity";
        private const string StoryRoutePath = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";
        private const string StoryRouteSceneName = "04_StoryRoute";

        [SetUp]
        public void OpenRealityRoom()
        {
            EditorSceneManager.OpenScene(RealityRoomPath, OpenSceneMode.Single);
        }

        [Test]
        public void ExistingDoor_IsTheOnlyStoryRouteExit()
        {
            GameObject door = GameObject.Find("Door");
            Assert.That(door, Is.Not.Null, "RealityRoom must retain its existing Door object.");

            Component[] exits = UnityEngine.Object.FindObjectsByType<Component>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component != null &&
                    component.GetType().FullName == "SmallWorld.Flow.StoryRouteEntryInteractable")
                .ToArray();

            Assert.That(exits, Has.Length.EqualTo(1),
                "RealityRoom must contain exactly one StoryRoute exit; remove duplicate entry objects.");
            Assert.That(exits[0].gameObject, Is.SameAs(GameObject.Find("Door Hinge")),
                "The existing Door Hinge must own the exit so it can observe the door-open completion event.");
            Assert.That(GameObject.Find("Stage 15 Story Route Entry"), Is.Null,
                "The temporary duplicate StoryRoute entry object must be removed.");
        }

        [Test]
        public void ExistingDoor_ExitIsNotCoveredByAWallCollider()
        {
            GameObject door = GameObject.Find("Door");
            Assert.That(door, Is.Not.Null);
            Collider doorway = door.GetComponent<Collider>();
            Assert.That(doorway, Is.Not.Null, "The existing Door needs an interaction collider.");

            Bounds opening = doorway.bounds;
            opening.Expand(new Vector3(-0.1f, -0.1f, -0.02f));
            Collider[] blockingWalls = UnityEngine.Object.FindObjectsByType<Collider>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(collider => collider != doorway && collider.enabled &&
                    collider.gameObject.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    collider.bounds.Intersects(opening))
                .ToArray();

            Assert.That(blockingWalls, Is.Empty,
                "A wall collider overlaps the usable doorway: " +
                string.Join(", ", blockingWalls.Select(collider => collider.name)));
        }

        [Test]
        public void StoryRouteExit_TargetsEnabledBuildSceneThroughPlayingTransition()
        {
            EditorBuildSettingsScene buildScene = EditorBuildSettings.scenes.SingleOrDefault(
                scene => string.Equals(scene.path, StoryRoutePath, StringComparison.Ordinal));
            Assert.That(buildScene, Is.Not.Null, "StoryRoute must be present in Build Settings.");
            Assert.That(buildScene.enabled, Is.True, "StoryRoute must be enabled in Build Settings.");

            Type exitType = FindType("SmallWorld.Flow.StoryRouteEntryInteractable");
            Type transitionType = FindType("SmallWorld.Flow.SceneTransitionService");
            Assert.That(exitType, Is.Not.Null, "StoryRoute exit interaction component is missing.");
            Assert.That(transitionType, Is.Not.Null, "Scene transition service is missing.");

            FieldInfo target = exitType.GetField("StorySceneName",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(target, Is.Not.Null,
                "The exit must declare the StoryRoute scene it sends to the transition service.");
            Assert.That(target.GetRawConstantValue(), Is.EqualTo(StoryRouteSceneName));
            Assert.That(transitionType.GetMethod("LoadPlayingSceneAsync", new[] { typeof(string) }), Is.Not.Null,
                "The exit transition must use the playing-scene transition entry point.");
        }

        private static Type FindType(string fullName) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(type => type != null);
    }
}
