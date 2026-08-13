using System;
using SmallWorld.Player;
using SmallWorld.Save.Stage10.Integration;
using SmallWorld.UI.Stage7;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    public enum StoryRouteStep { Dialogue, Puzzle, Memory }

    public interface IStoryRouteProgressSource
    {
        bool IsNodeUnlocked(string nodeId);
        bool IsFinalGateUnlocked { get; }
        void ReportNodeReached(string nodeId);
        void ReportStep(string nodeId, StoryRouteStep step);
    }

    [Serializable]
    public sealed class StoryRouteNode
    {
        public string Id;
        public string DisplayName;
        public Transform Arrival;
        public Transform DialogueEntry;
        public Transform PuzzleEntry;
        public Transform MemoryEntry;
    }

    public sealed class StoryRouteController : MonoBehaviour
    {
        private enum RuntimeOverlay { None, Records, Paused }

        [SerializeField] private Transform player;
        [SerializeField] private StoryRouteNode[] nodes = Array.Empty<StoryRouteNode>();
        [SerializeField] private int fallbackUnlockedIndex;

        private IStoryRouteProgressSource progressSource;
        private RuntimeOverlay runtimeOverlay;
        private FirstPersonPlayerController playerController;
        private Stage10ManualSavePanel savePanel;
        private bool playerWasEnabled;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;
        private float timeScaleBeforePause = 1f;
        private bool inputStateCaptured;

        public int NodeCount => nodes?.Length ?? 0;
        public int FallbackUnlockedIndex => fallbackUnlockedIndex;
        public bool IsFinalGateUnlocked => progressSource?.IsFinalGateUnlocked ?? false;
        public bool IsRuntimeOverlayOpen => runtimeOverlay != RuntimeOverlay.None;
        public bool IsRuntimePaused => runtimeOverlay == RuntimeOverlay.Paused;

        public void Configure(Transform playerTransform, StoryRouteNode[] routeNodes)
        {
            player = playerTransform;
            nodes = routeNodes ?? Array.Empty<StoryRouteNode>();
        }

        public void BindProgressSource(IStoryRouteProgressSource source) => progressSource = source;

        public int RestoreToNodeOrPrologue(int requestedIndex)
        {
            int safeIndex = nodes != null && requestedIndex >= 0 && requestedIndex < nodes.Length
                ? requestedIndex
                : 0;
            if (nodes == null || nodes.Length == 0 || nodes[safeIndex]?.Arrival == null || player == null)
                return -1;

            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.SetPositionAndRotation(nodes[safeIndex].Arrival.position, nodes[safeIndex].Arrival.rotation);
            if (character != null) character.enabled = true;
            fallbackUnlockedIndex = Mathf.Max(fallbackUnlockedIndex, safeIndex);
            return safeIndex;
        }

        public void ReportStep(string nodeId, StoryRouteStep step) => progressSource?.ReportStep(nodeId, step);

        private void Awake()
        {
            ResolveRuntimeInputOwners();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleTabPressed();
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame) HandleEscapePressed();
        }

        private void OnDestroy()
        {
            CloseRuntimeOverlay();
        }

        public bool HandleTabPressed()
        {
            if (IsSaveMenuOpen()) return false;
            if (runtimeOverlay == RuntimeOverlay.Paused) return true;
            SetRuntimeOverlay(runtimeOverlay == RuntimeOverlay.Records
                ? RuntimeOverlay.None
                : RuntimeOverlay.Records);
            return true;
        }

        public bool HandleEscapePressed()
        {
            if (IsSaveMenuOpen()) return false;
            SetRuntimeOverlay(runtimeOverlay == RuntimeOverlay.None
                ? RuntimeOverlay.Paused
                : RuntimeOverlay.None);
            return true;
        }

        private void SetRuntimeOverlay(RuntimeOverlay overlay)
        {
            if (runtimeOverlay == overlay) return;
            if (runtimeOverlay == RuntimeOverlay.None && overlay != RuntimeOverlay.None)
                CaptureGameplayInputState();

            if (runtimeOverlay == RuntimeOverlay.Paused && overlay != RuntimeOverlay.Paused)
                RestoreTimeScale();

            runtimeOverlay = overlay;
            if (runtimeOverlay == RuntimeOverlay.Paused)
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (runtimeOverlay == RuntimeOverlay.None) RestoreGameplayInputState();
        }

        private void CloseRuntimeOverlay()
        {
            if (runtimeOverlay == RuntimeOverlay.Paused) RestoreTimeScale();
            runtimeOverlay = RuntimeOverlay.None;
            RestoreGameplayInputState();
        }

        private void CaptureGameplayInputState()
        {
            if (inputStateCaptured) return;
            ResolveRuntimeInputOwners();
            playerWasEnabled = playerController != null && playerController.enabled;
            previousCursorLockState = DialogueCursorMode.RequestedLockState;
            previousCursorVisible = DialogueCursorMode.RequestedVisible;
            inputStateCaptured = true;
            if (playerWasEnabled) playerController.enabled = false;
            DialogueCursorMode.RequestUi();
        }

        private void RestoreGameplayInputState()
        {
            if (!inputStateCaptured) return;
            inputStateCaptured = false;
            if (playerController != null) playerController.enabled = playerWasEnabled;
            DialogueCursorMode.Restore(previousCursorLockState, previousCursorVisible);
        }

        private void ResolveRuntimeInputOwners()
        {
            if (playerController == null && player != null)
                playerController = player.GetComponent<FirstPersonPlayerController>();
            if (playerController == null)
                playerController = FindFirstObjectByType<FirstPersonPlayerController>();
            if (savePanel == null)
                savePanel = FindFirstObjectByType<Stage10ManualSavePanel>(FindObjectsInactive.Include);
        }

        private bool IsSaveMenuOpen()
        {
            ResolveRuntimeInputOwners();
            return savePanel != null && savePanel.IsOpen;
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f) Time.timeScale = Mathf.Max(0.0001f, timeScaleBeforePause);
        }

        private void OnGUI()
        {
            if (runtimeOverlay == RuntimeOverlay.None) return;
            float width = Mathf.Min(620f, Screen.width - 40f);
            float height = runtimeOverlay == RuntimeOverlay.Paused ? 220f : 360f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, runtimeOverlay == RuntimeOverlay.Paused ? "Paused" : "Records");
            Rect message = new Rect(panel.x + 28f, panel.y + 62f, panel.width - 56f, panel.height - 105f);
            GUI.Label(message, runtimeOverlay == RuntimeOverlay.Paused
                ? "Press Esc to return to the story."
                : "No route records have been collected yet.\n\nPress Tab or Esc to close.");
        }

        public bool TryTravelTo(int index, out string feedback)
        {
            if (nodes == null || index < 0 || index >= nodes.Length || nodes[index]?.Arrival == null)
            {
                feedback = "The story route node is not configured.";
                return false;
            }

            StoryRouteNode node = nodes[index];
            bool unlocked = progressSource != null
                ? progressSource.IsNodeUnlocked(node.Id)
                : index <= fallbackUnlockedIndex;
            if (!unlocked)
            {
                feedback = $"{node.DisplayName} is still sealed.";
                return false;
            }

            if (player == null)
            {
                feedback = "The route player is unavailable.";
                return false;
            }

            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.SetPositionAndRotation(node.Arrival.position, node.Arrival.rotation);
            if (character != null) character.enabled = true;
            progressSource?.ReportNodeReached(node.Id);
            fallbackUnlockedIndex = Mathf.Max(fallbackUnlockedIndex, Mathf.Min(index + 1, nodes.Length - 1));
            feedback = $"Entered {node.DisplayName}.";
            return true;
        }
    }
}
