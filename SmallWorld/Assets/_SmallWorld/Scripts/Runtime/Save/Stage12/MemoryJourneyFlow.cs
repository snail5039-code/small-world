using System;
using SmallWorld.Save.Stage10;

namespace SmallWorld.Save.Stage12
{
    public enum MemoryExitResult
    {
        BlockedByPuzzle,
        ReturnedToWhiteRoom
    }

    /// <summary>Coordinates memory-space, puzzle, and active-scene save state.</summary>
    public sealed class MemoryJourneyFlow
    {
        private readonly MemorySpaceDefinition definition;
        private readonly MemorySpaceFlow spaceFlow = new MemorySpaceFlow();
        private readonly MemorySpaceProgress spaceProgress = new MemorySpaceProgress();
        private readonly MemoryPuzzleFlow puzzleFlow;

        public MemoryJourneyFlow(MemorySpaceDefinition definition, string puzzleId, int[] solution)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.definition.Validate();
            puzzleFlow = new MemoryPuzzleFlow(puzzleId, solution);
        }

        public MemorySpaceState RestoreSpace(SaveData save) => spaceProgress.Get(RequireSave(save), definition.Id);

        public MemoryPuzzleState RestorePuzzle(SaveData save)
        {
            save = RequireSave(save);
            PuzzleSaveEntry entry = save.Puzzles.Find(item => item.PuzzleId == puzzleFlow.PuzzleId);
            return puzzleFlow.Normalize(entry == null ? null : new MemoryPuzzleState
            {
                PuzzleId = entry.PuzzleId,
                Progress = entry.CurrentStep,
                Completed = entry.Status != 0,
                Mistakes = entry.IncorrectAttempts
            });
        }

        public MemorySpaceState Enter(SaveData save)
        {
            save = RequireSave(save);
            MemorySpaceState state = spaceFlow.Enter(definition, RestoreSpace(save));
            state.PuzzleProgress = RestorePuzzle(save).Progress;
            spaceProgress.Set(save, state);
            save.ActiveSceneId = definition.EntrySceneId;
            return state;
        }

        public bool SubmitChoice(SaveData save, int choice)
        {
            save = RequireSave(save);
            MemorySpaceState space = RestoreSpace(save);
            if (space.Phase != MemorySpacePhase.Inside)
                throw new InvalidOperationException("기억 공간 안에서만 퍼즐을 진행할 수 있습니다.");

            MemoryPuzzleState puzzle = RestorePuzzle(save);
            bool accepted = puzzleFlow.Submit(puzzle, choice);
            StorePuzzle(save, puzzle);
            space.PuzzleProgress = puzzle.Progress;
            spaceProgress.Set(save, space);
            return accepted;
        }

        public MemoryExitResult TryExit(SaveData save)
        {
            save = RequireSave(save);
            MemoryPuzzleState puzzle = RestorePuzzle(save);
            if (!puzzle.Completed) return MemoryExitResult.BlockedByPuzzle;

            MemorySpaceState space = spaceFlow.Exit(definition, RestoreSpace(save));
            space.PuzzleProgress = puzzle.Progress;
            spaceProgress.Set(save, space);
            save.ActiveSceneId = definition.ReturnSceneId;
            return MemoryExitResult.ReturnedToWhiteRoom;
        }

        private void StorePuzzle(SaveData save, MemoryPuzzleState state)
        {
            PuzzleSaveEntry entry = save.Puzzles.Find(item => item.PuzzleId == puzzleFlow.PuzzleId);
            if (entry == null)
            {
                entry = new PuzzleSaveEntry { PuzzleId = puzzleFlow.PuzzleId };
                save.Puzzles.Add(entry);
            }

            entry.Status = state.Completed ? 1 : 0;
            entry.CurrentStep = state.Progress;
            entry.IncorrectAttempts = state.Mistakes;
        }

        private static SaveData RequireSave(SaveData save) =>
            save ?? throw new ArgumentNullException(nameof(save));
    }
}
