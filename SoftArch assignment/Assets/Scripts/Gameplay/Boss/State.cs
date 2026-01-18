using UnityEngine;
using System.Collections.Generic;
using System;


namespace DungeonCrawler.Gameplay.Boss
{
    /// <summary>
    /// Base class for all FSM states.
    /// Manages entry/exit hooks, transitions, and per-frame updates.
    /// </summary>
    public abstract class State
    {
        public string StateName;
        public bool IsFinished;
        public Action OnEnter;
        public Action OnExit;

        // List of possible transitions from this state to other states
        // Marked with [SerializeReference] to show properties in the Unity Inspector for easy debugging
        [SerializeReference]
        public List<Transition> Transitions = new List<Transition>();

        // Called when the state is entered.
        // Can be overridden by derived states to perform setup logic.
        public virtual void Enter()
        {
            IsFinished = false;
            OnEnter?.Invoke();
        }

        // Checks all transitions and returns the next state if any condition is met.
        // Returns the next state to transition to, or null if no conditions are met.
        public State NextState()
        {
            for (int i = 0; i < Transitions.Count; i++)
            {
                if (Transitions[i]._condition())
                {
                    return Transitions[i]._nextState;
                }
            }
            return null;
        }

        // Called when the state is exited.
        // Can be overridden by derived states to perform cleanup logic.
        public virtual void Exit()
        {
            IsFinished = false;
            OnExit?.Invoke();
        }

        // Called every frame (or tick) while the state is active.
        // Can be overridden by derived states to implement custom behavior.
        public virtual void Step()
        {

        }
    }
}