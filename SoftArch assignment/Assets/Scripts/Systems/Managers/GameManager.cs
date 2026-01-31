using DungeonCrawler.Core.Events;
using DungeonCrawler.Gameplay.Combat;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;



namespace DungeonCrawler.Systems
{
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
            {// Won't work properly with online stuff
                localPlayer.GetComponent<Health>().RestoreHp();
                //localPlayer.SetActive(true);
                var nid = localPlayer.GetComponent<NetworkIdentity>();
                var conn = nid.connectionToClient;
                NetworkServer.Spawn(localPlayer, conn);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                bool godMode = localPlayer.GetComponent<Health>().godMode;
                localPlayer.GetComponent<Health>().godMode = !godMode;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Core.Events.EventBus.Instance.Enqueue(new ExperienceGainedEvent(localPlayer.GetComponent<Core.Utils.Entity>(), 999999999));
            }

        }


    }
}