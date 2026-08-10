using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Save.Stage10.Integration
{
    public sealed class Stage10AutoSaveTrigger : MonoBehaviour
    {
        [SerializeField] private RealityRoomSaveCoordinator coordinator;
        [SerializeField] private string checkpointId = "reality.entry";
        private bool triggered;
        public void Configure(RealityRoomSaveCoordinator value, string id) { coordinator = value; checkpointId = id; }
        private void OnTriggerEnter(Collider other)
        {
            if (triggered || coordinator == null || other.GetComponent<FirstPersonPlayerController>() == null) return;
            triggered = coordinator.AutoSave(checkpointId);
        }
    }
}
