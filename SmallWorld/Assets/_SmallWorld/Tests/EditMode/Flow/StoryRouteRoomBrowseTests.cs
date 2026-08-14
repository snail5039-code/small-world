#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class StoryRouteRoomBrowseTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void PreviousRoomAndReturn_MoveArrivalWithoutReportingPastRoomProgress()
        {
            root = new GameObject("route-browse-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(4);
            var progress = new BrowseProgressSource(3);
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            Assert.That(controller.RestoreToNodeOrPrologue(3), Is.EqualTo(3));

            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.True);
            Assert.That(controller.ActiveNodeIndex, Is.EqualTo(2));
            Assert.That(player.position, Is.EqualTo(nodes[2].Arrival.position));
            Assert.That(progress.ReportedNodeCount, Is.Zero,
                "Reviewing an earlier room must not write completion state.");

            Assert.That(controller.HandleRoomBrowse(1, out _), Is.True);
            Assert.That(controller.ActiveNodeIndex, Is.EqualTo(3));
            Assert.That(progress.ReportedNodeCount, Is.EqualTo(1),
                "Returning to the live chapter may report its arrival only.");
            Assert.That(progress.LatestUnlockedNodeIndex, Is.EqualTo(3));
        }

        [Test]
        public void Browse_CannotMoveBeforePrologueOrBeyondCurrentChapter()
        {
            root = new GameObject("route-browse-boundary-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(5);
            var progress = new BrowseProgressSource(2);
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            controller.RestoreToNodeOrPrologue(2);

            Assert.That(controller.HandleRoomBrowse(1, out _), Is.False);
            Assert.That(controller.ActiveNodeIndex, Is.EqualTo(2));
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.True);
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.True);
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.False);
            Assert.That(controller.ActiveNodeIndex, Is.Zero);
            Assert.That(progress.ReportedNodeCount, Is.Zero);
        }

        [Test]
        public void PastRoom_IsHardLockedForStoryActions()
        {
            Assert.That(StoryRouteProgressAdapter.IsLiveChapterRoom(2,
                SmallWorld.Save.Story.StoryChapterId.Chapter3), Is.False);
            Assert.That(StoryRouteProgressAdapter.IsLiveChapterRoom(3,
                SmallWorld.Save.Story.StoryChapterId.Chapter3), Is.True);
        }

        [Test]
        public void TryTravelTo_IsBlockedWhileRuntimeOverlayIsOpen()
        {
            root = new GameObject("route-overlay-travel-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(3);
            var progress = new BrowseProgressSource(2);
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            controller.RestoreToNodeOrPrologue(2);
            Vector3 before = player.position;

            Assert.That(controller.HandleEscapePressed(), Is.True);
            Assert.That(controller.TryTravelTo(1, out string feedback), Is.False);
            Assert.That(feedback, Does.Contain("UI"));
            Assert.That(player.position, Is.EqualTo(before));
            Assert.That(progress.ReportedNodeCount, Is.Zero);
        }

        private StoryRouteNode[] CreateNodes(int count)
        {
            var nodes = new StoryRouteNode[count];
            for (int i = 0; i < count; i++)
            {
                Transform arrival = new GameObject($"arrival-{i}").transform;
                arrival.SetParent(root.transform);
                arrival.position = new Vector3(i * 10f, 0f, 0f);
                nodes[i] = new StoryRouteNode { Id = i == 0 ? "prologue" : $"chapter-{i}", DisplayName = $"Room {i}", Arrival = arrival };
            }
            return nodes;
        }

        private sealed class BrowseProgressSource : IStoryRouteProgressSource, IStoryRouteChapterPositionSource
        {
            public BrowseProgressSource(int latest) => LatestUnlockedNodeIndex = latest;
            public int LatestUnlockedNodeIndex { get; }
            public int ReportedNodeCount { get; private set; }
            public bool IsFinalGateUnlocked => false;
            public bool IsNodeUnlocked(string nodeId) => true;
            public void ReportNodeReached(string nodeId) => ReportedNodeCount++;
            public void ReportStep(string nodeId, StoryRouteStep step) { }
        }
    }
}
#endif
