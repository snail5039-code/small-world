using System;
using System.Linq;
using SmallWorld.Core;
using SmallWorld.UI;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private LoadingScreenView loadingScreen;

        public void Configure(LoadingScreenView view)
        {
            loadingScreen = view;
        }

        private async void Start()
        {
            SceneTransitionService transitionService = SceneTransitionService.Instance;
            GameObject services = transitionService != null
                ? transitionService.gameObject
                : new GameObject("SmallWorld Services");

            if (GameStateService.Instance == null)
            {
                services.AddComponent<GameStateService>();
            }

            if (loadingScreen != null)
            {
                loadingScreen.transform.SetParent(services.transform, false);
            }

            if (transitionService == null)
            {
                transitionService = services.AddComponent<SceneTransitionService>();
            }

            transitionService.Initialize(loadingScreen);

            if (Environment.GetCommandLineArgs().Contains(
                    "-stage2SmokeTest",
                    StringComparer.OrdinalIgnoreCase))
            {
                Stage2RuntimeSmokeProbe probe =
                    services.GetComponent<Stage2RuntimeSmokeProbe>() ??
                    services.AddComponent<Stage2RuntimeSmokeProbe>();
                await probe.RunAsync(transitionService);
                return;
            }

            await transitionService.LoadSceneAsync(SceneId.MainMenu);
        }
    }
}
