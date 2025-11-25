using UnityEngine;

/// <summary>
/// Forwards animation events to the scripts
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    PlayerWeaponController weaponCntrl;

    void Awake()
    {
        weaponCntrl = GetComponentInParent<PlayerWeaponController>();
    }
    // Called by animation event (no param)
    public void OnAttackAnimationStart()
    {
        if (weaponCntrl != null) weaponCntrl.OnAttackAnimationStart();
        else Debug.LogWarning("[AnimationEventForwarder] No PlayerWeaponController found in parents to forward OnAttackAnimationStart.");
    }

    // Called by animation event (no param)
    public void OnAttackAnimationEnd()
    {
        if (weaponCntrl != null) weaponCntrl.OnAttackAnimationEnd();
        else Debug.LogWarning("[AnimationEventForwarder] No PlayerWeaponController found in parents to forward OnAttackAnimationEnd.");
    }

    // LLM offer:
    // If you prefer, add overloads that accept AnimationEvent or a string:
    // public void OnAttackAnimationEnd(AnimationEvent evt) { ... }
}
