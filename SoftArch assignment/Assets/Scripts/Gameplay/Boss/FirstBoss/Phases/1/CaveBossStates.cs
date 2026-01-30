using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Combat;
using Mirror;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Boss.FirstBoss
{
    // Idle
    public class IdleState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        float timer = 1f;
        float wait = 1f;

        public IdleState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "Idle";
        }

        public override void Enter()
        {
            base.Enter();
            owner.Animator.SetBool("Idle", true);
            timer = 0f;
        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= wait)
            {
                IsFinished = true;
            }
        }
        // Called when the state is exited.
        // Can be overridden by derived states to perform cleanup logic.
        public override void Exit()
        {
            owner.Animator.SetBool("Idle", false);
            base.Exit();
        }
    }

    // Melee attack
    public class MeleeAttackState : State
    {
        protected BossStateMachine owner;
        CaveBossBlackboard bb;
        float attackDuration = 1.8f;
        float timer;

        public MeleeAttackState(BossStateMachine owner)
        {
            this.owner = owner;
            bb = owner.GetComponent<CaveBossBlackboard>();
            attackDuration = bb.MeleeAttackDuration;
            StateName = "MeleeAttack";
        }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;
            owner.NetAnimator.SetTrigger("Melee");

            if (!owner.isServer)
                return;

            if (bb == null || bb.MeleeSpikePrefab == null)
                return;

            // Spawns GOs in rings with a delay, depending on how far it is from center.
            Vector3 center = owner.transform.position;
            float ringStep = bb.MeleeMaxRadius / Mathf.Max(1, bb.MeleeRings);

            for (int ring = 0; ring <= bb.MeleeRings; ring++)
            {
                float radius = ring * ringStep;
                int count = ring == 0 ? 1 : bb.MeleeSpikesPerRing;

                for (int i = 0; i < count; i++)
                {
                    Vector3 offset;

                    if (ring == 0)
                    {
                        offset = Vector3.zero;
                    }
                    else
                    {
                        float angle = (i / (float)count) * Mathf.PI * 2f;
                        offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    }

                    Vector3 groundPos = center + offset;
                    Vector3 randOffset = new Vector3(0, (UnityEngine.Random.Range(-.1f, .1f)), 0);
                    Vector3 spawnPos = groundPos + Vector3.down * bb.MeleeSpikeSpawnDepth + randOffset;

                    float delay = offset.magnitude * bb.MeleeDelayPerUnit + bb.MeleeAttackAnticipation;

                    float yawOffset = UnityEngine.Random.Range(-180, 180);
                    float pitchOffset = - 90.0f;
                    Quaternion rot = Quaternion.Euler(pitchOffset, yawOffset, 0f);

                    GameObject go = GameObject.Instantiate(bb.MeleeSpikePrefab, spawnPos, rot);

                    var spike = go.GetComponent<CaveBossSpike>();
                    if (spike != null)
                    {
                        spike.Initialize(
                            Vector3.up,
                            bb.MeleeSpikeSpawnDepth,
                            bb.MeleeSpikeRiseTime,
                            bb.MeleeForwardSpeed,
                            bb.MeleeDecelTime,
                            bb.MeleeSpikeLifeAfterStop,
                            bb.MeleeSpikeRiseHeight,
                            bb.MeleeDamage,
                            delay
                        );
                    }

                    NetworkServer.Spawn(go);
                }
            }

            if (owner.isServer)
            {
                owner.RpcShowVisualAttackCircleClue(center, bb.MeleeImpactRadius, bb.MeleeVisualTime, bb.CircleSegments);
            }
        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= attackDuration)
            {
                IsFinished = true;
            }

            // transition logic handled by the FSM transitions
        }

        public override void Exit()
        {
            //owner.Animator.SetBool("Melee", false);
            base.Exit();
        }
    }

    // Ranged attack
    public class RangedAttackState : State
    {
        protected BossStateMachine owner;
        CaveBossBlackboard bb;
        float duration = 5.0f;
        float timer;

        public RangedAttackState(BossStateMachine owner)
        {
            this.owner = owner;
            bb = owner.GetComponent<CaveBossBlackboard>();
            duration = bb.RangedAttackDuration;
            StateName = "RangedAttack";
        }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;

            if (owner == null || owner.GetComponent<CaveBossBlackboard>() == null)
            {
                Debug.LogWarning("[RangedAttackState] Missing owner or blackboard; aborting ranged spawn.");
                return;
            }

            owner.NetAnimator.SetTrigger("Ranged");

            if (bb.RangedSpikePrefab == null)
            {
                Debug.LogWarning("[RangedAttackState] RangedSpikePrefab not set on blackboard.");
                return;
            }

            // spawn multiple spikes within the spawn radius
            for (int i = 0; i < Mathf.Max(1, bb.RangedSpikeCount); i++)
            {
                // choose a random point inside a circle on the XZ plane
                Vector2 rand = UnityEngine.Random.insideUnitCircle * bb.RangedSpawnRadius;
                Vector3 spawnGround = owner.transform.position + new Vector3(rand.x, 0f, rand.y);

                // start below ground by SpikeSpawnDepth
                Vector3 spawnPos = spawnGround + Vector3.down * bb.SpikeSpawnDepth;

                float yawOffset = UnityEngine.Random.Range(-180, 180);
                float pitchOffset = UnityEngine.Random.Range(-bb.SpikeAngleVariance, bb.SpikeAngleVariance) - 90.0f;
                Quaternion rot = Quaternion.Euler(pitchOffset, yawOffset, 0f);
                Vector3 forwardDir = rot * Vector3.forward;

                // Instantiate on server
                GameObject go = GameObject.Instantiate(bb.RangedSpikePrefab, spawnPos, Quaternion.LookRotation(forwardDir, Vector3.up));
                var spike = go.GetComponent<CaveBossSpike>();
                if (spike != null)
                {
                    // Initialize parameters on server
                    spike.Initialize(
                        forwardDir.normalized,
                        bb.SpikeSpawnDepth,
                        bb.SpikeRiseTime,
                        bb.SpikeForwardSpeed,
                        bb.SpikeDecelTime,
                        bb.SpikeLifeAfterStop,
                        bb.SpikeRiseHeight,
                        bb.RangedDamage,
                        bb.RangedAttackAnticipation
                    );
                }
                NetworkServer.Spawn(go);

                if (owner.isServer)
                {
                    owner.RpcShowVisualAttackCircleClue(spawnGround, bb.SpikeImpactRadius, bb.SpikeVisualTime, bb.CircleSegments);
                }
            }
        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= duration)
            {
                IsFinished = true;
            }
            // simple: let spawned spikes handle their own lifetime
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
    // Heal state
    public class HealState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        CaveBossBlackboard bb;
        float timer;
        float healDuration = 4f;
        int healAmount = 15;

        public HealState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "Heal";
        }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;
            bb = owner.GetComponent<CaveBossBlackboard>();
            healDuration = bb.HealDuration;
            healAmount = bb.HealAmount;
            owner.Animator.SetBool("Heal", true);

        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= healDuration)
            {
                IsFinished = true;
                // call health heal (server-side in networked env)
                var health = owner.GetComponent<Health>();
                if (health != null)
                {
                    // For networked games, ensure Heal is called on server.
                    health.Heal(healAmount);
                }
            }
            // nothing else - transitions will return to idle
        }

        public override void Exit()
        {
            owner.Animator.SetBool("Heal", false);
            base.Exit();
        }
    }

    // Death state
    public class DeathState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        CaveBossBlackboard bb;
        bool started = false;
        Transform transform;
        bool rushing = false;

        public DeathState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            bb = owner.GetComponent<CaveBossBlackboard>();
            transform = owner.GetComponent<Transform>();
            StateName = "Death";
        }

        public override void Enter()
        {
            base.Enter();
            owner.Animator.SetBool("Death", true);
            if (!started)
            {
                started = true;
                owner.StartCoroutine(DeathCoroutine());
            }
        }

        System.Collections.IEnumerator DeathCoroutine()
        {
            // give some time for death animation / events
            yield return new WaitForSeconds(3.0f);
            rushing = true;
            // disable gameobject
            Debug.Log($"{this} has died");
            //owner.gameObject.SetActive(false);
        }

        public override void Step()
        {
            base.Step();
            transform.LookAt(owner.CurrentTarget);
            if (rushing)
            {
                transform.position += transform.forward.normalized * bb.DeathSpeed * Time.deltaTime;
                Collider[] hits = Physics.OverlapSphere(transform.position, 1);
                foreach (var hit in hits)
                {
                    Entity playerEntity = hit.GetComponent<Entity>();
                    Entity myEntity = owner.GetComponent<Entity>();
                    if (playerEntity != null && playerEntity.tag == "Player")
                    {
                        EventBus.Instance?.Enqueue(new DamageEvent(playerEntity, myEntity, bb.DeathDamage));
                        SpawnBlowingEffect(bb.DeathEffectGameObject, hit.transform);

                        // Destroy a bit later, so the entity is processed
                        NetworkBehaviour.Destroy(transform.gameObject, 0.1f);
                    }
                }
            }
        }

        [ClientRpc]
        void SpawnBlowingEffect(GameObject gm, Transform transform)
        {
            var go = GameObject.Instantiate(bb.DeathEffectGameObject, transform.position, Quaternion.identity);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                GameObject.Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        public override void Exit()
        {
            base.Exit();
            owner.Animator.SetBool("Death", false);
        }
    }
}
