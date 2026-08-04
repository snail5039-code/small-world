using SmallWorld.Core;
using SmallWorld.Player;
using SmallWorld.UI;
using SmallWorld.UI.Stage7;
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

        private InteractableBase observedInteractable;
        private int observedInteractionCount;

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

        private void Awake()
        {
            if (dialogueView == null) dialogueView = FindFirstObjectByType<Stage7DialogueView>();
            if (stage6UI == null) return;
            stage6UI.ResumeRequested += RestoreGameplay;
            stage6UI.ReturnToTitleRequested += ReturnToTitle;
            stage6UI.QuitRequested += QuitGame;
            stage6UI.StateMachine.Changed += OnUIStateChanged;
            if (inspectionView != null) inspectionView.CloseRequested += CloseInspection;
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
            RestoreRuntimeState();
        }

        private async void Update()
        {
            ObserveInteraction();
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
            if (dialogueView != null && dialogueView.HandleEscape()) return true;
            if (stage6UI == null) return false;

            if (stage6UI.StateMachine.Current == UIState.Gameplay) stage6UI.Pause();
            else if (stage6UI.StateMachine.Current == UIState.Paused) stage6UI.Resume();
            else if (stage6UI.StateMachine.Current == UIState.Settings ||
                     stage6UI.StateMachine.Current == UIState.Inspection) stage6UI.CloseOverlay();
            return true;
        }

        private void ObserveInteraction()
        {
            InteractableBase current = interactionDetector != null
                ? interactionDetector.CurrentInteractable as InteractableBase
                : null;
            if (current != observedInteractable)
            {
                observedInteractable = current;
                observedInteractionCount = current != null ? current.InteractionCount : 0;
                return;
            }
            if (current == null || current.InteractionCount == observedInteractionCount) return;
            observedInteractionCount = current.InteractionCount;
            if (current is InspectableInteractable inspectable)
            {
                inspectionView?.Show(current.name, inspectable.Description);
                stage6UI?.ShowInspection();
            }
            else notifications?.Enqueue("상호작용을 완료했습니다.");
        }

        private void OnUIStateChanged(UIState previous, UIState current)
        {
            bool gameplay = current == UIState.Gameplay &&
                            (dialogueView == null || !dialogueView.IsDialogueActive);
            if (player != null) player.enabled = gameplay;
        }

        private void CloseInspection()
        {
            stage6UI?.CloseOverlay();
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
