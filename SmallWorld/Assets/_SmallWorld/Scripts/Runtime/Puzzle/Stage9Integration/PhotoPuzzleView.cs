using System;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Player;
using SmallWorld.Puzzle.Stage9;
using SmallWorld.Puzzle.Stage9.Persistence;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Puzzle.Stage9Integration
{
    public sealed class PhotoPuzzleView : MonoBehaviour
    {
        public const string PuzzleId = "reality.photo_sequence";
        public const string CompletionRecordId = "photo.restored_room";
        public const string PersistenceKey = PhotoPuzzleSaveContract.Key;

        [SerializeField] private CanvasGroup panel;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text progressText;
        [SerializeField] private Button[] pieceButtons = Array.Empty<Button>();
        [SerializeField] private Button closeButton;
        [SerializeField] private FirstPersonPlayerController player;
        [SerializeField] private Stage6UIController stage6UI;
        [SerializeField] private Stage7DialogueView dialogueView;
        [SerializeField] private Stage8RecordView recordView;
        [SerializeField] private GameObject modelHouseRoof;
        [SerializeField] private string persistenceKey = PersistenceKey;

        private static readonly int[] CorrectOrder = { 1, 0, 2 };
        private PuzzleRuntime runtime;
        private PhotoPuzzlePersistence persistence;
        private bool restoring;

        public bool IsOpen => IsVisible(panel);
        public bool IsCompleted => TryGetState(out PuzzleState state) && state.Status == PuzzleStatus.Completed;
        public PuzzleState CurrentState { get { TryGetState(out PuzzleState state); return state; } }
        public string StorageKey => persistenceKey;

        public void Configure(CanvasGroup root, Text instruction, Text feedback, Text progress,
            Button[] choices, Button close, FirstPersonPlayerController playerController,
            Stage6UIController stage6Controller, Stage7DialogueView dialogue,
            Stage8RecordView records, GameObject roof)
        {
            Unbind();
            panel = root;
            instructionText = instruction;
            feedbackText = feedback;
            progressText = progress;
            pieceButtons = choices ?? Array.Empty<Button>();
            closeButton = close;
            player = playerController;
            stage6UI = stage6Controller;
            dialogueView = dialogue;
            recordView = records;
            modelHouseRoof = roof;
            CreateRuntime();
            Bind();
            SetVisible(panel, false);
            RenderState();
        }

        private void Awake()
        {
            if (runtime == null) CreateRuntime();
            Bind();
            SetVisible(panel, false);
            RenderState();
        }

        private void OnDestroy()
        {
            Unbind();
            UnbindRuntime();
        }

        public bool Open()
        {
            if (IsOpen || IsCompleted || stage6UI == null || stage6UI.StateMachine.Current != UIState.Gameplay ||
                (dialogueView != null && dialogueView.IsDialogueActive) ||
                (recordView != null && recordView.IsOpen)) return false;

            PuzzleActionResult result = runtime.Start(PuzzleId);
            if (result != PuzzleActionResult.Accepted && result != PuzzleActionResult.AlreadyStarted) return false;
            SetVisible(panel, true);
            if (player != null) player.enabled = false;
            DialogueCursorMode.RequestUi();
            if (feedbackText != null) feedbackText.text = "사진의 흔적을 따라 조각을 골라 보세요.";
            RenderState();
            return true;
        }

        public bool Close()
        {
            if (!IsOpen) return false;
            SetVisible(panel, false);
            if (CanRestoreGameplay())
            {
                if (player != null) player.enabled = true;
                DialogueCursorMode.RequestGameplay();
            }
            else DialogueCursorMode.RequestUi();
            return true;
        }

        public PuzzleActionResult SelectPiece(int pieceIndex)
        {
            if (!IsOpen || pieceIndex < 0 || pieceIndex >= pieceButtons.Length)
                return PuzzleActionResult.NotStarted;
            PuzzleState state = CurrentState;
            bool correct = state != null && state.CurrentStep < CorrectOrder.Length &&
                CorrectOrder[state.CurrentStep] == pieceIndex;
            PuzzleActionResult result = runtime.Submit(PuzzleId, correct);
            if (result == PuzzleActionResult.Incorrect && feedbackText != null)
                feedbackText.text = CurrentState != null && CurrentState.IncorrectAttempts > 1
                    ? "이 순서가 아닙니다. 힌트: 창문의 빛 다음에는 현관, 마지막은 지붕입니다."
                    : "이 순서가 아닙니다. 힌트: 가장 밝은 테두리 조각부터 시작하세요.";
            else if (IsCompleted)
            {
                if (feedbackText != null) feedbackText.text = "사진이 완성되자 모형 집의 지붕이 열렸습니다.";
                SetButtonsInteractable(false);
            }
            else if (feedbackText != null) feedbackText.text = "맞는 조각입니다. 다음 흔적을 이어 보세요.";
            RenderState();
            return result;
        }

        public void SelectFirst() => SelectPiece(0);
        public void SelectSecond() => SelectPiece(1);
        public void SelectThird() => SelectPiece(2);

        public PuzzleSnapshot CaptureSnapshot() => runtime.CaptureSnapshot();

        public void ConfigurePersistence(IPhotoPuzzleStorage storage, bool restoreImmediately = true)
        {
            persistence = new PhotoPuzzlePersistence(persistenceKey, storage);
            if (restoreImmediately) RestoreSavedProgress();
        }

        public bool RestoreSavedProgress()
        {
            if (persistence == null) return false;
            if (!persistence.TryRestore(out PuzzleSnapshot snapshot)) return false;
            try
            {
                RestoreSnapshot(snapshot);
                return true;
            }
            catch (Exception)
            {
                persistence.QuarantineCurrent();
                CreateRuntime();
                SetButtonsInteractable(true);
                return false;
            }
        }

        public void ClearSavedProgress() => persistence?.Clear();

        public static void ClearSavedProgress(IPhotoPuzzleStorage storage) =>
            PhotoPuzzleSaveContract.Clear(storage);

        public void RestoreSnapshot(PuzzleSnapshot snapshot)
        {
            restoring = true;
            try { runtime.RestoreSnapshot(snapshot); }
            finally { restoring = false; }
            if (!IsCompleted) return;
            ApplyCompletedSpatialState(isRestore: true);
            EnsureCompletionRecord();
            SetButtonsInteractable(false);
            SetVisible(panel, false);
        }

        private void CreateRuntime()
        {
            UnbindRuntime();
            var definition = new PuzzleDefinition(PuzzleId, CorrectOrder.Length,
                new[] { new HintRule("photo.edge", 1), new HintRule("photo.light", 2) },
                new[] { new SpatialChangeCommand("model-house.roof", "SetActive", "false") });
            runtime = new PuzzleRuntime(new[] { definition }, new DelegatePuzzleCompletionSink(OnPuzzleCompleted));
            runtime.StateChanged += OnStateChanged;
            runtime.HintAvailable += OnHintAvailable;
            runtime.SpatialChangeRequested += OnSpatialChangeRequested;
        }

        private void UnbindRuntime()
        {
            if (runtime == null) return;
            runtime.StateChanged -= OnStateChanged;
            runtime.HintAvailable -= OnHintAvailable;
            runtime.SpatialChangeRequested -= OnSpatialChangeRequested;
        }

        private void Bind()
        {
            Unbind();
            if (pieceButtons.Length > 0) pieceButtons[0]?.onClick.AddListener(SelectFirst);
            if (pieceButtons.Length > 1) pieceButtons[1]?.onClick.AddListener(SelectSecond);
            if (pieceButtons.Length > 2) pieceButtons[2]?.onClick.AddListener(SelectThird);
            closeButton?.onClick.AddListener(CloseFromButton);
            if (stage6UI != null) stage6UI.StateMachine.Changed += OnUIStateChanged;
            if (dialogueView != null) dialogueView.DialogueActivityChanged += OnDialogueActivityChanged;
        }

        private void Unbind()
        {
            if (pieceButtons.Length > 0) pieceButtons[0]?.onClick.RemoveListener(SelectFirst);
            if (pieceButtons.Length > 1) pieceButtons[1]?.onClick.RemoveListener(SelectSecond);
            if (pieceButtons.Length > 2) pieceButtons[2]?.onClick.RemoveListener(SelectThird);
            closeButton?.onClick.RemoveListener(CloseFromButton);
            if (stage6UI != null) stage6UI.StateMachine.Changed -= OnUIStateChanged;
            if (dialogueView != null) dialogueView.DialogueActivityChanged -= OnDialogueActivityChanged;
        }

        private void OnStateChanged(PuzzleStateChangedEvent change)
        {
            RenderState(change.State);
            if (!restoring && persistence != null) persistence.Save(runtime.CaptureSnapshot());
        }

        private void OnHintAvailable(HintAvailableEvent hint)
        {
            if (feedbackText == null) return;
            feedbackText.text = hint.HintId == "photo.edge"
                ? "힌트: 가장 밝은 테두리 조각부터 시작하세요."
                : "힌트: 창문의 빛 다음에는 현관, 마지막은 지붕입니다.";
        }

        private void OnSpatialChangeRequested(SpatialChangeRequestedEvent request)
        {
            if (request.Command.TargetId == "model-house.roof" && request.Command.Operation == "SetActive" &&
                modelHouseRoof != null)
                modelHouseRoof.SetActive(!string.Equals(request.Command.Value, "false", StringComparison.OrdinalIgnoreCase));
        }

        private void OnPuzzleCompleted(string puzzleId)
        {
            if (puzzleId != PuzzleId || recordView == null) return;
            EnsureCompletionRecord();
        }

        private void EnsureCompletionRecord()
        {
            if (recordView == null) return;
            recordView.AddRecord(new InventoryRecord(CompletionRecordId, RecordKind.Photo, "복원된 방 사진",
                "세 조각을 올바른 순서로 맞추자 사진 속 집과 모형 집이 함께 반응했다.", 60));
        }

        private void ApplyCompletedSpatialState(bool isRestore)
        {
            if (!isRestore || modelHouseRoof == null) return;
            modelHouseRoof.SetActive(false);
        }

        private void RenderState() => RenderState(CurrentState);

        private void RenderState(PuzzleState state)
        {
            if (state == null) return;
            if (instructionText != null) instructionText.text = "사진 조각 순서 맞추기";
            if (progressText != null)
                progressText.text = state.Status == PuzzleStatus.Completed ? "완료" :
                    $"진행 {state.CurrentStep} / {state.StepCount}  ·  오답 {state.IncorrectAttempts}";
        }

        private bool TryGetState(out PuzzleState state)
        {
            state = null;
            if (runtime == null) return false;
            return runtime.TryGetState(PuzzleId, out state);
        }
        private void SetButtonsInteractable(bool value)
        {
            for (int i = 0; i < pieceButtons.Length; i++) if (pieceButtons[i] != null) pieceButtons[i].interactable = value;
        }
        private void CloseFromButton() => Close();
        private void OnUIStateChanged(UIState previous, UIState current) { if (current != UIState.Gameplay && IsOpen) Close(); }
        private void OnDialogueActivityChanged(bool active) { if (active && IsOpen) Close(); }
        private bool CanRestoreGameplay() => stage6UI != null && stage6UI.StateMachine.Current == UIState.Gameplay &&
            (dialogueView == null || !dialogueView.IsDialogueActive) && (recordView == null || !recordView.IsOpen);
        private static bool IsVisible(CanvasGroup group) => group != null && group.alpha > 0f && group.interactable && group.blocksRaycasts;
        private static void SetVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
