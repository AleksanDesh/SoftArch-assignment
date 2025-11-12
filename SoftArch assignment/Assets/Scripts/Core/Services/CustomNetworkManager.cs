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
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // default behavior (adds player object)
        base.OnServerAddPlayer(conn);

        // make sure we have dungeon manager ref
        if (dungeonManager == null) dungeonManager = Object.FindAnyObjectByType<DungeonCrawler.Levels.Runtime.DungeonManager>();

        // send current dungeon state to the newly connected client
        if (dungeonManager != null)
        {
            dungeonManager.SendFullStateToClient(conn);
            Debug.Log($"CustomNetworkManager: Sent dungeon full state to client {conn.connectionId}");
        }
    }
}
