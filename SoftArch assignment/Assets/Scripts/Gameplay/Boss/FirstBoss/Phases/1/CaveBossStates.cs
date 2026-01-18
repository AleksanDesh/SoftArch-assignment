using DungeonCrawler.Gameplay.Combat;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Boss.FirstBoss
{
    // Idle
    public class IdleState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        float timer = 0f;
        float wait = 1f;

        public IdleState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "Idle";
        }

        public override void Enter()
        {
            base.Enter();
            
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
            base.Exit();
        }
    }

    // Melee attack
    public class MeleeAttackState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        float attackDuration = 0.8f;
        float timer;

        public MeleeAttackState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "MeleeAttack";
        }

        public override void Enter()
        {
            base .Enter();
            timer = 0f;
            // TODO: play VFX, deal damage to player (call player's health / damage)
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
            base.Exit();
        }
    }

    // Ranged attack
    public class RangedAttack : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        float duration = 0.6f;
        float timer;

        public RangedAttack(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "RangedAttack";
        }

        public override void Enter()
        {
            base.Enter();
            timer = 0f;
            // TODO: instantiate projectile / do ranged damage
        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= duration)
            {
                IsFinished = true;
            }
            // simple: let animation/event spawn projectile; transition back via transition
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
        float timer;
        float healDuration = 1.2f;
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
            // call health heal (server-side in networked env)
            var health = owner.GetComponent<Health>();
            if (health != null)
            {
                // For networked games, ensure Heal is called on server.
                health.Heal(healAmount);
            }
        }

        public override void Step()
        {
            timer += Time.deltaTime;
            if (timer >= healDuration)
            {
                IsFinished = true;
            }
            // nothing else - transitions will return to idle
        }

        public override void Exit()
        {
            base.Exit();
        }
    }

    // Death state
    public class DeathState : Gameplay.Boss.State
    {
        protected Gameplay.Boss.BossStateMachine owner;
        bool started = false;

        public DeathState(Gameplay.Boss.BossStateMachine owner)
        {
            this.owner = owner;
            StateName = "Death";
        }

        public override void Enter()
        {
            base.Enter();
            if (!started)
            {
                started = true;
                owner.StartCoroutine(DeathCoroutine());
            }
        }

        System.Collections.IEnumerator DeathCoroutine()
        {
            // give some time for death animation / events
            yield return new WaitForSeconds(1.0f);
            // disable gameobject
            Debug.Log($"{this} has died");
            owner.gameObject.SetActive(false);
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
