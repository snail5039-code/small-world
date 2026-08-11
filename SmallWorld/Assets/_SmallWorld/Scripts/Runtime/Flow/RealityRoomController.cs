using SmallWorld.Core;
using SmallWorld.Player;
using SmallWorld.Inventory.Stage8;
using SmallWorld.Puzzle.Stage9Integration;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
using SmallWorld.UI.Stage8;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    public sealed class RealityRoomController : MonoBehaviour
    {
        [SerializeField] private Stage6UIController stage6UI;
        [SerializeField] private InspectionView inspectionView;
        [SerializeField] private NotificationQueueView notifications;
        [SerializeField] private Stage6LoadingView loadingView;
        [SerializeField] private FirstPersonPlayerController player;
        [SerializeField] private PlayerInteractionDetector interactionDetector;
        [SerializeField] private Stage7DialogueView dialogueView;
        [SerializeField] private Stage8RecordView recordView;
        [SerializeField] private PhotoPuzzleView photoPuzzleView;

        private InteractableBase[] trackedInteractables = System.Array.Empty<InteractableBase>();

        public void ConfigureStage6(Stage6UIController controller, InspectionView inspection,
            NotificationQueueView notificationView, Stage6LoadingView loading,
            FirstPersonPlayerController playerController, PlayerInteractionDetector detector)
        {
            stage6UI = controller;
            inspectionView = inspection;
            notifications = notificationView;
            loadingView = loading;
            player = playerController;
            interactionDetector = detector;
        }

        public void ConfigureStage7(Stage7DialogueView dialogue)
        {
            dialogueView = dialogue;
        }

        public void ConfigureStage8(Stage8RecordView records)
        {
            if (recordView != null) recordView.NewRecordAdded -= OnNewRecordAdded;
            recordView = records;
            if (recordView != null) recordView.NewRecordAdded += OnNewRecordAdded;
        }

        public void ConfigureStage9(PhotoPuzzleView photoPuzzle)
        {
            photoPuzzleView = photoPuzzle;
        }

        private void Awake()
        {
            if (dialogueView == null) dialogueView = FindFirstObjectByType<Stage7DialogueView>();
            if (recordView == null) recordView = FindFirstObjectByType<Stage8RecordView>();
            if (recordView != null) recordView.NewRecordAdded += OnNewRecordAdded;
            if (stage6UI == null) return;
            stage6UI.ResumeRequested += RestoreGameplay;
            stage6UI.ReturnToTitleRequested += ReturnToTitle;
            stage6UI.QuitRequested += QuitGame;
            stage6UI.StateMachine.Changed += OnUIStateChanged;
            if (inspectionView != null) inspectionView.CloseRequested += CloseInspection;
            SubscribeInteractions();
            stage6UI.ConfigureInitialState(UIState.Gameplay);
        }

        private void OnDestroy()
        {
            if (stage6UI != null)
            {
                stage6UI.ResumeRequested -= RestoreGameplay;
                stage6UI.ReturnToTitleRequested -= ReturnToTitle;
                stage6UI.QuitRequested -= QuitGame;
                stage6UI.StateMachine.Changed -= OnUIStateChanged;
            }
            if (inspectionView != null) inspectionView.CloseRequested -= CloseInspection;
            if (recordView != null) recordView.NewRecordAdded -= OnNewRecordAdded;
            UnsubscribeInteractions();
            RestoreRuntimeState();
        }

        private async void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                if (SceneTransitionService.Instance != null)
                    await SceneTransitionService.Instance.LoadSceneAsync(SceneId.FirstMemory);
                return;
            }
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (photoPuzzleView != null && photoPuzzleView.IsOpen) return;
                recordView?.Toggle();
                return;
            }
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (HandleEscapePressed()) return;

            if (SceneTransitionService.Instance == null)
            {
                Debug.LogError("[SmallWorld] Cannot return: SceneTransitionService is missing.", this);
                return;
            }

            await SceneTransitionService.Instance.LoadSceneAsync(SceneId.MainMenu);
        }

        internal bool HandleEscapePressed()
        {
            if (photoPuzzleView != null && photoPuzzleView.IsOpen) return photoPuzzleView.Close();
            if (recordView != null && recordView.IsOpen) return recordView.Close();
            if (dialogueView != null && dialogueView.HandleEscape()) return true;
            if (stage6UI == null) return false;

            if (stage6UI.StateMachine.Current == UIState.Gameplay) stage6UI.Pause();
            else if (stage6UI.StateMachine.Current == UIState.Paused) stage6UI.Resume();
            else if (stage6UI.StateMachine.Current == UIState.Settings ||
                     stage6UI.StateMachine.Current == UIState.Inspection) stage6UI.CloseOverlay();
            return true;
        }

        private void OnInteractionCompleted(InteractableBase current)
        {
            if (current is PhotoPuzzleInteractable) return;
            if (current is InspectableInteractable inspectable)
            {
                inspectionView?.Show(current.name, inspectable.Description);
                stage6UI?.ShowInspection();
                AddDemoRecord(current.name, inspectable.Description);
            }
            else
            {
                AddDemoRecord(current.name, string.Empty);
                notifications?.Enqueue("상호작용을 완료했습니다.");
            }
        }

        private void SubscribeInteractions()
        {
            UnsubscribeInteractions();
            trackedInteractables = FindObjectsByType<InteractableBase>(FindObjectsSortMode.None);
            for (int i = 0; i < trackedInteractables.Length; i++)
                trackedInteractables[i].InteractionCompleted += OnInteractionCompleted;
        }

        private void UnsubscribeInteractions()
        {
            for (int i = 0; i < trackedInteractables.Length; i++)
                if (trackedInteractables[i] != null)
                    trackedInteractables[i].InteractionCompleted -= OnInteractionCompleted;
            trackedInteractables = System.Array.Empty<InteractableBase>();
        }

        private void OnUIStateChanged(UIState previous, UIState current)
        {
            bool gameplay = current == UIState.Gameplay &&
                            (dialogueView == null || !dialogueView.IsDialogueActive);
            if (player != null) player.enabled = gameplay;
            if (gameplay) DialogueCursorMode.RequestGameplay();
            else DialogueCursorMode.RequestUi();
        }

        private void CloseInspection()
        {
            stage6UI?.CloseOverlay();
        }

        private void AddDemoRecord(string objectName, string description)
        {
            if (recordView == null) return;
            InventoryRecord record = null;
            switch (objectName)
            {
                case "Old Telephone":
                    record = new InventoryRecord("reality.old_phone", RecordKind.KeyItem, "낡은 전화기",
                        "수화기 너머에서 희미한 숨소리가 들린다.", 10);
                    break;
                case "Midnight Clock":
                    record = new InventoryRecord("memory.midnight", RecordKind.MemoryFragment, "멈춘 자정",
                        description, 20);
                    break;
                case "Empty Frame":
                    record = new InventoryRecord("photo.empty_frame", RecordKind.Photo, "비어 있는 액자",
                        description, 30);
                    break;
                case "Model House Table":
                    record = new InventoryRecord("name.small_house", RecordKind.NameFragment, "작은 집",
                        description, 40);
                    break;
                case "Monitor Screen":
                    record = new InventoryRecord("investigation.monitor", RecordKind.Investigation, "켜진 모니터",
                        "모니터에는 낯선 게임이 실행 중이다.", 50);
                    break;
            }
            if (record != null) recordView.AddRecord(record);
        }

        private void OnNewRecordAdded(InventoryRecord record)
        {
            notifications?.Enqueue("새 기록: " + record.Title);
        }

        private void RestoreGameplay()
        {
            if (player != null && (dialogueView == null || !dialogueView.IsDialogueActive))
                player.enabled = true;
        }

        private async void ReturnToTitle()
        {
            RestoreRuntimeState();
            loadingView?.Show("메인 메뉴를 불러오는 중...");
            if (SceneTransitionService.Instance != null)
                await SceneTransitionService.Instance.LoadSceneAsync(SceneId.MainMenu);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("[SmallWorld] Quit requested. Application.Quit is ignored in the Editor.");
#else
            Application.Quit();
#endif
        }

        private void RestoreRuntimeState()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
