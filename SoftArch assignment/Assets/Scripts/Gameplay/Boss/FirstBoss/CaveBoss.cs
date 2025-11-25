using TMPro;
using UnityEngine;

public class CaveBoss : MonoBehaviour
{

    private BossStateMachine currentStateMachine;
    private CaveBossStateMachineStage1 fsm;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentStateMachine = fsm;
    }

    // Update is called once per frame
    void Update()
    {
        currentStateMachine.Update();
    }
}
