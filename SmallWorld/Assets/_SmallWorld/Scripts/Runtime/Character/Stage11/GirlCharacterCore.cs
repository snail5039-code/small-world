using System;

namespace SmallWorld.Character.Stage11
{
    public enum GirlMood
    {
        Guarded,
        Calm,
        Warm,
        Hurt
    }

    public enum GirlBehavior
    {
        KeepDistance,
        Observe,
        Approach,
        ShareMemory,
        Withdraw
    }

    public enum PlayerAction
    {
        None,
        Greet,
        Listen,
        Help,
        Ignore,
        BreakPromise
    }

    public enum DeathMemoryHandling
    {
        None,
        Comforted,
        GivenSpace,
        Dismissed
    }

    public sealed class GirlCharacterState
    {
        public const int MinimumRelationship = -100;
        public const int MaximumRelationship = 100;

        public GirlCharacterState(string characterId, int relationship = 0)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                throw new ArgumentException("Value cannot be empty.", nameof(characterId));
            CharacterId = characterId;
            Relationship = ClampRelationship(relationship);
            Mood = ResolveMood(Relationship, PlayerAction.None);
        }

        public string CharacterId { get; }
        public int Relationship { get; private set; }
        public GirlMood Mood { get; private set; }
        public PlayerAction LastPlayerAction { get; private set; }
        public int InteractionCount { get; private set; }
        public bool SharedPrivateMemory { get; private set; }
        public int DeathCount { get; private set; }
        public DeathMemoryHandling LastDeathHandling { get; private set; }
        public int ReactedDeathCount { get; private set; }
        public bool HasPendingDeathReaction => DeathCount > ReactedDeathCount;

        public GirlBehavior React(PlayerAction action)
        {
            LastPlayerAction = action;
            InteractionCount++;
            Relationship = ClampRelationship(Relationship + RelationshipDelta(action));
            Mood = ResolveMood(Relationship, action);

            GirlBehavior behavior = GirlBehaviorPolicy.Select(Relationship, action, SharedPrivateMemory);
            if (behavior == GirlBehavior.ShareMemory) SharedPrivateMemory = true;
            return behavior;
        }

        public void Restore(int relationship, GirlMood mood, PlayerAction lastPlayerAction,
            int interactionCount, bool sharedPrivateMemory)
        {
            Restore(relationship, mood, lastPlayerAction, interactionCount, sharedPrivateMemory,
                0, DeathMemoryHandling.None, 0);
        }

        public void Restore(int relationship, GirlMood mood, PlayerAction lastPlayerAction,
            int interactionCount, bool sharedPrivateMemory, int deathCount,
            DeathMemoryHandling lastDeathHandling, int reactedDeathCount)
        {
            if (interactionCount < 0) throw new ArgumentOutOfRangeException(nameof(interactionCount));
            if (deathCount < 0) throw new ArgumentOutOfRangeException(nameof(deathCount));
            if (reactedDeathCount < 0 || reactedDeathCount > deathCount)
                throw new ArgumentOutOfRangeException(nameof(reactedDeathCount));
            Relationship = ClampRelationship(relationship);
            Mood = mood;
            LastPlayerAction = lastPlayerAction;
            InteractionCount = interactionCount;
            SharedPrivateMemory = sharedPrivateMemory;
            DeathCount = deathCount;
            LastDeathHandling = lastDeathHandling;
            ReactedDeathCount = reactedDeathCount;
        }

        public void RememberDeath(DeathMemoryHandling handling)
        {
            DeathCount++;
            LastDeathHandling = handling;
        }

        public bool ConsumeDeathReaction()
        {
            if (!HasPendingDeathReaction) return false;
            ReactedDeathCount = DeathCount;
            return true;
        }

        private static int RelationshipDelta(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Greet: return 2;
                case PlayerAction.Listen: return 5;
                case PlayerAction.Help: return 10;
                case PlayerAction.Ignore: return -5;
                case PlayerAction.BreakPromise: return -20;
                default: return 0;
            }
        }

        private static GirlMood ResolveMood(int relationship, PlayerAction action)
        {
            if (action == PlayerAction.BreakPromise) return GirlMood.Hurt;
            if (relationship >= 50) return GirlMood.Warm;
            if (relationship >= 10) return GirlMood.Calm;
            return GirlMood.Guarded;
        }

        private static int ClampRelationship(int value) =>
            Math.Max(MinimumRelationship, Math.Min(MaximumRelationship, value));
    }

    public static class GirlBehaviorPolicy
    {
        public static GirlBehavior Select(int relationship, PlayerAction action, bool sharedPrivateMemory)
        {
            if (action == PlayerAction.BreakPromise || relationship <= -20) return GirlBehavior.Withdraw;
            if (relationship < 10) return GirlBehavior.KeepDistance;
            if (relationship < 35) return GirlBehavior.Observe;
            if (relationship >= 70 && !sharedPrivateMemory &&
                (action == PlayerAction.Listen || action == PlayerAction.Help)) return GirlBehavior.ShareMemory;
            return GirlBehavior.Approach;
        }
    }
}
