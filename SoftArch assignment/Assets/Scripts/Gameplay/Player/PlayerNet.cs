using KinematicCharacterController;
using Mirror;
using UnityEngine;

public class PlayerNet : NetworkBehaviour
{
    // Called on server: TargetTeleport(conn, dest)
    // Runs on the target client.
    [TargetRpc]
    public void TargetTeleport(NetworkConnection target, Vector3 dest)
    {
        //Debug.Log($"PlayerNet.TargetTeleport received on client for {gameObject.name} -> {dest}");

        var kcm = GetComponent<KinematicCharacterMotor>();
        if (kcm != null)
        {
            kcm.SetPosition(dest);
            return;
        }

        // fallback
        transform.position = dest;
    }

    [TargetRpc]
    public void GameObjectSetActive(GameObject gm, bool state)
    {
        if (gm != null) 
            gm.SetActive(state);
    }
}