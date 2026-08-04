using System;
using UnityEngine;

namespace SmallWorld.Player
{
    [Serializable]
    public sealed class PlayerAccessibilitySettings
    {
        [SerializeField, Range(0.05f, 2f)] private float lookSensitivity = 0.35f;
        [SerializeField, Range(60f, 110f)] private float fieldOfView = 85f;
        [SerializeField] private bool cameraBobEnabled = true;
        [SerializeField] private bool crosshairEnabled = true;
        [SerializeField] private bool fixedComfortDot;

        public float LookSensitivity => Mathf.Clamp(lookSensitivity, 0.05f, 2f);
        public float FieldOfView => Mathf.Clamp(fieldOfView, 60f, 110f);
        public bool CameraBobEnabled => cameraBobEnabled && !fixedComfortDot;
        public bool CrosshairVisible => crosshairEnabled || fixedComfortDot;
        public bool FixedComfortDot => fixedComfortDot;

        public void Configure(float sensitivity, float fov, bool cameraBob, bool crosshair, bool comfortDot)
        {
            lookSensitivity = sensitivity;
            fieldOfView = fov;
            cameraBobEnabled = cameraBob;
            crosshairEnabled = crosshair;
            fixedComfortDot = comfortDot;
        }
    }
}
