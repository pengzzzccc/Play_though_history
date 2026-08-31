using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownTechnology.Core.Events
{
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> handler);
        void Publish<T>(T message);
        void Clear();
    }

    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> subscribers = new();

        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var messageType = typeof(T);
            if (!subscribers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Delegate>();
                subscribers.Add(messageType, handlers);
            }

            handlers.Add(handler);
            return new Subscription(() => Unsubscribe(handler));
        }

        public void Publish<T>(T message)
        {
            if (!subscribers.TryGetValue(typeof(T), out var handlers) || handlers.Count == 0)
            {
                return;
            }

            var snapshot = handlers.ToArray();
            foreach (var subscriber in snapshot)
            {
                try
                {
                    ((Action<T>)subscriber).Invoke(message);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void Clear()
        {
            subscribers.Clear();
        }

        private void Unsubscribe<T>(Action<T> handler)
        {
            if (!subscribers.TryGetValue(typeof(T), out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                subscribers.Remove(typeof(T));
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;

            public Subscription(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                var action = dispose;
                dispose = null;
                action?.Invoke();
            }
        }
    }
}
