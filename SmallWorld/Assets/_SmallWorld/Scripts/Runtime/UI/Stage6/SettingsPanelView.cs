using System;
using UnityEngine;
using UnityEngine.UI;

namespace SmallWorld.UI
{
    [Serializable]
    public struct UISettingsSnapshot
    {
        public float master;
        public float music;
        public float sfx;
        public float voice;
        public bool fullscreen;
        public int width;
        public int height;

        public UISettingsSnapshot Validated()
        {
            UISettingsSnapshot copy = this;
            copy.master = Sanitize(copy.master);
            copy.music = Sanitize(copy.music);
            copy.sfx = Sanitize(copy.sfx);
            copy.voice = Sanitize(copy.voice);
            copy.width = Mathf.Clamp(copy.width, 640, 7680);
            copy.height = Mathf.Clamp(copy.height, 360, 4320);
            return copy;
        }

        private static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
        }
    }

    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Slider master;
        [SerializeField] private Slider music;
        [SerializeField] private Slider sfx;
        [SerializeField] private Slider voice;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private InputField width;
        [SerializeField] private InputField height;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;

        public event Action<UISettingsSnapshot> ApplyRequested;
        public event Action CancelRequested;

        public void Configure(Slider masterSlider, Slider musicSlider, Slider sfxSlider, Slider voiceSlider,
            Toggle fullscreenToggle, InputField widthField, InputField heightField,
            Button apply, Button cancel)
        {
            applyButton?.onClick.RemoveListener(Submit);
            cancelButton?.onClick.RemoveListener(Cancel);
            master = masterSlider;
            music = musicSlider;
            sfx = sfxSlider;
            voice = voiceSlider;
            fullscreen = fullscreenToggle;
            width = widthField;
            height = heightField;
            applyButton = apply;
            cancelButton = cancel;
            applyButton?.onClick.AddListener(Submit);
            cancelButton?.onClick.AddListener(Cancel);
        }

        private void Awake()
        {
            applyButton?.onClick.AddListener(Submit);
            cancelButton?.onClick.AddListener(Cancel);
        }

        private void OnDestroy()
        {
            applyButton?.onClick.RemoveListener(Submit);
            cancelButton?.onClick.RemoveListener(Cancel);
        }

        public void Present(UISettingsSnapshot value)
        {
            value = value.Validated();
            if (master != null) master.value = value.master;
            if (music != null) music.value = value.music;
            if (sfx != null) sfx.value = value.sfx;
            if (voice != null) voice.value = value.voice;
            if (fullscreen != null) fullscreen.isOn = value.fullscreen;
            if (width != null) width.text = value.width.ToString();
            if (height != null) height.text = value.height.ToString();
        }

        public UISettingsSnapshot Read()
        {
            return new UISettingsSnapshot
            {
                master = master != null ? master.value : 1f,
                music = music != null ? music.value : 1f,
                sfx = sfx != null ? sfx.value : 1f,
                voice = voice != null ? voice.value : 1f,
                fullscreen = fullscreen == null || fullscreen.isOn,
                width = Parse(width, 1920),
                height = Parse(height, 1080)
            }.Validated();
        }

        private void Submit() => ApplyRequested?.Invoke(Read());
        private void Cancel() => CancelRequested?.Invoke();

        private static int Parse(InputField field, int fallback)
        {
            return field != null && int.TryParse(field.text, out int value) ? value : fallback;
        }
    }
}
