using UnityEngine;
using UnityEngine.Audio;

namespace SmallWorld.Services
{
    [DefaultExecutionOrder(-800)]
    public sealed class AudioService : MonoBehaviour
    {
        public const float MinimumDecibels = -80f;

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterParameter = "MasterVolume";
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SfxVolume";
        [SerializeField] private string voiceParameter = "VoiceVolume";

        public static AudioService Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(nameof(AudioService)).AddComponent<AudioService>();
            }
        }

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

        private void Start()
        {
            if (SettingsService.Instance != null)
            {
                SettingsService.Instance.SettingsChanged += Apply;
                Apply(SettingsService.Instance.Current);
            }
        }

        private void OnDestroy()
        {
            if (SettingsService.Instance != null)
            {
                SettingsService.Instance.SettingsChanged -= Apply;
            }
        }

        public void ConfigureMixer(AudioMixer audioMixer)
        {
            mixer = audioMixer;

            if (SettingsService.Instance != null)
            {
                Apply(SettingsService.Instance.Current);
            }
        }

        public void Apply(SettingsData settings)
        {
            if (mixer == null || settings == null)
            {
                return;
            }

            SetVolume(masterParameter, settings.masterVolume);
            SetVolume(musicParameter, settings.musicVolume);
            SetVolume(sfxParameter, settings.sfxVolume);
            SetVolume(voiceParameter, settings.voiceVolume);
        }

        public static float LinearToDecibels(float linearVolume)
        {
            var sanitized = SettingsData.SanitizeVolume(linearVolume);
            return sanitized <= 0.0001f
                ? MinimumDecibels
                : Mathf.Clamp(20f * Mathf.Log10(sanitized), MinimumDecibels, 0f);
        }

        private void SetVolume(string parameterName, float linearVolume)
        {
            if (!string.IsNullOrWhiteSpace(parameterName))
            {
                mixer.SetFloat(parameterName, LinearToDecibels(linearVolume));
            }
        }
    }
}

