using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // Assign in inspector or find at runtime
    public DungeonCrawler.Levels.Runtime.DungeonManager dungeonManager;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (dungeonManager == null) dungeonManager = Object.FindAnyObjectByType<DungeonCrawler.Levels.Runtime.DungeonManager>();
    }

    [Server]
    public void SetActive(uint parentNetId, string relativePath, bool state)
    {
        if (!NetworkServer.active)
            return;

        ApplyActiveState(parentNetId, relativePath, state); // apply locally on server
        RpcSetActive(parentNetId, relativePath, state);      // replicate to clients
    }

    //[ClientRpc]
    void RpcSetActive(uint parentNetId, string relativePath, bool state)
    {
        // apply on all clients
        ApplyActiveState(parentNetId, relativePath, state);
    }

    void ApplyActiveState(uint parentNetId, string relativePath, bool state)
    {
        if (NetworkClient.spawned.TryGetValue(parentNetId, out var identity) && identity != null)
        {
            Transform target = identity.transform;

            if (!string.IsNullOrEmpty(relativePath))
            {
                target = identity.transform.Find(relativePath);
                if (target == null)
                {
                    Debug.LogWarning($"[ApplyActiveState] Could not find path '{relativePath}' under '{identity.name}'");
                    return;
                }
            }

            if (target != null && target.gameObject != null)
            {
                target.gameObject.SetActive(state);
            }
        }
        else
        {
            Debug.LogWarning($"[ApplyActiveState] Could not find NetworkIdentity with netId={parentNetId} passed relativePath {relativePath}");
        }
    }
}
