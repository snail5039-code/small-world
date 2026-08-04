using System;
using UnityEngine;

namespace SmallWorld.Core
{
    /// <summary>
    /// Persistent owner of the application's high-level state. Consumers observe state
    /// through <see cref="StateChanged"/> instead of coupling themselves to scene objects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameStateService : MonoBehaviour
    {
        [SerializeField] private GameState currentState = GameState.Boot;

        public static GameStateService Instance { get; private set; }

        public GameState CurrentState => currentState;

        public event EventHandler<GameStateChangedEventArgs> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SafeGameLogger.Warning("A duplicate GameStateService was discarded.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Changes state once and notifies all listeners. Repeating a state is a no-op.</summary>
        public bool TryChangeState(GameState nextState)
        {
            if (currentState == nextState)
            {
                return false;
            }

            GameState previousState = currentState;
            currentState = nextState;
            var args = new GameStateChangedEventArgs(previousState, nextState);

            Delegate[] listeners = StateChanged?.GetInvocationList();
            if (listeners != null)
            {
                foreach (Delegate listener in listeners)
                {
                    try
                    {
                        ((EventHandler<GameStateChangedEventArgs>)listener).Invoke(this, args);
                    }
                    catch (Exception exception)
                    {
                        SafeGameLogger.Error("A game-state listener failed.", exception);
                    }
                }
            }

            GameEventBus.Publish(args);
            return true;
        }
    }
}
