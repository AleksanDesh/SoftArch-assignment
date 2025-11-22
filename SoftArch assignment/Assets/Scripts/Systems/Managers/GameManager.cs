using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using DungeonCrawler.Gameplay.Combat;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject localPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }


    // Update is called once per frame
    void Update()
    {
        if (localPlayer == null) return;
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.None ? CursorLockMode.Locked : CursorLockMode.None;

            Cursor.visible = (Cursor.lockState == CursorLockMode.None);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {// Not intended for multiplayer, so won't work properly in multiplayer.
            localPlayer.GetComponent<Health>().RestoreHp();
            localPlayer.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            bool godMode = localPlayer.GetComponent<Health>().godMode;
            localPlayer.GetComponent<Health>().godMode = !godMode;
        }
        
    }
}
