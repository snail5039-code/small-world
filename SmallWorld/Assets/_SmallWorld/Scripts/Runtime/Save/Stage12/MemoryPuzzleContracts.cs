using System;

namespace SmallWorld.Save.Stage12
{
    /// <summary>작은 기억 공간에서 사용하는 순서 입력 퍼즐의 저장 가능한 상태.</summary>
    [Serializable]
    public sealed class MemoryPuzzleState
    {
        public string PuzzleId = "";
        public int Progress;
        public bool Completed;
        public int Mistakes;
    }

    public sealed class MemoryPuzzleFlow
    {
        private readonly int[] solution;
        public MemoryPuzzleFlow(string puzzleId, int[] solution)
        {
            if (string.IsNullOrWhiteSpace(puzzleId)) throw new ArgumentException("퍼즐 ID가 필요합니다.", nameof(puzzleId));
            if (solution == null || solution.Length == 0) throw new ArgumentException("퍼즐 해답이 필요합니다.", nameof(solution));
            PuzzleId = puzzleId; this.solution = (int[])solution.Clone();
        }
        public string PuzzleId { get; }
        public MemoryPuzzleState Start(MemoryPuzzleState state) { state = Normalize(state); if (!state.Completed) state.Progress = Math.Max(0, state.Progress); return state; }
        public bool Submit(MemoryPuzzleState state, int choice)
        {
            if (state == null || state.Completed || state.PuzzleId != PuzzleId) return false;
            if (state.Progress < 0 || state.Progress >= solution.Length) return false;
            if (solution[state.Progress] != choice) { state.Mistakes++; state.Progress = 0; return false; }
            state.Progress++;
            if (state.Progress == solution.Length) state.Completed = true;
            return true;
        }
        public MemoryPuzzleState Normalize(MemoryPuzzleState state)
        {
            if (state == null || state.PuzzleId != PuzzleId) state = new MemoryPuzzleState { PuzzleId = PuzzleId };
            state.Progress = Math.Max(0, Math.Min(solution.Length, state.Progress));
            state.Mistakes = Math.Max(0, state.Mistakes);
            state.Completed = state.Completed || state.Progress == solution.Length;
            return state;
        }
    }
}
