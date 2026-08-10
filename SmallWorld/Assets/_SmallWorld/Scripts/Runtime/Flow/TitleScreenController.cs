using System;
using System.Threading.Tasks;
using SmallWorld.Core;
using SmallWorld.Puzzle.Stage9.Persistence;
using SmallWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Flow
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Stage6UIController stage6UI;
        private IPhotoPuzzleStorage newGameStorage;
        private Func<string, Task> newGameSceneLoader;

        public void Configure(Button newGame, Button quit)
        {
            newGameButton = newGame;
            quitButton = quit;
        }

        public void ConfigureStage6(Stage6UIController controller)
        {
            stage6UI = controller;
        }

        public void ConfigureNewGamePersistence(IPhotoPuzzleStorage storage)
        {
            newGameStorage = storage;
        }

        public void ConfigureNewGameSceneLoader(Func<string, Task> loader)
        {
            newGameSceneLoader = loader;
        }

        private void Awake()
        {
            if (stage6UI != null)
            {
                stage6UI.NewGameRequested += StartNewGame;
                stage6UI.ContinueRequested += ContinueGame;
                stage6UI.QuitRequested += QuitGame;
                stage6UI.ConfigureInitialState(UIState.Title);
                stage6UI.SetCanContinue(false);
                return;
            }
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(StartNewGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void OnDestroy()
        {
            if (stage6UI != null)
            {
                stage6UI.NewGameRequested -= StartNewGame;
                stage6UI.ContinueRequested -= ContinueGame;
                stage6UI.QuitRequested -= QuitGame;
            }
            newGameButton?.onClick.RemoveListener(StartNewGame);
            quitButton?.onClick.RemoveListener(QuitGame);
        }

        public async void StartNewGame()
        {
            PhotoPuzzleSaveContract.Clear(newGameStorage ?? new PlayerPrefsPhotoPuzzleStorage());
            if (newGameSceneLoader != null)
            {
                await newGameSceneLoader(SceneId.RealityRoom.ToString());
                return;
            }
            if (SceneTransitionService.Instance == null)
            {
                Debug.LogError("[SmallWorld] Cannot start: SceneTransitionService is missing.", this);
                return;
            }

            await SceneTransitionService.Instance.LoadSceneAsync(SceneId.RealityRoom);
        }

        private void ContinueGame()
        {
            // Stage 6 has no save-game service yet. The button remains disabled until one is supplied.
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("[SmallWorld] Quit requested. Application.Quit is ignored in the Editor.", this);
#else
            Application.Quit();
#endif
        }
    }
}
