using System;
using UnityEngine;

namespace SmallWorld.Services
{
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentSchemaVersion = 1;
        public const int DefaultWidth = 1920;
        public const int DefaultHeight = 1080;

        public int schemaVersion = CurrentSchemaVersion;
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public float voiceVolume = 1f;
        public bool fullscreen = true;
        public int width = DefaultWidth;
        public int height = DefaultHeight;

        public static SettingsData CreateDefault()
        {
            return new SettingsData();
        }

        public SettingsData ValidatedCopy()
        {
            if (schemaVersion != CurrentSchemaVersion)
            {
                return CreateDefault();
            }

            return new SettingsData
            {
                schemaVersion = CurrentSchemaVersion,
                masterVolume = SanitizeVolume(masterVolume),
                musicVolume = SanitizeVolume(musicVolume),
                sfxVolume = SanitizeVolume(sfxVolume),
                voiceVolume = SanitizeVolume(voiceVolume),
                fullscreen = fullscreen,
                width = Mathf.Clamp(width, 640, 7680),
                height = Mathf.Clamp(height, 360, 4320)
            };
        }

        public static float SanitizeVolume(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? 1f
                : Mathf.Clamp01(value);
        }
    }
}

