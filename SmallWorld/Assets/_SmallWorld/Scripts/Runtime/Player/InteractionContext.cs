using UnityEngine;

namespace SmallWorld.Player
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject interactor, PlayerInteractionDetector detector)
        {
            Interactor = interactor;
            Detector = detector;
        }

        public GameObject Interactor { get; }
        public PlayerInteractionDetector Detector { get; }

        public void ShowFeedback(string message)
        {
            Detector?.ShowFeedback(message);
        }
    }
}
