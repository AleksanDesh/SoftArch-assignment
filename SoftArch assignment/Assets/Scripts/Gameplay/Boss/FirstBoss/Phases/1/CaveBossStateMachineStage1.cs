using DungeonCrawler.Gameplay.Boss.FirstBoss;
using DungeonCrawler.Gameplay.Combat;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Boss
{
    public class CaveBossStateMachineStage1 : BossStateMachine
    {
        private State idle;
        private State melee;
        private State ranged;
        private State heal;
        private State death;

        protected override void Start()
        {
            Blackboard = GetComponent<BossBlackboard>();
            if (Blackboard == null)
            {
                Blackboard = gameObject.AddComponent<BossBlackboard>();
                if (EnableDebug) Debug.Log("[FSM] No Blackboard found on boss; added default BossBlackboard.");
            }

            var health = GetComponent<Health>();
            health.SetMaxHp(Blackboard.MaxHP);
            health.RestoreHp();

            // create instances of states
            idle = new FirstBoss.IdleState(this);
            melee = new FirstBoss.MeleeAttackState(this);
            ranged = new FirstBoss.RangedAttack(this);
            heal = new FirstBoss.HealState(this);
            death = new FirstBoss.DeathState(this);

            // transitions from idle
            idle.Transitions.Add(new Transition(() => health.GetCurrentHp() <= 0, death, "die_from_idle"));
            idle.Transitions.Add(new Transition(() => DistanceToPrimary() <= Blackboard.MeleeRange, melee, "idle_to_melee"));
            idle.Transitions.Add(new Transition(() => DistanceToPrimary() >= Blackboard.MeleeRange && DistanceToPrimary() < Blackboard.RangedRange, ranged, "idle_to_ranged_mid"));
            idle.Transitions.Add(new Transition(() => health.GetCurrentHp() <= (health.GetMaxHP() * 0.35f), heal, "idle_to_heal_lowhp"));

            // after melee -> go back to idle or to death
            melee.Transitions.Add(new Transition(() => health.GetCurrentHp() <= 0, death, "melee_to_death"));
            melee.Transitions.Add(new Transition(() => melee.IsFinished, idle, "melee_to_idle")); // use IsFinished

            // after ranged -> go back to idle or death
            ranged.Transitions.Add(new Transition(() => health.GetCurrentHp() <= 0, death, "ranged_to_death"));
            ranged.Transitions.Add(new Transition(() => ranged.IsFinished, idle, "ranged_to_idle"));

            // after heal -> idle or death
            heal.Transitions.Add(new Transition(() => health.GetCurrentHp() <= 0, death, "heal_to_death"));
            heal.Transitions.Add(new Transition(() => heal.IsFinished, idle, "heal_to_idle"));


            // death is terminal - no transitions

            idle.OnEnter += () => { Animator.SetBool("Idle", true); };
            idle.OnExit += () => { Animator.SetBool("Idle", false); };
            melee.OnEnter += () => { Animator.SetBool("Melee", true); };
            melee.OnExit += () => { Animator.SetBool("Melee", false); };
            ranged.OnEnter += () => { Animator.SetBool("Ranged", true); };
            ranged.OnExit += () => { Animator.SetBool("Ranged", false); };
            heal.OnEnter += () => { Animator.SetBool("Heal", true); };
            heal.OnExit += () => { Animator.SetBool("Heal", false); };

            // set initial state
            SetInitialState(idle);
            base.Start();
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
        }
    }
}