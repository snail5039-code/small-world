using System.Threading.Tasks;
using SmallWorld.Core;
using SmallWorld.Save.Stage10;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.Save.Stage12;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    /// <summary>Stage 12 first memory space: safe zone and explicit return to the white room.</summary>
    public sealed class Stage12MemorySpaceController : MonoBehaviour
    {
        [SerializeField] private Transform safeZone;
        [SerializeField] private Light memoryLight;
        [SerializeField] private Renderer[] memoryMarkers = System.Array.Empty<Renderer>();
        [SerializeField] private Color activeColor = new Color(0.35f, 0.65f, 1f, 1f);
        [SerializeField] private Color completedColor = new Color(0.75f, 1f, 0.8f, 1f);
        [SerializeField] private string memorySpaceId = "first-memory";
        [SerializeField] private string progressKey = "stage12.memory.first.entered";
        public bool HasEntered { get; private set; }
        public bool IsExitBlocked { get; private set; }

        private Stage13MemoryPuzzleController puzzleController;
        private MemoryJourneyFlow journeyFlow;

        private void Awake()
        {
            HasEntered = PlayerPrefs.GetInt(progressKey, 0) != 0;
            if (memoryLight == null) memoryLight = GetComponentInChildren<Light>(true);
            if (memoryMarkers == null || memoryMarkers.Length == 0)
                memoryMarkers = GetComponentsInChildren<Renderer>(true);
            puzzleController = GetComponent<Stage13MemoryPuzzleController>();
            if (puzzleController == null) puzzleController = GetComponentInChildren<Stage13MemoryPuzzleController>(true);
            if (puzzleController != null) puzzleController.ChoiceSubmitted += OnPuzzleChoiceSubmitted;
            journeyFlow = CreateJourneyFlow();
        }

        private void OnDestroy()
        {
            if (puzzleController != null) puzzleController.ChoiceSubmitted -= OnPuzzleChoiceSubmitted;
        }

        private void Start()
        {
            HasEntered = true;
            PlayerPrefs.SetInt(progressKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[Stage12] First memory space entered; safe zone active.", this);
            ApplyPresentation(false);
            SaveData data = LoadLatest();
            journeyFlow.Enter(data);
            if (puzzleController != null) puzzleController.Restore(journeyFlow.RestorePuzzle(data));
            Save(data);
        }

        private void OnPuzzleChoiceSubmitted(int choice)
        {
            SaveData data = LoadLatest();
            journeyFlow.SubmitChoice(data, choice);
            Save(data);
        }

        private void UpdatePresentation()
        {
            if (memoryLight == null) return;
            float pulse = 0.9f + Mathf.Sin(Time.time * 1.6f) * 0.1f;
            memoryLight.intensity = (HasEntered ? 2f : 1f) * pulse;
        }

        private void LateUpdate() => UpdatePresentation();

        private void ApplyPresentation(bool completed)
        {
            Color color = completed ? completedColor : activeColor;
            if (memoryLight != null) memoryLight.color = color;
            for (int i = 0; i < memoryMarkers.Length; i++)
            {
                Renderer marker = memoryMarkers[i];
                if (marker == null || marker.material == null) continue;
                if (marker.material.HasProperty("_BaseColor")) marker.material.SetColor("_BaseColor", color);
                else if (marker.material.HasProperty("_Color")) marker.material.color = color;
            }
        }

        private async void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                await ReturnToWhiteRoom();
        }

        public bool IsInSafeZone(Vector3 position) => safeZone == null ||
            Vector3.Distance(position, safeZone.position) <= 2.5f;

        public async Task ReturnToWhiteRoom()
        {
            SaveData data = LoadLatest();
            if (journeyFlow.TryExit(data) == MemoryExitResult.BlockedByPuzzle)
            {
                IsExitBlocked = true;
                Debug.Log("[Stage14] Memory exit blocked until the puzzle is solved.", this);
                return;
            }

            IsExitBlocked = false;
            PlayerPrefs.SetInt("stage12.memory.first.exited", 1);
            PlayerPrefs.Save();
            Save(data);
            ApplyPresentation(true);
            Debug.Log("[Stage13] Memory exit presentation completed; white room return queued.", this);
            if (SceneTransitionService.Instance != null)
                await SceneTransitionService.Instance.LoadSceneAsync(SceneId.RealityRoom);
        }

        private MemoryJourneyFlow CreateJourneyFlow()
        {
            string puzzleId = puzzleController != null ? puzzleController.PuzzleId : "first-memory-sequence";
            int[] solution = puzzleController != null ? puzzleController.Solution : new[] { 1, 2, 3 };
            return new MemoryJourneyFlow(new MemorySpaceDefinition
            {
                Id = memorySpaceId,
                EntrySceneId = SceneId.FirstMemory.ToString(),
                ReturnSceneId = SceneId.RealityRoom.ToString(),
                SafeZoneId = "first-memory-safe-zone"
            }, puzzleId, solution);
        }

        private static SaveData LoadLatest()
        {
            SaveReadResult latest = Stage10SaveRuntime.FindLatest();
            return latest.IsSuccess && latest.Data != null ? latest.Data : SaveData.CreateNew();
        }

        private static void Save(SaveData data) => Stage10SaveRuntime.Service.AutoSave(data);
    }
}
