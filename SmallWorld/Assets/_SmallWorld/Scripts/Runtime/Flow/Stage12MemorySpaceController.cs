using System.Threading.Tasks;
using SmallWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SmallWorld.Flow
{
    /// <summary>Stage 12 first memory space: safe zone and explicit return to the white room.</summary>
    public sealed class Stage12MemorySpaceController : MonoBehaviour
    {
        [SerializeField] private Transform safeZone;
        [SerializeField] private string progressKey = "stage12.memory.first.entered";
        public bool HasEntered { get; private set; }

        private void Awake() => HasEntered = PlayerPrefs.GetInt(progressKey, 0) != 0;

        private void Start()
        {
            HasEntered = true;
            PlayerPrefs.SetInt(progressKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[Stage12] First memory space entered; safe zone active.", this);
        }

        private async void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                await ReturnToWhiteRoom();
        }

        public bool IsInSafeZone(Vector3 position) => safeZone == null ||
            Vector3.Distance(position, safeZone.position) <= 2.5f;

        public async Task ReturnToWhiteRoom()
        {
            if (SceneTransitionService.Instance != null)
                await SceneTransitionService.Instance.LoadSceneAsync(SceneId.RealityRoom);
        }
    }
}
