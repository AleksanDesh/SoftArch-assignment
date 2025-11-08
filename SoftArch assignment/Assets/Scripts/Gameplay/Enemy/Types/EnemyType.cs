using DungeonCrawler.Gameplay.Stats.Rewards;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Enemy.Types
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Enemy/EnemyType", fileName = "NewEnemyType")]
    public class EnemyType : ScriptableObject
    {
        [Header("Movement")]
        public float MoveSpeed = 3.5f;
        public float Acceleration = 8f;
        public float StoppingDistance = 1.0f;

        [Header("Aggro")]
        public float AggroRange = 10f;
        public float AggroDuration = 3f;

        [Header("Combat")]
        public float AttackRange = 2f;
        public float AttackCooldown = 1.2f;
        public int AttackDamage = 10;

        [Header("Optional")]
        [Tooltip("If true, the NavMesh mover will sample positions on the navmesh for destinations.")]
        public bool SampleTargetPositionOnNavMesh = true;
    }
}
