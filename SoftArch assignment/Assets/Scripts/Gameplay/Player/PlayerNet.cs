using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Logic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;

public class PlayerNet : NetworkBehaviour
{
    //Entity _entity;
    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    _entity = this.gameObject.GetComponent<Entity>();
    //    if (_entity != null)
    //    {
    //        EnemyAI.RegisterPlayer(_entity);
    //        // EntityManager.Instance?.Register(_entity);
    //    }
    //}

    //public override void OnStopServer()
    //{
    //    base.OnStopServer();
    //    if (_entity == null) _entity = this.gameObject.GetComponent<Entity>();
    //    if (_entity != null)
    //    {
    //        EnemyAI.UnregisterPlayer(_entity);
    //        // EntityManager.Instance?.Unregister(_entity);
    //    }
    //}
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