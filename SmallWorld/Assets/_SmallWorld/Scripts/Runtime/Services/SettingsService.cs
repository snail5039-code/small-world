using System;
using System.IO;
using UnityEngine;

namespace SmallWorld.Services
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SettingsService : MonoBehaviour
    {
        private const string FileName = "settings.json";

        public static SettingsService Instance { get; private set; }

        public SettingsData Current { get; private set; } = SettingsData.CreateDefault();

        public event Action<SettingsData> SettingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                new GameObject(nameof(SettingsService)).AddComponent<SettingsService>();
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
            Load();
        }

        public void Load()
        {
            Current = ReadValidatedOrDefault();
            SettingsChanged?.Invoke(Current);
        }

        public void Apply(SettingsData settings, bool save = true)
        {
            Current = (settings ?? SettingsData.CreateDefault()).ValidatedCopy();

            if (save)
            {
                Save();
            }

            SettingsChanged?.Invoke(Current);
        }

        public void ResetToDefaults(bool save = true)
        {
            Apply(SettingsData.CreateDefault(), save);
        }

        public bool Save()
        {
            var temporaryPath = SettingsPath + ".tmp";

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(Current.ValidatedCopy(), true));
                File.Copy(temporaryPath, SettingsPath, true);
                File.Delete(temporaryPath);
                return true;
            }
            catch (Exception exception)
            {
                TryDeleteTemporaryFile(temporaryPath);
                Debug.LogWarning($"[SmallWorld] Settings could not be saved ({exception.GetType().Name}).");
                return false;
            }
        }

        private static SettingsData ReadValidatedOrDefault()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return SettingsData.CreateDefault();
                }

                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonUtility.FromJson<SettingsData>(json);
                return (loaded ?? SettingsData.CreateDefault()).ValidatedCopy();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SmallWorld] Settings were invalid and defaults were restored ({exception.GetType().Name}).");
                return SettingsData.CreateDefault();
            }
        }

        private static string SettingsPath =>
            Path.Combine(Application.persistentDataPath, FileName);

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best effort cleanup; never expose the local path in logs.
            }
        }
    }
}

