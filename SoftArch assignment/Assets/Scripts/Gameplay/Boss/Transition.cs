using System;

namespace DungeonCrawler.Gameplay.Boss
{
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
        public Func<bool> _condition;
        // The state to transition to if the condition returns true.
        public State _nextState;
        // Debug    
        public string Label;

        // Constructor to create a new transition.
        public Transition(Func<bool> pCondition, State pNextState, string label = null)
        {
            _condition = pCondition;
            _nextState = pNextState;
            Label = label;
        }

        // Evaluate the condition and return the result.
        public bool Evaluate(out string diagnostic)
        {
            try
            {
                bool result = _condition?.Invoke() ?? false;
                diagnostic = result ? "TRUE" : "false";
                return result;
            }
            catch (Exception ex)
            {
                diagnostic = $"ERROR: {ex.Message}";
                return false;
            }
        }
    }
}

