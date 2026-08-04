using System;
using System.Threading.Tasks;
using SmallWorld.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmallWorld.Flow
{
    [DisallowMultipleComponent]
    public sealed class Stage2RuntimeSmokeProbe : MonoBehaviour
    {
        public async Task RunAsync(SceneTransitionService transitionService)
        {
            if (transitionService == null)
            {
                Fail("SceneTransitionService is missing.");
                return;
            }

            try
            {
                Debug.Log("[Stage2Smoke] START");

                await LoadAndVerifyAsync(
                    transitionService,
                    SceneId.MainMenu,
                    "Boot -> MainMenu");
                await LoadAndVerifyAsync(
                    transitionService,
                    SceneId.RealityRoom,
                    "MainMenu -> RealityRoom");
                await LoadAndVerifyAsync(
                    transitionService,
                    SceneId.MainMenu,
                    "RealityRoom -> MainMenu");

                Debug.Log("[Stage2Smoke] PASS");
                await Task.Yield();
                Application.Quit(0);
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
            }
        }

        private static async Task LoadAndVerifyAsync(
            SceneTransitionService transitionService,
            SceneId expectedScene,
            string step)
        {
            Debug.Log($"[Stage2Smoke] {step} BEGIN");
            await transitionService.LoadSceneAsync(expectedScene);

            Scene activeScene = SceneManager.GetActiveScene();
            if (!SceneCatalog.TryGetId(activeScene.name, out SceneId actualScene) ||
                actualScene != expectedScene)
            {
                throw new InvalidOperationException(
                    $"{step} failed. Expected {SceneCatalog.GetName(expectedScene)}, " +
                    $"but active scene is {activeScene.name}.");
            }

            GameState expectedState = expectedScene == SceneId.MainMenu
                ? GameState.MainMenu
                : GameState.Playing;
            if (GameStateService.Instance == null ||
                GameStateService.Instance.CurrentState != expectedState)
            {
                throw new InvalidOperationException(
                    $"{step} reached the scene but game state is not {expectedState}.");
            }

            Debug.Log($"[Stage2Smoke] {step} OK");
        }

        private static void Fail(string reason)
        {
            Debug.LogError($"[Stage2Smoke] FAIL: {reason}");
            Application.Quit(1);
        }
    }
}
