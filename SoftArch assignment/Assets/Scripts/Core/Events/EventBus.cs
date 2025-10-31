using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Core.Events
{
    public class EventBus : MonoBehaviour
    {
        public static EventBus Instance { get; private set; }

        // Subscriptions: event type -> list of delegates
        readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        // Processing queues
        readonly Queue<GameEvent> _currentQueue = new Queue<GameEvent>();
        readonly Queue<GameEvent> _nextQueue = new Queue<GameEvent>();

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

        void Dispatch(GameEvent ev)
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
                    handlers[i].DynamicInvoke(ev);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        // Enqueue for immediate processing in this tick/frame
        public void Enqueue(GameEvent ev)
        {
            if (ev == null) return;
            _currentQueue.Enqueue(ev);
        }

        // Enqueue for next tick/frame
        public void EnqueueNext(GameEvent ev)
        {
            if (ev == null) return;
            _nextQueue.Enqueue(ev);
        }

        // Strongly-typed subscribe/unsubscribe
        public void Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : GameEvent
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list)) return;
            list.Remove(handler);
            if (list.Count == 0) _subscribers.Remove(type);
        }
    }
}