using System;
using SmallWorld.Player;
using SmallWorld.Save.Stage10;
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
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text[] slotMetadataTexts = Array.Empty<Text>();
        private RealityRoomSaveCoordinator coordinator;
        private Func<SaveData> captureSave;
        private Func<int, bool> loadSave;
        [SerializeField] private FirstPersonPlayerController player;
        private bool inputStateCaptured;
        private bool playerWasEnabled;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;
        private float previousTimeScale;
        public bool IsOpen => IsVisible();
        public string LastFeedback { get; private set; } = string.Empty;
        public string DisplayedFeedback => feedbackText != null ? feedbackText.text : LastFeedback;
        public void Configure(CanvasGroup root, Button[] saves, Button[] loads, Button close)
        {
            panel = root; saveButtons = saves ?? Array.Empty<Button>(); loadButtons = loads ?? Array.Empty<Button>(); closeButton = close;
            ApplyTheme(); Bind(); Close();
        }
        public void ConfigureFeedback(Text feedback, Text[] slotMetadata)
        {
            feedbackText = feedback;
            slotMetadataTexts = slotMetadata ?? Array.Empty<Text>();
            SmallWorldUiTheme.ApplyText(feedbackText, SmallWorldTextRole.Feedback);
            for (int i = 0; i < slotMetadataTexts.Length; i++)
                SmallWorldUiTheme.ApplyText(slotMetadataTexts[i], SmallWorldTextRole.Body);
            RefreshLoads();
            PresentFeedback(LastFeedback);
        }
        public void Configure(RealityRoomSaveCoordinator value) => coordinator = value;
        public void Configure(Func<SaveData> capture, Func<int, bool> load = null)
        {
            captureSave = capture;
            loadSave = load;
        }
        public void Configure(FirstPersonPlayerController value) => player = value;
        private void Awake() { ApplyTheme(); Bind(); Close(); }
        private void OnDestroy() { RestoreInputState(); Unbind(); }
        public void Open()
        {
            if (IsVisible() || IsAnotherUiOpen()) return;
            CaptureInputState();
            SetVisible(true);
            RefreshLoads();
            if (string.IsNullOrWhiteSpace(LastFeedback)) PresentFeedback("저장하거나 불러올 슬롯을 선택하세요.");
        }
        public void Close()
        {
            SetVisible(false);
            RestoreInputState();
        }
        public void Save0() => Save(0); public void Save1() => Save(1); public void Save2() => Save(2);
        public void Load0() => Load(0); public void Load1() => Load(1); public void Load2() => Load(2);
        private void Save(int slot)
        {
            bool saved = coordinator != null
                ? coordinator.SaveManual(slot)
                : captureSave != null && Stage10SaveRuntime.Service.SaveManual(slot, captureSave());
            PresentFeedback(saved ? $"슬롯 {slot + 1}에 저장했습니다." : $"슬롯 {slot + 1} 저장에 실패했습니다.");
            RefreshLoads();
        }
        private void Load(int slot)
        {
            bool loaded = coordinator != null ? coordinator.LoadManual(slot) : loadSave != null && loadSave(slot);
            PresentFeedback(loaded ? $"슬롯 {slot + 1}을 불러왔습니다." : $"슬롯 {slot + 1}을 불러오지 못했습니다.");
            if (loaded) Close(); else RefreshLoads();
        }
        private void RefreshLoads()
        {
            for (int i = 0; i < loadButtons.Length && i < 3; i++)
            {
                Button button = loadButtons[i];
                if (button == null) continue;
                SaveReadResult read = Stage10SaveRuntime.Service.LoadManual(i);
                button.interactable = read.IsSuccess;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.text = read.IsSuccess
                    ? $"슬롯 {i + 1} 불러오기 · {SceneLabel(read.Data)}"
                    : $"슬롯 {i + 1} · 비어 있음";
                if (i < slotMetadataTexts.Length && slotMetadataTexts[i] != null)
                    slotMetadataTexts[i].text = read.IsSuccess
                        ? $"저장됨 · {SceneLabel(read.Data)}"
                        : "비어 있는 슬롯";
            }
        }
        private void PresentFeedback(string message)
        {
            LastFeedback = message ?? string.Empty;
            if (feedbackText == null) return;
            feedbackText.text = LastFeedback;
            feedbackText.color = SmallWorldUiTheme.FeedbackColor(LastFeedback);
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(LastFeedback));
        }
        private static string SceneLabel(SaveData data)
        {
            if (data == null) return "알 수 없음";
            if (!string.IsNullOrWhiteSpace(data.CheckpointId)) return data.CheckpointId;
            return string.IsNullOrWhiteSpace(data.ActiveSceneId) ? "진행 기록" : data.ActiveSceneId;
        }
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
        private void ApplyTheme()
        {
            SmallWorldUiTheme.ApplyPanel(panel, true);
            for (int i = 0; i < saveButtons.Length; i++)
                SmallWorldUiTheme.ApplyButton(saveButtons[i], $"슬롯 {i + 1} 저장");
            for (int i = 0; i < loadButtons.Length; i++)
                SmallWorldUiTheme.ApplyButton(loadButtons[i], $"슬롯 {i + 1} 불러오기");
            SmallWorldUiTheme.ApplyButton(closeButton, "닫기");
            SmallWorldUiTheme.ApplyText(feedbackText, SmallWorldTextRole.Feedback);
            for (int i = 0; i < slotMetadataTexts.Length; i++)
                SmallWorldUiTheme.ApplyText(slotMetadataTexts[i], SmallWorldTextRole.Body);
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
