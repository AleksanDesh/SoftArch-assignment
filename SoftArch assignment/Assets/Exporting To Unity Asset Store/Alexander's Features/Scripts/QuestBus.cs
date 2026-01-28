using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlexanderFeatures
{
    public class QuestBus : MonoBehaviour
    {
        public static QuestBus Instance { get; private set; }

        // Subscriptions: event type -> list of delegates
        //readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
        readonly Dictionary<Type, List<(Delegate typedHandler, Action<BaseEvent> adapter)>> _subscribers = new Dictionary<Type, List<(Delegate, Action<BaseEvent>)>>();

        // Processing queues
        readonly Queue<BaseEvent> _currentQueue = new Queue<BaseEvent>();
        readonly Queue<BaseEvent> _nextQueue = new Queue<BaseEvent>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        void LateUpdate()
        {
            ProcessTick();
        }

        void ProcessTick()
        {
            while (_currentQueue.Count > 0)
            {
                var ev = _currentQueue.Dequeue();
                Dispatch(ev);
            }

            // swap queues: next becomes current for next frame/tick
            while (_nextQueue.Count > 0)
                _currentQueue.Enqueue(_nextQueue.Dequeue());
        }

        void Dispatch(BaseEvent ev)
        {
            if (ev == null) return;
            var type = ev.GetType();
            if (!_subscribers.TryGetValue(type, out var list)) return;

            // iterate a copy to allow subscribe/unsubscribe during dispatch
            var handlers = list.ToArray();
            for (int i = 0; i < handlers.Length; i++)
            {
                if (ev.Consumed) break;
                try
                {
                    handlers[i].adapter(ev);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        // Enqueue for immediate processing in this tick/frame
        public void Enqueue(BaseEvent ev)
        {
            if (ev == null) return;
            _currentQueue.Enqueue(ev);
        }

        // Enqueue for next tick/frame
        public void EnqueueNext(BaseEvent ev)
        {
            if (ev == null) return;
            _nextQueue.Enqueue(ev);
        }

        // Strongly-typed subscribe/unsubscribe
        public void Subscribe<T>(Action<T> handler) where T : BaseEvent
        {
            if (handler == null) return;
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<(Delegate, Action<BaseEvent>)>();
                _subscribers[type] = list;
            }

            // Prevent duplicate subscriptions of the same handler
            foreach (var entry in list)
            {
                if (Delegate.Equals(entry.typedHandler, handler))
                    return;
            }

            Action<BaseEvent> wrapper = ev =>
            {
                if (ev is T te) handler(te);
            };

            list.Add((handler, wrapper));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : BaseEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (Delegate.Equals(list[i].typedHandler, handler))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
            if (list.Count == 0) _subscribers.Remove(type);
        }
    }
}