using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Base class for all FSM states.
/// Manages entry/exit hooks, transitions, and per-frame updates.
/// </summary>
public abstract class State
{
    public string stateName;
    public Action onEnter;
    public Action onExit;

    // List of possible transitions from this state to other states
    // Marked with [SerializeReference] to show properties in the Unity Inspector for easy debugging
    [SerializeReference]
    public List<Transition> transitions = new List<Transition>();

    // Called when the state is entered.
    // Can be overridden by derived states to perform setup logic.
    public virtual void Enter()
    {
        onEnter?.Invoke();
    }

    // Checks all transitions and returns the next state if any condition is met.
    // Returns the next state to transition to, or null if no conditions are met.
    public State NextState()
    {
        for (int i = 0; i < transitions.Count; i++)
        {
            if (transitions[i].condition())
            {
                return transitions[i].nextState;
            }
        }
        return null;
    }

    // Called when the state is exited.
    // Can be overridden by derived states to perform cleanup logic.
    public virtual void Exit()
    {
        onExit?.Invoke();
    }

    // Called every frame (or tick) while the state is active.
    // Can be overridden by derived states to implement custom behavior.
    public virtual void Step()
    {

    }
}

