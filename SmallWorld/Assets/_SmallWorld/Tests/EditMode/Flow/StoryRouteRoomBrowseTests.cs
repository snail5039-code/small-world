#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI.Stage7;
using System.Threading.Tasks;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class StoryRouteRoomBrowseTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            DialogueCursorMode.RequestGameplay();
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

        [Test]
        public void RecordsPauseAndSaveUi_BlockEveryRoomNavigationPathWithoutStealingState()
        {
            root = new GameObject("route-all-navigation-guards-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(3);
            var progress = new BrowseProgressSource(2);
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            controller.RestoreToNodeOrPrologue(2);

            Assert.That(controller.HandleTabPressed(), Is.True);
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.False);
            Assert.That(controller.TryTravelTo(1, out _), Is.False);
            Assert.That(controller.ActiveNodeIndex, Is.EqualTo(2));
            Assert.That(controller.HandleEscapePressed(), Is.True, "Esc must close records without opening pause.");
            Assert.That(controller.IsRuntimeOverlayOpen, Is.False);

            Assert.That(controller.HandleEscapePressed(), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.False);
            Assert.That(controller.TryTravelTo(1, out _), Is.False);
            Assert.That(Time.timeScale, Is.Zero, "Rejected navigation must not release pause ownership.");
            Assert.That(DialogueCursorMode.RequestedLockState, Is.EqualTo(CursorLockMode.None));
            controller.HandleEscapePressed();
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            var saveObject = new GameObject("route-navigation-save-owner");
            saveObject.transform.SetParent(root.transform);
            CanvasGroup canvas = saveObject.AddComponent<CanvasGroup>();
            Stage10ManualSavePanel savePanel = saveObject.AddComponent<Stage10ManualSavePanel>();
            savePanel.Configure(canvas, null, null, null);
            savePanel.Open();
            Assert.That(controller.HandleTabPressed(), Is.False);
            Assert.That(controller.HandleEscapePressed(), Is.False);
            Assert.That(controller.HandleRoomBrowse(-1, out _), Is.False);
            Assert.That(controller.TryTravelTo(1, out _), Is.False);
            Assert.That(controller.ActiveNodeIndex, Is.EqualTo(2));
            Assert.That(savePanel.IsOpen, Is.True);
        }

        [Test]
        public async Task RealityReturn_FromPrologue_PreparesSaveAndLoadsExactlyOnce()
        {
            root = new GameObject("route-reality-return-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(3);
            var progress = new ReturnProgressSource(2);
            int loads = 0;
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            controller.RestoreToNodeOrPrologue(0);
            controller.ConfigureRealityRoomLoader(() => { loads++; return Task.CompletedTask; });

            StoryRouteReturnResult result = await controller.ReturnToRealityRoomAsync();

            Assert.That(result.Accepted, Is.True);
            Assert.That(progress.PrepareCount, Is.EqualTo(1));
            Assert.That(loads, Is.EqualTo(1));
            Assert.That(progress.LatestUnlockedNodeIndex, Is.EqualTo(2), "CurrentChapter must not rewind.");
        }

        [Test]
        public async Task RealityReturn_BlocksNonPrologueUiAndDuplicateTransitions()
        {
            root = new GameObject("route-reality-return-guards-test");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            StoryRouteNode[] nodes = CreateNodes(3);
            var progress = new ReturnProgressSource(2);
            controller.Configure(player, nodes);
            controller.BindProgressSource(progress);
            controller.RestoreToNodeOrPrologue(2);
            controller.ConfigureRealityRoomLoader(() => Task.CompletedTask);
            Assert.That((await controller.ReturnToRealityRoomAsync()).Accepted, Is.False);
            Assert.That(progress.PrepareCount, Is.Zero);
            Assert.That(controller.CurrentObjective, Does.Contain("프롤로그"));

            controller.RestoreToNodeOrPrologue(0);
            controller.HandleEscapePressed();
            Assert.That((await controller.ReturnToRealityRoomAsync()).Accepted, Is.False);
            Assert.That(progress.PrepareCount, Is.Zero);
            controller.HandleEscapePressed();

            var pending = new TaskCompletionSource<bool>();
            controller.ConfigureRealityRoomLoader(() => pending.Task);
            Task<StoryRouteReturnResult> first = controller.ReturnToRealityRoomAsync();
            StoryRouteReturnResult duplicate = await controller.ReturnToRealityRoomAsync();
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.Feedback, Does.Contain("이미"));
            Assert.That(progress.PrepareCount, Is.EqualTo(1));
            pending.SetResult(true);
            Assert.That((await first).Accepted, Is.True);
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

        private sealed class ReturnProgressSource : IStoryRouteProgressSource, IStoryRouteChapterPositionSource,
            IStoryRouteRealityReturnSource
        {
            public ReturnProgressSource(int latest) => LatestUnlockedNodeIndex = latest;
            public int LatestUnlockedNodeIndex { get; }
            public int PrepareCount { get; private set; }
            public bool IsFinalGateUnlocked => false;
            public bool IsNodeUnlocked(string nodeId) => true;
            public void ReportNodeReached(string nodeId) { }
            public void ReportStep(string nodeId, StoryRouteStep step) { }
            public bool PrepareRealityRoomReturn(out string feedback)
            {
                PrepareCount++;
                feedback = "저장 완료";
                return true;
            }
        }
    }
}
#endif
