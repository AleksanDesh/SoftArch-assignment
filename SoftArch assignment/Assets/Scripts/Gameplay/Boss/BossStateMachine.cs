using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class BossStateMachine : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private float chaseRange = 3f;
    [SerializeField]
    private float chaseThreshold = 1f;
    [SerializeField]
    private float attackRange = 1.5f;
    [SerializeField]
    private float rotateSpeed = 90f;
    [SerializeField]
    private float idleTime = 2f;
    [SerializeField]
    private Animator animator;

    // The current state
    [SerializeReference]
    private State currentState;


    [SerializeField]
    private TextMeshProUGUI stateText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    public virtual void Update()
    {
        currentState.Step();
        if (currentState.NextState() != null)
        {
            //Cache the next state, because after currentState.Exit, calling
            //currentState.NextState again might return null because of change
            //of context.
            State nextState = currentState.NextState();
            currentState.Exit();
            currentState = nextState;
            stateText.text = currentState.stateName;
            currentState.Enter();
        }
    }
}
