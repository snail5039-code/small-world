using SmallWorld.Player;
using UnityEngine;

namespace SmallWorld.Flow
{
    public sealed class StoryRouteInteractable : InteractableBase
    {
        [SerializeField] private StoryRouteController route;
        [SerializeField] private int targetNodeIndex;
        [SerializeField] private string markerFeedback;
        [SerializeField] private bool finalGate;
        [SerializeField] private string nodeId;
        [SerializeField] private StoryRouteStep step;

        public void ConfigureTravel(StoryRouteController controller, int index, string prompt)
        {
            route = controller;
            targetNodeIndex = index;
            Configure(prompt, GetComponentsInChildren<Renderer>(true));
        }

        public void ConfigureMarker(StoryRouteController controller, string storyNodeId,
            StoryRouteStep routeStep, string prompt, string feedback)
        {
            route = controller;
            nodeId = storyNodeId;
            step = routeStep;
            markerFeedback = feedback;
            Configure(prompt, GetComponentsInChildren<Renderer>(true));
        }

        public void ConfigureFinalGate(StoryRouteController controller, string prompt)
        {
            route = controller;
            finalGate = true;
            Configure(prompt, GetComponentsInChildren<Renderer>(true));
        }

        protected override void BeginInteraction(InteractionContext context)
        {
            if (finalGate)
                context.ShowFeedback(route != null && route.IsFinalGateUnlocked
                    ? "Final chapter entry contract satisfied. Awaiting final-chapter scene integration."
                    : "The final chapter remains sealed until chapters 1-6 are complete.");
            else
            {
                if (route != null && !string.IsNullOrEmpty(nodeId)) route.ReportStep(nodeId, step);
                context.ShowFeedback(markerFeedback);
            }

            if (!finalGate && route != null && string.IsNullOrEmpty(markerFeedback))
            {
                route.TryTravelTo(targetNodeIndex, out string travelFeedback);
                context.ShowFeedback(travelFeedback);
            }
            CompleteInteraction();
        }
    }
}
