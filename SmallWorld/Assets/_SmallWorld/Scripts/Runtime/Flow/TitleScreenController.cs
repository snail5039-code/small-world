using SmallWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.Flow
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;

        public void Configure(Button newGame, Button quit)
        {
            newGameButton = newGame;
            quitButton = quit;
        }

        private void Awake()
        {
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
            newGameButton?.onClick.RemoveListener(StartNewGame);
            quitButton?.onClick.RemoveListener(QuitGame);
        }

        public async void StartNewGame()
        {
            if (SceneTransitionService.Instance == null)
            {
                Debug.LogError("[SmallWorld] Cannot start: SceneTransitionService is missing.", this);
                return;
            }

            await SceneTransitionService.Instance.LoadSceneAsync(SceneId.RealityRoom);
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
