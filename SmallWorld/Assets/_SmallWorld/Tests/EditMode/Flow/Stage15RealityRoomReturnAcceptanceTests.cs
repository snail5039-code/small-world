#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Threading.Tasks;
using NUnit.Framework;
using SmallWorld.Flow;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Story;
using UnityEngine;

namespace SmallWorld.Tests.EditMode.Flow
{
    public sealed class Stage15RealityRoomReturnAcceptanceTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Stage10SaveRuntime.ConsumePendingLoad();
            Stage10SaveRuntime.ConsumeSceneSession();
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void PrepareRealityReturnPreservesStoryChoicesAndQueuesTheSameSession()
        {
            var service = new CapturingSaveService();
            Stage10SaveRuntime.Configure(service);
            SaveData save = SaveData.CreateNew();
            save.ActiveSceneId = "04_StoryRoute";
            save.Relationships.Add(new RelationshipSaveEntry { CharacterId = "girl", Value = 17 });
            var progress = new StoryProgress { CurrentChapter = StoryChapterId.Chapter3 };
            progress.GetChapter(StoryChapterId.Chapter2).ObjectiveCompleted = true;
            progress.ImportantChoices.Add(new StoryChoiceState { ChoiceId = "prologue-answer", OutcomeId = "stay" });
            progress.ExternalEntityFlags.Add("girl-remembers-player");
            new SaveDataStoryProgressStore().Save(save, progress);
            Stage10SaveRuntime.QueueSceneSession(save);

            root = new GameObject("reality-return-save-contract");
            StoryRouteController route = root.AddComponent<StoryRouteController>();
            StoryRouteProgressAdapter adapter = root.AddComponent<StoryRouteProgressAdapter>();
            string storyBefore = JsonUtility.ToJson(new SaveDataStoryProgressStore().Load(save));
            int relationshipBefore = save.Relationships[0].Value;

            Assert.That(adapter.PrepareRealityRoomReturn(out _), Is.True);
            Assert.That(adapter.CurrentSave, Is.SameAs(save));
            Assert.That(adapter.CurrentChapter, Is.EqualTo(StoryChapterId.Chapter3));
            Assert.That(JsonUtility.ToJson(new SaveDataStoryProgressStore().Load(save)), Is.EqualTo(storyBefore));
            Assert.That(save.Relationships[0].Value, Is.EqualTo(relationshipBefore));
            Assert.That(save.ActiveSceneId, Is.EqualTo("RealityRoom"));
            Assert.That(service.AutoSaved, Is.SameAs(save));
            Assert.That(Stage10SaveRuntime.PendingLoad, Is.SameAs(save),
                "RealityRoom must consume the exact selected story session.");
        }

        [Test]
        public async Task ReturnRejectsOpenUiAndDuplicateTransitionRequests()
        {
            root = new GameObject("reality-return-input-contract");
            StoryRouteController controller = root.AddComponent<StoryRouteController>();
            Transform player = new GameObject("player").transform;
            player.SetParent(root.transform);
            Transform arrival = new GameObject("arrival").transform;
            arrival.SetParent(root.transform);
            controller.Configure(player, new[]
            {
                new StoryRouteNode { Id = "prologue", DisplayName = "Prologue", Arrival = arrival }
            });
            var source = new ReturnSource();
            controller.BindProgressSource(source);
            controller.RestoreToNodeOrPrologue(0);
            var transition = new TaskCompletionSource<bool>();
            int loaderCalls = 0;
            controller.ConfigureRealityRoomLoader(async () =>
            {
                loaderCalls++;
                await transition.Task;
            });

            controller.HandleEscapePressed();
            Assert.That((await controller.ReturnToRealityRoomAsync()).Accepted, Is.False);
            Assert.That(source.PrepareCount, Is.Zero);
            controller.HandleEscapePressed();
            controller.HandleTabPressed();
            Assert.That((await controller.ReturnToRealityRoomAsync()).Accepted, Is.False);
            Assert.That(source.PrepareCount, Is.Zero);
            controller.HandleEscapePressed();

            var saveOwner = new GameObject("return-save-panel");
            saveOwner.transform.SetParent(root.transform);
            CanvasGroup canvas = saveOwner.AddComponent<CanvasGroup>();
            Stage10ManualSavePanel savePanel = saveOwner.AddComponent<Stage10ManualSavePanel>();
            savePanel.Configure(canvas, null, null, null);
            savePanel.Open();
            Assert.That((await controller.ReturnToRealityRoomAsync()).Accepted, Is.False);
            Assert.That(source.PrepareCount, Is.Zero);
            savePanel.Close();

            Task<StoryRouteReturnResult> first = controller.ReturnToRealityRoomAsync();
            Assert.That(loaderCalls, Is.EqualTo(1));
            Assert.That(source.PrepareCount, Is.EqualTo(1));
            StoryRouteReturnResult duplicate = await controller.ReturnToRealityRoomAsync();
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(loaderCalls, Is.EqualTo(1));
            Assert.That(source.PrepareCount, Is.EqualTo(1));
            transition.SetResult(true);
            Assert.That((await first).Accepted, Is.True);
        }

        private sealed class ReturnSource : IStoryRouteProgressSource, IStoryRouteChapterPositionSource,
            IStoryRouteRealityReturnSource
        {
            public int PrepareCount { get; private set; }
            public int LatestUnlockedNodeIndex => 0;
            public bool IsFinalGateUnlocked => false;
            public bool IsNodeUnlocked(string nodeId) => nodeId == "prologue";
            public void ReportNodeReached(string nodeId) { }
            public void ReportStep(string nodeId, StoryRouteStep step) { }
            public bool PrepareRealityRoomReturn(out string feedback)
            {
                PrepareCount++;
                feedback = "prepared";
                return true;
            }
        }

        private sealed class CapturingSaveService : IGameSaveService
        {
            public SaveData AutoSaved;
            public bool AutoSave(SaveData data) { AutoSaved = data; return true; }
            public bool SaveManual(int slotIndex, SaveData data) => true;
            public SaveReadResult LoadLatestAutoSave() => SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveReadResult LoadManual(int slotIndex) => SaveReadResult.Failure(SaveReadStatus.Missing);
            public SaveData StartNewGame() => SaveData.CreateNew();
        }
    }
}
#endif
