using UnityEngine;

public class AttackStateForwarderBehaviour : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Debug.Log("I entered the attack state");
        var forwarder = animator.GetComponentInChildren<AnimationEventForwarder>(includeInactive: true);
        forwarder?.OnAttackAnimationStart();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var forwarder = animator.GetComponentInChildren<AnimationEventForwarder>(includeInactive: true);
        forwarder?.OnAttackAnimationEnd();
        //Debug.Log($"I exited the attack state, and {forwarder}");
    }
}
