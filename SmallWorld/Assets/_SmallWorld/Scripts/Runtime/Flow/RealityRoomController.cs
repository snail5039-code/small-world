using SmallWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    public sealed class RealityRoomController : MonoBehaviour
    {
        private async void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (SceneTransitionService.Instance == null)
            {
                Debug.LogError("[SmallWorld] Cannot return: SceneTransitionService is missing.", this);
                return;
            }

            await SceneTransitionService.Instance.LoadSceneAsync(SceneId.MainMenu);
        }
    }
}
