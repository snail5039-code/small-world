using System;
using System.Threading.Tasks;
using SmallWorld.Core;
using SmallWorld.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Flow
{
    public sealed class SceneTransitionService : MonoBehaviour
    {
        private const float MinimumLoadingSeconds = 0.35f;

        public static SceneTransitionService Instance { get; private set; }
        public bool IsTransitioning { get; private set; }

        private LoadingScreenView loadingScreen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(LoadingScreenView view)
        {
            loadingScreen = view;
        }

        public async Task LoadSceneAsync(SceneId sceneId)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"[SmallWorld] Ignored duplicate scene transition to {sceneId}.", this);
                return;
            }

            string sceneName = SceneCatalog.GetName(sceneId);
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"[SmallWorld] Scene {sceneId} ({sceneName}) is not available in Build Settings.",
                    this);
                return;
            }

            IsTransitioning = true;
            float startedAt = Time.realtimeSinceStartup;
            GameState previousState = GameStateService.Instance != null
                ? GameStateService.Instance.CurrentState
                : GameState.Boot;
            GameStateService.Instance?.TryChangeState(GameState.Loading);
            loadingScreen?.Show();

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    throw new InvalidOperationException($"Unity could not start loading scene '{sceneName}'.");
                }

                operation.allowSceneActivation = false;
                while (operation.progress < 0.9f)
                {
                    loadingScreen?.SetProgress(operation.progress / 0.9f);
                    await Task.Yield();
                }

                while (Time.realtimeSinceStartup - startedAt < MinimumLoadingSeconds)
                {
                    await Task.Yield();
                }

                loadingScreen?.SetProgress(1f);
                operation.allowSceneActivation = true;
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                GameStateService.Instance?.TryChangeState(GetGameState(sceneId));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                GameStateService.Instance?.TryChangeState(previousState);
            }
            finally
            {
                loadingScreen?.HideImmediate();
                IsTransitioning = false;
            }
        }

        private static GameState GetGameState(SceneId sceneId)
        {
            switch (sceneId)
            {
                case SceneId.Boot:
                    return GameState.Boot;
                case SceneId.MainMenu:
                    return GameState.MainMenu;
                case SceneId.RealityRoom:
                    return GameState.Playing;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown scene id.");
            }
        }
    }
}
