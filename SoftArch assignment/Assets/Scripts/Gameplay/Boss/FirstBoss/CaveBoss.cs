using TMPro;
using UnityEngine;
using DungeonCrawler.Gameplay.Combat;

namespace DungeonCrawler.Gameplay.Boss.FirstBoss
{
    public class CaveBoss : MonoBehaviour
    {

        private BossStateMachine _currentStateMachine;
        private CaveBossStateMachineStage1 _fsm;

        Health _health;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _health = GetComponent<Health>();
            _fsm = GetComponent<CaveBossStateMachineStage1>();
            _currentStateMachine = _fsm;
        }

        // Update is called once per frame
        void Update()
        {
            _currentStateMachine.Update();
        }
    }
}