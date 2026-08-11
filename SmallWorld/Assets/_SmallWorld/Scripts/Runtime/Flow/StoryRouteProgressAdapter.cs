using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Story;
using UnityEngine;

namespace SmallWorld.Flow
{
    [RequireComponent(typeof(StoryRouteController))]
    public sealed class StoryRouteProgressAdapter : MonoBehaviour, IStoryRouteProgressSource
    {
        private readonly SaveDataStoryProgressStore store = new SaveDataStoryProgressStore();
        private readonly StoryFlowService flow = new StoryFlowService();
        private readonly Stage15OpeningStoryService openingStory = new Stage15OpeningStoryService();
        private SaveData save;
        private StoryProgress progress;

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
            GetComponent<StoryRouteController>().BindProgressSource(this);
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
            OpeningStoryResult result = openingStory.TryPerform(save, progress, action);
            if (result.Accepted) Persist();
            return result;
        }

        private void EnsureLoaded()
        {
            if (progress != null) return;
            SaveReadResult latest = Stage10SaveRuntime.FindLatest();
            save = latest.IsSuccess && latest.Data != null ? latest.Data : SaveData.CreateNew();
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
            if (nodeId != null && nodeId.StartsWith("chapter-") &&
                int.TryParse(nodeId.Substring(8), out int number) && number >= 1 && number <= 6)
            {
                chapter = (StoryChapterId)number;
                return true;
            }
            chapter = StoryChapterId.Prologue;
            return false;
        }
    }
}
