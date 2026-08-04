using SmallWorld.Services;
using UnityEngine;

namespace SmallWorld.UI
{
    public sealed class Stage6SettingsBinding : MonoBehaviour
    {
        [SerializeField] private Stage6UIController controller;
        [SerializeField] private SettingsPanelView view;

        public void Configure(Stage6UIController uiController, SettingsPanelView settingsView)
        {
            Unsubscribe();
            controller = uiController;
            view = settingsView;
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        public static UISettingsSnapshot ToSnapshot(SettingsData data)
        {
            data = (data ?? SettingsData.CreateDefault()).ValidatedCopy();
            return new UISettingsSnapshot
            {
                master = data.masterVolume,
                music = data.musicVolume,
                sfx = data.sfxVolume,
                voice = data.voiceVolume,
                fullscreen = data.fullscreen,
                width = data.width,
                height = data.height
            };
        }

        public static SettingsData ToData(UISettingsSnapshot value)
        {
            value = value.Validated();
            return new SettingsData
            {
                schemaVersion = SettingsData.CurrentSchemaVersion,
                masterVolume = value.master,
                musicVolume = value.music,
                sfxVolume = value.sfx,
                voiceVolume = value.voice,
                fullscreen = value.fullscreen,
                width = value.width,
                height = value.height
            }.ValidatedCopy();
        }

        private void PresentCurrent()
        {
            view?.Present(ToSnapshot(SettingsService.Instance != null
                ? SettingsService.Instance.Current
                : SettingsData.CreateDefault()));
        }

        private void Apply(UISettingsSnapshot snapshot)
        {
            SettingsData data = ToData(snapshot);
            SettingsService.Instance?.Apply(data);
            Screen.SetResolution(data.width, data.height,
                data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            controller?.CloseOverlay();
        }

        private void Cancel()
        {
            PresentCurrent();
            controller?.CloseOverlay();
        }

        private void Subscribe()
        {
            if (controller != null)
            {
                controller.SettingsRequested -= PresentCurrent;
                controller.SettingsRequested += PresentCurrent;
            }
            if (view != null)
            {
                view.ApplyRequested -= Apply;
                view.ApplyRequested += Apply;
                view.CancelRequested -= Cancel;
                view.CancelRequested += Cancel;
            }
        }

        private void Unsubscribe()
        {
            if (controller != null) controller.SettingsRequested -= PresentCurrent;
            if (view != null)
            {
                view.ApplyRequested -= Apply;
                view.CancelRequested -= Cancel;
            }
        }
    }
}
