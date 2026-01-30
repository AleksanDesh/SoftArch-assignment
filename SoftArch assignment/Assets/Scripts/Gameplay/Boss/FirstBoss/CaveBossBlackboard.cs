using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Boss.FirstBoss
{
    public class CaveBossBlackboard : BossBlackboard
    {
        [Header("CaveBoss values")]

        [Header("Healing")]
        [Tooltip("How much health to heal per heal")]
        public int HealAmount = 30;

        [Tooltip("Amount of time in this state")]
        public float HealDuration = 5;

        [Header("Melee Spike Attack")]
        public GameObject MeleeSpikePrefab;

        [Tooltip("Anticipation for spikes")]
        public float MeleeAttackAnticipation = 1f;

        [Tooltip("Amount of time in this state")]
        public float MeleeAttackDuration = 5;

        [Tooltip("Maximum radius of the melee spike wave")]
        public float MeleeMaxRadius = 6f;

        [Tooltip("Number of concentric rings (center counts as ring 0)")]
        public int MeleeRings = 5;

        [Tooltip("Number of spikes per ring (excluding center spike)")]
        public int MeleeSpikesPerRing = 12;

        [Tooltip("Base delay added per unit distance from center")]
        public float MeleeDelayPerUnit = 0.12f;

        [Tooltip("Depth below ground to spawn spikes from")]
        public float MeleeSpikeSpawnDepth = 2f;

        [Tooltip("Time it takes for spikes to rise from the ground")]
        public float MeleeSpikeRiseTime = 0.2f;

        [Tooltip("Initial forward speed after popping out")]
        public float MeleeForwardSpeed = 14f;

        [Tooltip("Time in seconds over which the spike decelerates to a stop")]
        public float MeleeDecelTime = 0.45f;

        [Tooltip("Additional height above ground after rising")]
        public float MeleeSpikeRiseHeight = 0.5f;

        [Tooltip("Lifetime after spike finishes movement")]
        public float MeleeSpikeLifeAfterStop = 2f;

        [Header("Ranged spike (tuning)")]
        [Tooltip("Prefab for the ranged spike. Must have NetworkIdentity and NetworkTransform on the prefab.")]
        public GameObject RangedSpikePrefab;

        [Tooltip("Anticipation for spikes")]
        public float RangedAttackAnticipation = 1f;

        [Tooltip("Amount of time in this state")]
        public float RangedAttackDuration = 5;

        [Tooltip("How many spikes to spawn for one ranged attack")]
        public int RangedSpikeCount = 6;

        [Tooltip("Spawn radius around the boss (randomized within)")]
        public float RangedSpawnRadius = 8f;

        [Tooltip("Spawn depth below ground (how far underground the spike starts)")]
        public float SpikeSpawnDepth = 2f;

        [Tooltip("How many units above ground the spike should end up after rising")]
        public float SpikeRiseHeight = 0.5f;

        [Tooltip("Time it takes the spike to rise from underground to ground")]
        public float SpikeRiseTime = 0.18f;

        [Tooltip("Initial forward speed after popping out")]
        public float SpikeForwardSpeed = 14f;

        [Tooltip("Time in seconds over which the spike decelerates to a stop")]
        public float SpikeDecelTime = 0.45f;

        [Tooltip("Maximum random yaw offset (degrees) relative to boss forward for each spike, +/-")]
        public float SpikeAngleVariance = 30f;

        [Tooltip("How long the spike remains after stopping (seconds) before the server destroys it)")]
        public float SpikeLifeAfterStop = 4f;

        [Header("Death")]
        [Tooltip("Death particle system")]
        public GameObject DeathEffectGameObject;

        [Tooltip("Anticipation for death")]
        public float DeathAnticipation = 3f;

        [Tooltip("Movespeed")]
        public float DeathSpeed = 1f;

        [Tooltip("Death damage amount")]
        public int DeathDamage = 9999;

        [Header("Ranged visual")]
        [Tooltip("Radius of the impact circle to show on the ground")]
        public float SpikeImpactRadius = 0.5f;

        [Tooltip("How long (seconds) the impact marker should be visible on clients")]
        public float SpikeVisualTime = 1.25f;

        [Header("Melee visual")]
        [Tooltip("Radius of the impact circle to show on the ground")]
        public float MeleeImpactRadius = 0.5f;

        [Tooltip("How long (seconds) the impact marker should be visible on clients")]
        public float MeleeVisualTime = 1.25f;

        [Header ("Visuals setup")]
        [Tooltip("Number of segments used to draw the circle (higher = smoother)")]
        public int CircleSegments = 32;
    }
}