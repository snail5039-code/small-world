using System;

namespace SmallWorld.Core
{
    /// <summary>Immutable state transition data published after a successful change.</summary>
    public sealed class GameStateChangedEventArgs : EventArgs
    {
        public GameStateChangedEventArgs(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }

        public GameState PreviousState { get; }
        public GameState CurrentState { get; }
    }
}
