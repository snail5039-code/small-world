#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SmallWorld.Save.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15StoryRouteDiscoverabilityContractTests
    {
        private const string StoryRouteScene = "Assets/_SmallWorld/Scenes/04_StoryRoute.unity";

        private static readonly string[] CanonicalSpaces =
        {
            "프롤로그", "네 번째 자리", "마지막 승강장", "완벽한 하루",
            "얼굴 없는 사무실", "장례식 없는 묘지", "창문 안의 도시",
            "아무것도 남지 않은 하얀 방"
        };

        private static readonly string[] HubNames =
        {
            "Prologue - The White Room", "Chapter 1 - The Fourth Place",
            "Chapter 2 - Last Platform", "Chapter 3 - A Perfect Day",
            "Chapter 4 - Faceless Office", "Chapter 5 - Cemetery Without a Funeral",
            "Chapter 6 - City in the Window", "Final Chapter - The White Room With Nothing Left"
        };

        [Test]
        public void Guidance_EveryChapterNamesSpaceAndActionableObjective()
        {
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            MethodInfo location = RequireStaticMethod(guidance, "Location");
            MethodInfo objective = RequireStaticMethod(guidance, "ArrivalObjective");

            for (int i = 0; i < CanonicalSpaces.Length; i++)
            {
                object chapter = Enum.ToObject(typeof(StoryChapterId), i);
                Assert.That((string)location.Invoke(null, new[] { chapter }), Does.Contain(CanonicalSpaces[i]));
                Assert.That((string)objective.Invoke(null, new[] { chapter }), Is.Not.Empty);
            }
        }

        [Test]
        public void Guidance_RelationshipBranchesKeepGuidanceAndResultsExplainRecovery()
        {
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            MethodInfo arrivalDialogue = RequireStaticMethod(guidance, "ArrivalDialogue");
            MethodInfo nextObjective = RequireStaticMethod(guidance, "NextObjective");
            Type actionType = RequireType("SmallWorld.Flow.OpeningStoryAction");
            var progress = new StoryProgress();

            string warm = (string)arrivalDialogue.Invoke(null, new object[] { progress, 10 });
            string neutral = (string)arrivalDialogue.Invoke(null, new object[] { progress, 0 });
            string wary = (string)arrivalDialogue.Invoke(null, new object[] { progress, -1 });
            Assert.That(new HashSet<string> { warm, neutral, wary }.Count, Is.EqualTo(3));
            Assert.That(warm, Does.Contain("표식"));
            Assert.That(neutral, Does.Contain("표식"));

            object completedStep = Enum.Parse(actionType, "ReverseAnnouncement3");
            string success = (string)nextObjective.Invoke(null,
                new[] { (object)StoryChapterId.Chapter2, completedStep, true });
            string locked = (string)nextObjective.Invoke(null,
                new[] { (object)StoryChapterId.Chapter2, completedStep, false });
            Assert.That(success, Does.Contain("목적지"));
            Assert.That(success, Does.Contain("안전 구역"));
            Assert.That(locked, Does.Contain("잠김 사유"));
            Assert.That(locked, Does.Contain("직전"));
        }

        [Test]
        public void Guidance_FinalChoiceReadyExplicitlyStopsBeforeEndingExecution()
        {
            Type guidance = RequireType("SmallWorld.Flow.StoryRouteGuidance");
            Type actionType = RequireType("SmallWorld.Flow.OpeningStoryAction");
            string result = (string)RequireStaticMethod(guidance, "NextObjective").Invoke(null,
                new[] { (object)StoryChapterId.FinalChapter, Enum.Parse(actionType, "PrepareFinalChoice"), true });
            Assert.That(result, Does.Contain("준비 완료"));
            Assert.That(result, Does.Contain("여기서 멈춘다"));
            Assert.That(result, Does.Contain("실행하지 않는다"));
        }

        [Test]
        public void Scene_CurrentChapterRestoresToEightArrivalsAndInvalidFallsBackToPrologue()
        {
            OpenStoryRoute();
            GameObject routeObject = GameObject.Find("Stage 15 Story Route");
            Component controller = routeObject?.GetComponent("StoryRouteController");
            Component adapter = routeObject?.GetComponent("StoryRouteProgressAdapter");
            Assert.That(controller, Is.Not.Null);
            Assert.That(adapter, Is.Not.Null);

            MethodInfo map = adapter.GetType().GetMethod("CurrentChapterNodeIndex",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo restore = controller.GetType().GetMethod("RestoreToNodeOrPrologue",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(map, Is.Not.Null);
            Assert.That(restore, Is.Not.Null);
            GameObject player = GameObject.Find("First Person Player");

            for (int i = 0; i < 8; i++)
            {
                int mapped = (int)map.Invoke(null, new[] { Enum.ToObject(typeof(StoryChapterId), i) });
                int restored = (int)restore.Invoke(controller, new object[] { mapped });
                Assert.That(mapped, Is.EqualTo(i));
                Assert.That(restored, Is.EqualTo(i));
                Assert.That(player.transform.position.z, Is.EqualTo(i * 36f - 13f).Within(0.001f));
            }

            int fallback = (int)map.Invoke(null, new[] { Enum.ToObject(typeof(StoryChapterId), 999) });
            Assert.That(fallback, Is.Zero);
            Assert.That((int)restore.Invoke(controller, new object[] { fallback }), Is.Zero);
            Assert.That(player.transform.position.z, Is.EqualTo(-13f).Within(0.001f));
        }

        [Test]
        public void Scene_EightRoomsExposeDistinctNonBlockingWayfindingContract()
        {
            OpenStoryRoute();
            var floorColors = new HashSet<Color>();
            for (int room = 0; room < 8; room++)
            {
                GameObject hub = GameObject.Find($"{room:00} {HubNames[room]}");
                Assert.That(hub, Is.Not.Null);
                Renderer floor = hub.transform.Find("Hub Floor")?.GetComponent<Renderer>();
                Renderer wall = hub.transform.Find($"Route Room {room} Left Sight Wall")?.GetComponent<Renderer>();
                Assert.That(floor, Is.Not.Null);
                Assert.That(wall, Is.Not.Null);
                Assert.That(floor.sharedMaterial.color, Is.Not.EqualTo(wall.sharedMaterial.color));
                floorColors.Add(floor.sharedMaterial.color);

                Assert.That(GameObject.Find($"Route Room {room} Entrance Sign")?.GetComponent<TextMesh>(), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Objective Light")?.GetComponent<Light>(), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Dialogue Highlight"), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Puzzle Highlight"), Is.Not.Null);
                Assert.That(GameObject.Find($"Route Room {room} Memory Highlight"), Is.Not.Null);

                for (int segment = 1; segment <= 3; segment++)
                for (int step = 1; step <= 3; step++)
                {
                    GameObject marker = GameObject.Find($"Route Room {room} Path {segment}-{step}");
                    Assert.That(marker, Is.Not.Null);
                    Assert.That(marker.GetComponent<Collider>().enabled, Is.False);
                }
            }
            Assert.That(floorColors.Count, Is.EqualTo(8));
        }

        private static void OpenStoryRoute()
        {
            EditorSceneManager.OpenScene(StoryRouteScene);
        }

        private static Type RequireType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, fullName + " public contract is not integrated.");
            return type;
        }

        private static MethodInfo RequireStaticMethod(Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, type.FullName + "." + name + " public contract is missing.");
            return method;
        }
    }
}
#endif
