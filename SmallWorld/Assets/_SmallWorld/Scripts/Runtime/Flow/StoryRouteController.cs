using System;
using UnityEngine;

namespace SmallWorld.Flow
{
    public enum StoryRouteStep { Dialogue, Puzzle, Memory }

    public interface IStoryRouteProgressSource
    {
        bool IsNodeUnlocked(string nodeId);
        bool IsFinalGateUnlocked { get; }
        void ReportNodeReached(string nodeId);
        void ReportStep(string nodeId, StoryRouteStep step);
    }

    [Serializable]
    public sealed class StoryRouteNode
    {
        public string Id;
        public string DisplayName;
        public Transform Arrival;
        public Transform DialogueEntry;
        public Transform PuzzleEntry;
        public Transform MemoryEntry;
    }

    public sealed class StoryRouteController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private StoryRouteNode[] nodes = Array.Empty<StoryRouteNode>();
        [SerializeField] private int fallbackUnlockedIndex;

        private IStoryRouteProgressSource progressSource;

        public int NodeCount => nodes?.Length ?? 0;
        public int FallbackUnlockedIndex => fallbackUnlockedIndex;
        public bool IsFinalGateUnlocked => progressSource?.IsFinalGateUnlocked ?? false;

        public void Configure(Transform playerTransform, StoryRouteNode[] routeNodes)
        {
            player = playerTransform;
            nodes = routeNodes ?? Array.Empty<StoryRouteNode>();
        }

        public void BindProgressSource(IStoryRouteProgressSource source) => progressSource = source;

        public void ReportStep(string nodeId, StoryRouteStep step) => progressSource?.ReportStep(nodeId, step);

        public bool TryTravelTo(int index, out string feedback)
        {
            if (nodes == null || index < 0 || index >= nodes.Length || nodes[index]?.Arrival == null)
            {
                feedback = "The story route node is not configured.";
                return false;
            }

            StoryRouteNode node = nodes[index];
            bool unlocked = progressSource != null
                ? progressSource.IsNodeUnlocked(node.Id)
                : index <= fallbackUnlockedIndex;
            if (!unlocked)
            {
                feedback = $"{node.DisplayName} is still sealed.";
                return false;
            }

            if (player == null)
            {
                feedback = "The route player is unavailable.";
                return false;
            }

            CharacterController character = player.GetComponent<CharacterController>();
            if (character != null) character.enabled = false;
            player.SetPositionAndRotation(node.Arrival.position, node.Arrival.rotation);
            if (character != null) character.enabled = true;
            progressSource?.ReportNodeReached(node.Id);
            fallbackUnlockedIndex = Mathf.Max(fallbackUnlockedIndex, Mathf.Min(index + 1, nodes.Length - 1));
            feedback = $"Entered {node.DisplayName}.";
            return true;
        }
    }
}
