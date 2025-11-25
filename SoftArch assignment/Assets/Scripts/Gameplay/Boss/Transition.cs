using System;


[Serializable]
/// <summary>
/// Represents a transition between two states in the FSM.
/// Holds a condition that determines when the transition should occur,
/// and the state to transition to when the condition is true.
/// </summary>
public class Transition
{
    // A function delegate that returns true when the transition condition is met.
    // This is evaluated in Step() by states to check if the FSM should switch states.
    public Func<bool> condition;
    // The state to transition to if the condition returns true.
    public State nextState;

    // Constructor to create a new transition.
    public Transition(Func<bool> pCondition, State pNextState)
    {
        condition = pCondition;
        nextState = pNextState;
    }
}

