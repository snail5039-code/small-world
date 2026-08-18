using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Story;
using UnityEngine;

namespace SmallWorld.Flow
{
    [RequireComponent(typeof(StoryRouteController))]
    public sealed class StoryRouteProgressAdapter : MonoBehaviour, IStoryRouteProgressSource, IStoryRouteChapterPositionSource,
        IStoryRouteRealityReturnSource, IStoryRouteRecordSource
    {
        private readonly SaveDataStoryProgressStore store = new SaveDataStoryProgressStore();
        private readonly StoryFlowService flow = new StoryFlowService();
        private readonly Stage15OpeningStoryService openingStory = new Stage15OpeningStoryService();
        private SaveData save;
        private StoryProgress progress;

        public SaveData CurrentSave => save;
        public StoryChapterId CurrentChapter => Progress.CurrentChapter;
        public int LatestUnlockedNodeIndex => CurrentChapterNodeIndex(Progress.CurrentChapter);

        public bool IsFinalGateUnlocked => flow.CanEnterFinalChapter(Progress);

        private StoryProgress Progress
        {
            get
            {
                EnsureLoaded();
                return progress;
            }
        }

        private void Awake()
        {
            EnsureLoaded();
            StoryRouteController route = GetComponent<StoryRouteController>();
            route.BindProgressSource(this);
            Stage10ManualSavePanel manualSave = FindFirstObjectByType<Stage10ManualSavePanel>(FindObjectsInactive.Include);
            manualSave?.Configure(CaptureCurrentForManualSave, LoadManualSave);
            route.RestoreToNodeOrPrologue(CurrentChapterNodeIndex(progress.CurrentChapter));
            PresentArrival(progress.CurrentChapter);
        }

        public bool IsNodeUnlocked(string nodeId)
        {
            return TryGetChapter(nodeId, out StoryChapterId chapter) &&
                (int)Progress.CurrentChapter >= (int)chapter;
        }

        public void ReportNodeReached(string nodeId)
        {
            if (!TryGetChapter(nodeId, out StoryChapterId chapter)) return;
            Progress.GetChapter(chapter).ObjectiveCompleted = true;
            Persist();
        }

        public void ReportStep(string nodeId, StoryRouteStep step)
        {
            if (!TryGetChapter(nodeId, out StoryChapterId chapter)) return;
            if (chapter >= StoryChapterId.Chapter2) return;
            StoryChapterProgress chapterProgress = Progress.GetChapter(chapter);
            switch (step)
            {
                case StoryRouteStep.Dialogue: chapterProgress.DialogueCompleted = true; break;
                case StoryRouteStep.Puzzle: chapterProgress.PuzzleCompleted = true; break;
                case StoryRouteStep.Memory: chapterProgress.MemorySpaceCompleted = true; break;
            }
            flow.TryAdvance(Progress);
            Persist();
        }

        public OpeningStoryResult PerformOpeningAction(OpeningStoryAction action)
        {
            EnsureLoaded();
            StoryRouteController route = GetComponent<StoryRouteController>();
            if (route != null && !IsLiveChapterRoom(route.ActiveNodeIndex, progress.CurrentChapter))
                return new OpeningStoryResult(false, "과거 방에서는 완료한 행동을 다시 실행할 수 없습니다. PageDown으로 현재 진행 방에 복귀하세요.");
            OpeningStoryResult result = openingStory.TryPerform(save, progress, action);
            if (result.Accepted) Persist();
            string objective = StoryRouteGuidance.NextObjective(progress.CurrentChapter, action, result.Accepted);
            if (progress.CurrentChapter == StoryChapterId.Prologue)
                objective = "다음 대상 · " + StoryRouteGuidance.PrologueNextTarget(progress);
            if (route != null) route.UpdateObjective(objective);
            string status = result.Accepted ? "완료" : "잠김";
            return new OpeningStoryResult(result.Accepted,
                $"{status}: {result.Message}\n다음: {objective}");
        }

        public void PresentArrival(StoryChapterId chapter)
        {
            EnsureLoaded();
            StoryRouteController route = GetComponent<StoryRouteController>();
            if (route == null) return;
            route.UpdateGuidance(StoryRouteGuidance.Location(chapter),
                chapter == StoryChapterId.Prologue
                    ? "다음 대상 · " + StoryRouteGuidance.PrologueNextTarget(progress)
                    : StoryRouteGuidance.ArrivalObjective(chapter),
                StoryRouteGuidance.ArrivalDialogue(progress,
                    new StoryRelationshipService().Get(save, "girl")));
        }

        public void PresentVisitedRoom(StoryChapterId room, bool isCurrentChapter)
        {
            EnsureLoaded();
            StoryRouteController route = GetComponent<StoryRouteController>();
            if (route == null) return;
            string objective = isCurrentChapter
                ? StoryRouteGuidance.ArrivalObjective(room)
                : $"완료한 방을 다시 살펴보는 중입니다. PageDown 또는 다음 방 게이트로 {StoryRouteGuidance.Location(progress.CurrentChapter)}까지 복귀하세요.";
            string dialogue = isCurrentChapter
                ? StoryRouteGuidance.ArrivalDialogue(progress, new StoryRelationshipService().Get(save, "girl"))
                : "과거 방에서는 완료한 행동이 잠기며 저장된 진행은 바뀌지 않습니다.";
            route.UpdateGuidance(StoryRouteGuidance.Location(room), objective, dialogue);
        }

        public bool PrepareRealityRoomReturn(out string feedback)
        {
            EnsureLoaded();
            store.Save(save, progress);
            save.ActiveSceneId = "RealityRoom";
            if (!Stage10SaveRuntime.Service.AutoSave(save))
            {
                feedback = "현실방 복귀 상태를 저장하지 못했습니다.";
                return false;
            }
            Stage10SaveRuntime.QueueLoad(save);
            feedback = "현실방 복귀 상태를 저장했습니다.";
            return true;
        }

        public SaveData CaptureCurrentForManualSave()
        {
            EnsureLoaded();
            store.Save(save, progress);
            save.ActiveSceneId = "04_StoryRoute";
            return save;
        }

        public bool LoadManualSave(int slot)
        {
            SaveReadResult result = Stage10SaveRuntime.Service.LoadManual(slot);
            if (!result.IsSuccess || result.Data == null) return false;
            save = result.Data;
            progress = store.Load(save);
            StoryRouteController route = GetComponent<StoryRouteController>();
            route?.RestoreToNodeOrPrologue(CurrentChapterNodeIndex(progress.CurrentChapter));
            PresentArrival(progress.CurrentChapter);
            return true;
        }

        public string BuildRecordSummary(string nextObjective)
        {
            EnsureLoaded();
            StoryChapterProgress chapter = progress.GetChapter(progress.CurrentChapter);
            int completedActions = 0;
            for (int i = 0; i < progress.ForeshadowFlags.Count; i++)
                if (progress.ForeshadowFlags[i].StartsWith("opening-action-", System.StringComparison.Ordinal)) completedActions++;
            string completion = chapter.IsComplete ? "완료" : "진행 중";
            string choices = progress.ImportantChoices.Count == 0 ? "아직 선택 없음" : $"중요 선택 {progress.ImportantChoices.Count}개";
            string objective = string.IsNullOrWhiteSpace(nextObjective) ? StoryRouteGuidance.ArrivalObjective(progress.CurrentChapter) : nextObjective;
            return $"현재 장 · {StoryRouteGuidance.Location(progress.CurrentChapter)} ({completion})\n" +
                   $"완료한 조사/행동 · {completedActions}개\n선택 기록 · {choices}\n\n" +
                   $"다음 목표 · {objective}\n[E] 조사   [Tab/Esc] 기록 닫기";
        }

        private void EnsureLoaded()
        {
            if (progress != null) return;
            save = Stage10SaveRuntime.ConsumeSceneSession();
            if (save == null)
            {
                SaveReadResult latest = Stage10SaveRuntime.FindLatest();
                save = latest.IsSuccess && latest.Data != null ? latest.Data : SaveData.CreateNew();
            }
            progress = store.Load(save);
        }

        private void Persist()
        {
            store.Save(save, progress);
            save.ActiveSceneId = "04_StoryRoute";
            Stage10SaveRuntime.Service.AutoSave(save);
        }

        private static bool TryGetChapter(string nodeId, out StoryChapterId chapter)
        {
            if (nodeId == "prologue") { chapter = StoryChapterId.Prologue; return true; }
            if (nodeId == "final-chapter") { chapter = StoryChapterId.FinalChapter; return true; }
            if (nodeId != null && nodeId.StartsWith("chapter-") &&
                int.TryParse(nodeId.Substring(8), out int number) && number >= 1 && number <= 6)
            {
                chapter = (StoryChapterId)number;
                return true;
            }
            chapter = StoryChapterId.Prologue;
            return false;
        }

        internal static int CurrentChapterNodeIndex(StoryChapterId chapter)
        {
            int index = (int)chapter;
            return index >= (int)StoryChapterId.Prologue && index <= (int)StoryChapterId.FinalChapter
                ? index
                : 0;
        }

        internal static bool IsLiveChapterRoom(int activeNodeIndex, StoryChapterId currentChapter) =>
            activeNodeIndex == CurrentChapterNodeIndex(currentChapter);
    }
}
