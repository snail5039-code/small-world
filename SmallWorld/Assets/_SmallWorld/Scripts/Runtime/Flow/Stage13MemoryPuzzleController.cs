using SmallWorld.Save.Stage12;
using UnityEngine;

namespace SmallWorld.Flow
{
    /// <summary>첫 기억 공간의 무료 프로토타입 퍼즐: 1-2-3 순서 입력.</summary>
    public sealed class Stage13MemoryPuzzleController : MonoBehaviour
    {
        [SerializeField] private string puzzleId = "first-memory-sequence";
        [SerializeField] private int[] solution = { 1, 2, 3 };
        private MemoryPuzzleFlow flow;
        private MemoryPuzzleState state;
        private string SaveKey => "stage13.memory-puzzle." + puzzleId;
        public bool IsCompleted => state != null && state.Completed;
        public int Progress => state?.Progress ?? 0;

        private void Awake()
        {
            flow = new MemoryPuzzleFlow(puzzleId, solution);
            state = new MemoryPuzzleState { PuzzleId = puzzleId, Progress = PlayerPrefs.GetInt(SaveKey + ".progress", 0), Completed = PlayerPrefs.GetInt(SaveKey + ".completed", 0) != 0, Mistakes = PlayerPrefs.GetInt(SaveKey + ".mistakes", 0) };
            state = flow.Normalize(state);
        }

        public bool SubmitChoice(int choice)
        {
            bool accepted = flow.Submit(state, choice);
            PlayerPrefs.SetInt(SaveKey + ".progress", state.Progress);
            PlayerPrefs.SetInt(SaveKey + ".completed", state.Completed ? 1 : 0);
            PlayerPrefs.SetInt(SaveKey + ".mistakes", state.Mistakes);
            PlayerPrefs.Save();
            return accepted;
        }

        public void ResetPuzzle()
        {
            state = new MemoryPuzzleState { PuzzleId = puzzleId };
            PlayerPrefs.DeleteKey(SaveKey + ".progress"); PlayerPrefs.DeleteKey(SaveKey + ".completed"); PlayerPrefs.DeleteKey(SaveKey + ".mistakes"); PlayerPrefs.Save();
        }
    }
}
