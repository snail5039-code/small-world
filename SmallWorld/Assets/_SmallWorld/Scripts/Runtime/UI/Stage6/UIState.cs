using System;

namespace SmallWorld.UI
{
    public enum UIState
    {
        Title,
        Settings,
        Gameplay,
        Inspection,
        Paused,
        Loading
    }

    public sealed class UIStateMachine
    {
        public UIState Current { get; private set; } = UIState.Title;
        public UIState Previous { get; private set; } = UIState.Title;

        public event Action<UIState, UIState> Changed;

        public bool Set(UIState next)
        {
            if (next == Current) return false;
            UIState old = Current;
            Previous = old;
            Current = next;
            Changed?.Invoke(old, next);
            return true;
        }

        public bool ReturnToPrevious()
        {
            return Set(Previous);
        }
    }
}

