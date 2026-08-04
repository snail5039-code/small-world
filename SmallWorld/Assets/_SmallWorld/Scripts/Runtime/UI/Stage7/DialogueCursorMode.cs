using UnityEngine;

namespace SmallWorld.UI.Stage7
{
    public static class DialogueCursorMode
    {
        public static CursorLockMode RequestedLockState { get; private set; } = CursorLockMode.None;
        public static bool RequestedVisible { get; private set; } = true;

        public static void RequestUi()
        {
            Apply(CursorLockMode.None, true);
        }

        public static void RequestGameplay()
        {
            Apply(CursorLockMode.Locked, false);
        }

        private static void Apply(CursorLockMode lockState, bool visible)
        {
            RequestedLockState = lockState;
            RequestedVisible = visible;
            Cursor.lockState = lockState;
            Cursor.visible = visible;
        }
    }
}
