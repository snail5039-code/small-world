using System;
using SmallWorld.Player;
using SmallWorld.Puzzle.Stage9Integration;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Save.Stage10.Integration
{
    public sealed class Stage10ManualSavePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Button[] saveButtons = Array.Empty<Button>();
        [SerializeField] private Button[] loadButtons = Array.Empty<Button>();
        [SerializeField] private Button closeButton;
        private RealityRoomSaveCoordinator coordinator;
        [SerializeField] private FirstPersonPlayerController player;
        private bool inputStateCaptured;
        private bool playerWasEnabled;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;
        private float previousTimeScale;
        public bool IsOpen => IsVisible();
        public void Configure(CanvasGroup root, Button[] saves, Button[] loads, Button close) { panel = root; saveButtons = saves ?? Array.Empty<Button>(); loadButtons = loads ?? Array.Empty<Button>(); closeButton = close; Bind(); Close(); }
        public void Configure(RealityRoomSaveCoordinator value) => coordinator = value;
        public void Configure(FirstPersonPlayerController value) => player = value;
        private void Awake() { Bind(); Close(); }
        private void OnDestroy() { RestoreInputState(); Unbind(); }
        public void Open()
        {
            if (IsVisible() || IsAnotherUiOpen()) return;
            CaptureInputState();
            SetVisible(true);
            RefreshLoads();
        }
        public void Close()
        {
            SetVisible(false);
            RestoreInputState();
        }
        public void Save0() => Save(0); public void Save1() => Save(1); public void Save2() => Save(2);
        public void Load0() => Load(0); public void Load1() => Load(1); public void Load2() => Load(2);
        private void Save(int slot) { coordinator?.SaveManual(slot); RefreshLoads(); }
        private void Load(int slot) { if (coordinator != null && coordinator.LoadManual(slot)) Close(); }
        private void RefreshLoads() { for (int i = 0; i < loadButtons.Length && i < 3; i++) if (loadButtons[i] != null) loadButtons[i].interactable = Stage10SaveRuntime.Service.LoadManual(i).IsSuccess; }
        private void Bind()
        {
            Unbind();
            if (saveButtons.Length > 0) saveButtons[0]?.onClick.AddListener(Save0); if (saveButtons.Length > 1) saveButtons[1]?.onClick.AddListener(Save1); if (saveButtons.Length > 2) saveButtons[2]?.onClick.AddListener(Save2);
            if (loadButtons.Length > 0) loadButtons[0]?.onClick.AddListener(Load0); if (loadButtons.Length > 1) loadButtons[1]?.onClick.AddListener(Load1); if (loadButtons.Length > 2) loadButtons[2]?.onClick.AddListener(Load2);
            closeButton?.onClick.AddListener(Close);
        }
        private void Unbind()
        {
            if (saveButtons.Length > 0) saveButtons[0]?.onClick.RemoveListener(Save0); if (saveButtons.Length > 1) saveButtons[1]?.onClick.RemoveListener(Save1); if (saveButtons.Length > 2) saveButtons[2]?.onClick.RemoveListener(Save2);
            if (loadButtons.Length > 0) loadButtons[0]?.onClick.RemoveListener(Load0); if (loadButtons.Length > 1) loadButtons[1]?.onClick.RemoveListener(Load1); if (loadButtons.Length > 2) loadButtons[2]?.onClick.RemoveListener(Load2);
            closeButton?.onClick.RemoveListener(Close);
        }
        private void CaptureInputState()
        {
            if (player == null) player = FindFirstObjectByType<FirstPersonPlayerController>();
            playerWasEnabled = player != null && player.enabled;
            previousCursorLockState = DialogueCursorMode.RequestedLockState;
            previousCursorVisible = DialogueCursorMode.RequestedVisible;
            previousTimeScale = Time.timeScale;
            inputStateCaptured = true;
            if (playerWasEnabled) player.enabled = false;
            DialogueCursorMode.RequestUi();
        }
        private void RestoreInputState()
        {
            if (!inputStateCaptured) return;
            inputStateCaptured = false;
            if (player != null) player.enabled = playerWasEnabled;
            DialogueCursorMode.Restore(previousCursorLockState, previousCursorVisible);
            Time.timeScale = previousTimeScale;
        }
        private bool IsVisible() => panel != null && panel.alpha > 0f && panel.interactable && panel.blocksRaycasts;
        private static bool IsAnotherUiOpen()
        {
            Stage7DialogueView dialogue = FindFirstObjectByType<Stage7DialogueView>(FindObjectsInactive.Include);
            if (dialogue != null && dialogue.IsDialogueActive) return true;
            Stage8RecordView records = FindFirstObjectByType<Stage8RecordView>(FindObjectsInactive.Include);
            if (records != null && records.IsOpen) return true;
            PhotoPuzzleView puzzle = FindFirstObjectByType<PhotoPuzzleView>(FindObjectsInactive.Include);
            if (puzzle != null && puzzle.IsOpen) return true;
            Stage6UIController ui = FindFirstObjectByType<Stage6UIController>(FindObjectsInactive.Include);
            return ui != null && ui.StateMachine.Current != UIState.Gameplay;
        }
        private void SetVisible(bool visible) { if (panel == null) return; panel.alpha = visible ? 1f : 0f; panel.interactable = visible; panel.blocksRaycasts = visible; }
    }
}
