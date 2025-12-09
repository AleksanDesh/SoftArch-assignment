using DungeonCrawler.Gameplay.Player.Controller;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(MyCharacterController))]
public class PlayerWeaponController : NetworkBehaviour
{
    [Header("Weapon objects (assign in prefab)")]
    [SerializeField] private GameObject frontWeapon; // real weapon in hand
    [SerializeField] private GameObject backWeapon;  // fake on back

    [Header("Animator")]
    [SerializeField] private Animator animator;      // assign or auto-find
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Options")]
    [Tooltip("If true, the AnimationClip will call OnAttackAnimationStart() to show the weapon at a precise frame.")]
    [SerializeField] private bool useAnimationEventToShowWeapon = true;

    private int _hashAttack;
    private bool _isAttacking = false;

    MyCharacterController myCharacterController;
    WeaponCollisionCheck weaponCollisionCheck;
    float savedSpeed;

    void Awake()
    {
        myCharacterController = GetComponent<MyCharacterController>();
        weaponCollisionCheck = GetComponentInChildren<WeaponCollisionCheck>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _hashAttack = Animator.StringToHash(attackTriggerName);

        // Try auto-find (but prefer assigning in the prefab)
        if (frontWeapon == null) frontWeapon = transform.Find("FrontWeapon")?.gameObject;
        if (backWeapon == null) backWeapon = transform.Find("BackWeapon")?.gameObject;

        // Default visuals (prefab default)
        if (frontWeapon != null) frontWeapon.SetActive(false);
        if (backWeapon != null) backWeapon.SetActive(true);

        Debug.Log($"[PlayerWeaponController] Awake. animator={(animator != null)}, front={(frontWeapon != null)}, back={(backWeapon != null)}");
    }

    // Only the owner should request an attack
    void Update()
    {
        if (!isLocalPlayer) return; // only owner listens for local input here

        if (!_isAttacking && Input.GetMouseButton(0))
        {
            // Request the server to broadcast the attack to everyone
            CmdRequestAttack();
        }
    }

    // Owner -> Server: request an attack (server can validate here)
    [Command]
    private void CmdRequestAttack()
    {
        // Optional: validate cooldowns, stamina, etc. on the server here.

        // Tell all clients (including owner) to play the attack
        RpcPlayAttack();
    }

    // Server -> Clients: run the animation and visuals
    [ClientRpc]
    private void RpcPlayAttack()
    {
        _isAttacking = true;
        weaponCollisionCheck.ListenForAttack();

        // Show front weapon immediately unless you want animation event to do it
        //if (!useAnimationEventToShowWeapon)
        //    ShowFrontWeapon();

        if (animator != null)
            animator.SetTrigger(_hashAttack);

        //Debug.Log("Dividing speed");

    }

    // Called by Animation Event at the frame where the weapon should appear
    // (optional — only used if useAnimationEventToShowWeapon == true)
    public void OnAttackAnimationStart()
    {
        if (frontWeapon != null && !frontWeapon.activeSelf)
        {
            ShowFrontWeapon();

        }
    }

    // Called by Animation Event at the *end* of the attack animation
    // IMPORTANT: This must exist on the same GameObject that has the Animator playing the clip
    public void OnAttackAnimationEnd()
    {
        //Debug.Log("Attempting to end");
        if (frontWeapon != null && frontWeapon.activeSelf)
            EndAttack();
    }

    private void ShowFrontWeapon()
    {
        if (frontWeapon != null) frontWeapon.SetActive(true);
        if (backWeapon != null) backWeapon.SetActive(false);
        savedSpeed = myCharacterController.MaxStableMoveSpeed;
        myCharacterController.MaxStableMoveSpeed = (savedSpeed / 10);
    }

    private void EndAttack()
    {
        //Debug.Log("Setting speed back to normal");
        myCharacterController.MaxStableMoveSpeed = savedSpeed;
        if (frontWeapon != null) frontWeapon.SetActive(false);
        if (backWeapon != null) backWeapon.SetActive(true);

        _isAttacking = false;
    }

    // Force cancel (callable by other systems)
    public void CancelAttack()
    {
        // Reset state locally and let server/clients know if needed
        EndAttack();
        if (animator != null)
            animator.ResetTrigger(attackTriggerName);
    }

    // For debugging while running multiple clients/host
    public override void OnStartLocalPlayer()
    {
        Debug.Log("[PlayerWeaponController] OnStartLocalPlayer on " + netId);
    }

    public override void OnStartClient()
    {
        Debug.Log("[PlayerWeaponController] OnStartClient on " + netId + " isLocal=" + isLocalPlayer);
    }
}
