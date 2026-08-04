using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmallWorld.Core
{
    /// <summary>
    /// Small type-keyed event channel for decoupled runtime systems. Subscriber exceptions
    /// are isolated so one faulty observer cannot prevent delivery to the remaining observers.
    /// </summary>
    public static class GameEventBus
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            lock (Gate)
            {
                Subscribers.Clear();
            }
        }

        public static void Subscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (Gate)
            {
                Type eventType = typeof(TEvent);
                Subscribers.TryGetValue(eventType, out Delegate existing);
                Subscribers[eventType] = Delegate.Combine(existing, listener);
            }
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> listener)
        {
            if (listener == null)
            {
                return;
            }

            lock (Gate)
            {
                Type eventType = typeof(TEvent);
                if (!Subscribers.TryGetValue(eventType, out Delegate existing))
                {
                    return;
                }

                Delegate remaining = Delegate.Remove(existing, listener);
                if (remaining == null)
                {
                    Subscribers.Remove(eventType);
                }
                else
                {
                    Subscribers[eventType] = remaining;
                }
            }
        }

        public static void Publish<TEvent>(TEvent gameEvent)
        {
            Delegate snapshot;
            lock (Gate)
            {
                Subscribers.TryGetValue(typeof(TEvent), out snapshot);
            }

            if (snapshot == null)
            {
                return;
            }

            foreach (Delegate listener in snapshot.GetInvocationList())
            {
                try
                {
                    ((Action<TEvent>)listener).Invoke(gameEvent);
                }
                catch (Exception exception)
                {
                    SafeGameLogger.Error("A game-event subscriber failed.", exception);
                }
            }
        }
    }
}
