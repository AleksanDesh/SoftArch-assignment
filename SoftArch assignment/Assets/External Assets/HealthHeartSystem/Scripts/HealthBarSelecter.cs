using Mirror;
using UnityEngine;

public class HealthBarSelecter : NetworkBehaviour
{
    public GameObject multiplayerHolder;
    public GameObject singleplayerHolder;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isLocalPlayer)
        {
            singleplayerHolder.SetActive(true);
            multiplayerHolder.SetActive(false);
        }
        else
        {
            singleplayerHolder.SetActive(false);
            multiplayerHolder.SetActive(true);
        }
    }
}
