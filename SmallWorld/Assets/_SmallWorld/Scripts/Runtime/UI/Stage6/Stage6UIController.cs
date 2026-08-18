using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    public sealed class Stage6UIController : MonoBehaviour
    {
        [Serializable]
        private struct PanelBinding
        {
            public UIState state;
            public CanvasGroup panel;
        }

        [SerializeField] private PanelBinding[] panels = Array.Empty<PanelBinding>();
        [Header("Title")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private bool controlTimeScale = true;

        private float timeScaleBeforePause = 1f;

        public UIStateMachine StateMachine { get; } = new UIStateMachine();
        public bool CanContinue { get; private set; }

        public event Action NewGameRequested;
        public event Action ContinueRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;
        public event Action ResumeRequested;
        public event Action ReturnToTitleRequested;

        public void Configure(
            CanvasGroup title, CanvasGroup settings, CanvasGroup gameplay,
            CanvasGroup inspection, CanvasGroup paused, CanvasGroup loading,
            Button newGame, Button continueGame, Button openSettings, Button quit,
            Button resume, Button openPauseSettings, Button returnToTitle,
            bool manageTimeScale = true)
        {
            UnbindButtons();
            panels = new[]
            {
                Bind(UIState.Title, title), Bind(UIState.Settings, settings),
                Bind(UIState.Gameplay, gameplay), Bind(UIState.Inspection, inspection),
                Bind(UIState.Paused, paused), Bind(UIState.Loading, loading)
            };
            newGameButton = newGame;
            continueButton = continueGame;
            settingsButton = openSettings;
            quitButton = quit;
            resumeButton = resume;
            pauseSettingsButton = openPauseSettings;
            returnToTitleButton = returnToTitle;
            controlTimeScale = manageTimeScale;
            ApplyTheme();
            BindButtons();
            SetCanContinue(false);
            RefreshPanels(StateMachine.Current);
        }

        private void Awake()
        {
            ApplyTheme();
            StateMachine.Changed += OnStateChanged;
            BindButtons();
            RefreshPanels(StateMachine.Current);
            SetCanContinue(false);
        }

        private void ApplyTheme()
        {
            foreach (PanelBinding binding in panels)
                SmallWorldUiTheme.ApplyPanel(binding.panel, binding.state != UIState.Gameplay);
            SmallWorldUiTheme.ApplyButton(newGameButton, "새 게임");
            SmallWorldUiTheme.ApplyButton(continueButton, "이어하기");
            SmallWorldUiTheme.ApplyButton(settingsButton, "설정");
            SmallWorldUiTheme.ApplyButton(quitButton, "종료");
            SmallWorldUiTheme.ApplyButton(resumeButton, "계속하기");
            SmallWorldUiTheme.ApplyButton(pauseSettingsButton, "설정");
            SmallWorldUiTheme.ApplyButton(returnToTitleButton, "타이틀로 돌아가기");
        }

        private void OnDestroy()
        {
            StateMachine.Changed -= OnStateChanged;
            UnbindButtons();
            RestoreTimeScale();
        }

        public void ConfigureInitialState(UIState initialState)
        {
            StateMachine.Set(initialState);
            RefreshPanels(initialState);
        }

        public void SetCanContinue(bool canContinue)
        {
            CanContinue = canContinue;
            if (continueButton != null) continueButton.interactable = canContinue;
        }

        public void ShowTitle() => StateMachine.Set(UIState.Title);
        public void ShowGameplay() => StateMachine.Set(UIState.Gameplay);
        public void ShowLoading() => StateMachine.Set(UIState.Loading);
        public void ShowInspection() => StateMachine.Set(UIState.Inspection);
        public void ShowSettings()
        {
            StateMachine.Set(UIState.Settings);
            SettingsRequested?.Invoke();
        }

        public void Pause()
        {
            if (StateMachine.Current == UIState.Paused) return;
            if (controlTimeScale)
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }
            StateMachine.Set(UIState.Paused);
        }

        public void Resume()
        {
            if (StateMachine.Current != UIState.Paused && StateMachine.Current != UIState.Settings) return;
            RestoreTimeScale();
            StateMachine.Set(UIState.Gameplay);
            ResumeRequested?.Invoke();
        }

        public void CloseOverlay()
        {
            if (StateMachine.Current == UIState.Inspection) StateMachine.Set(UIState.Gameplay);
            else if (StateMachine.Current == UIState.Settings) StateMachine.ReturnToPrevious();
        }

        private void OnStateChanged(UIState previous, UIState current)
        {
            RefreshPanels(current);
        }

        private void RefreshPanels(UIState state)
        {
            foreach (PanelBinding binding in panels)
            {
                if (binding.panel == null) continue;
                bool visible = binding.state == state;
                binding.panel.alpha = visible ? 1f : 0f;
                binding.panel.interactable = visible;
                binding.panel.blocksRaycasts = visible;
            }
        }

        private void BindButtons()
        {
            newGameButton?.onClick.AddListener(OnNewGame);
            continueButton?.onClick.AddListener(OnContinue);
            settingsButton?.onClick.AddListener(ShowSettings);
            quitButton?.onClick.AddListener(OnQuit);
            resumeButton?.onClick.AddListener(Resume);
            pauseSettingsButton?.onClick.AddListener(ShowSettings);
            returnToTitleButton?.onClick.AddListener(OnReturnToTitle);
        }

        private void UnbindButtons()
        {
            newGameButton?.onClick.RemoveListener(OnNewGame);
            continueButton?.onClick.RemoveListener(OnContinue);
            settingsButton?.onClick.RemoveListener(ShowSettings);
            quitButton?.onClick.RemoveListener(OnQuit);
            resumeButton?.onClick.RemoveListener(Resume);
            pauseSettingsButton?.onClick.RemoveListener(ShowSettings);
            returnToTitleButton?.onClick.RemoveListener(OnReturnToTitle);
        }

        private void OnNewGame() => NewGameRequested?.Invoke();
        private void OnContinue() { if (CanContinue) ContinueRequested?.Invoke(); }
        private void OnQuit() => QuitRequested?.Invoke();
        private void OnReturnToTitle()
        {
            RestoreTimeScale();
            ReturnToTitleRequested?.Invoke();
        }

        private void RestoreTimeScale()
        {
            if (controlTimeScale && Time.timeScale == 0f)
                Time.timeScale = Mathf.Max(0.0001f, timeScaleBeforePause);
        }

        private static PanelBinding Bind(UIState state, CanvasGroup panel)
        {
            return new PanelBinding { state = state, panel = panel };
        }
    }
}
