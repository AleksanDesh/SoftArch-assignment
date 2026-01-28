using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Combat;
using Mirror;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;

namespace DungeonCrawler.Gameplay.Boss
{
    public class BossStateMachine : NetworkBehaviour
    {
        [Header("Movement / Ranges")]
        protected NavMeshAgent Agent;
        protected BossBlackboard Blackboard;

        [Header("Animation")]
        [SerializeField]
        public Animator Animator;
        [SerializeField]
        protected NetworkAnimator NetAnimator;

        [Header("Debug / UI")]
        [SerializeField]
        protected TextMeshPro StateText;
        [Tooltip("Enable console debug logs for FSM activity")]
        public bool EnableDebug = true;
        [Tooltip("Draw gizmos for ranges and target")]
        public bool DrawGizmos = false;

        // The current state
        [SerializeReference]
        protected State _currentState;

        List<Transform> _targets = new List<Transform>();
        Transform _currentTarget = null;
        public Transform CurrentTarget => _currentTarget;


        protected virtual void Start()
        {
            NetAnimator = GetComponent<NetworkAnimator>();
            SetAllPlayersAsTargets();
        }

        public virtual void Update()
        {


            if (_currentTarget == null)
            {
                SearchForTarget();
            }
            if (_currentState != null)
            {
                _currentState.Step();
                if (_currentState.NextState() != null)
                {
                    //Cache the next state, because after currentState.Exit, calling
                    //currentState.NextState again might return null because of change
                    //of context.
                    State nextState = _currentState.NextState();
                    _currentState.Exit();
                    _currentState = nextState;
                    _currentState.Enter();

                    if (EnableDebug)
                    {
                        DebugInfo();
                        Debug.Log($"[FSM] Transition: {_currentState.StateName} -> {nextState.StateName}");
                    }
                }
            }
        }

        public void SetInitialState(State s)
        {
            _currentState = s;
            _currentState?.Enter();
        }

        public void AddTarget(Transform t)
        {
            if (t != null && !_targets.Contains(t)) _targets.Add(t);
        }

        public void RemoveTarget(Transform t)
        {
            if (t != null && _targets.Contains(t)) _targets.Remove(t);
        }


        public float DistanceToPrimary()
        {
            if (_currentTarget == null) return Mathf.Infinity;
            return Vector3.Distance(transform.position, _currentTarget.position);
        }

        void SearchForTarget()
        {
            if (_targets.Count == 0) SetAllPlayersAsTargets();
            if (_targets.Count == 0)
            {
                if (EnableDebug) Debug.Log($"[FSM] found no targets");
                return;
            }
            float bestSqr = float.MaxValue;
            Transform best = null;
            foreach (Transform t in _targets)
            {
                float dSqr = (transform.position - t.position).sqrMagnitude;

                // choose the closest among those in range
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = t;
                }
            }
            _currentTarget = best;
            if (EnableDebug) Debug.Log($"[FSM] Found new target {_currentTarget.name}");
        }

        /// <summary>
        /// Gets all players and stores them internally.
        /// Selects the closest player  as a _currentTarget 
        /// </summary>
        void SetAllPlayersAsTargets()
        {
            float bestSqr = float.MaxValue;
            Transform best = null;

            foreach (var kv in NetworkServer.connections)
            {
                var conn = kv.Value;
                if (conn == null || conn.identity == null) continue;

                var go = conn.identity.gameObject;
                if (go == null) continue;

                // skip if not an Entity
                var entity = go.GetComponent<Entity>();
                if (entity == null) continue;

                _targets.Add(go.transform);
                float dSqr = (go.transform.position - transform.position).sqrMagnitude;

                // choose the closest among those in range
                if (dSqr < bestSqr)
                {
                    bestSqr = dSqr;
                    best = go.transform;
                }
            }
            if (EnableDebug) Debug.Log($"[FSM] Found {_targets.Count} network players as targets. Primary = {_currentTarget?.name ?? "none"}");
        }

        void DebugInfo()
        {
            // Build diagnostics for all transitions belonging to the current state
            var sb = new StringBuilder();
            sb.AppendLine($"State: {_currentState.StateName} IsFinised: {_currentState.IsFinished}");
            sb.AppendLine($"Target: {_currentTarget?.name ?? "none"} Dist: {DistanceToPrimary():F2}");

            // If there's a Health component, show HP for context
            var health = GetComponent<Health>();
            if (health != null)
            {
                sb.AppendLine($"HP: {health.GetCurrentHp()}/{health.GetMaxHP()}");
            }

            for (int i = 0; i < _currentState.Transitions.Count; i++)
            {
                var tr = _currentState.Transitions[i];
                string label = !string.IsNullOrEmpty(tr.Label) ? tr.Label : (tr._nextState != null ? tr._nextState.StateName : $"Transition{i}");
                string diag;
                bool value = false;

                // Evaluate
                try
                {
                    value = tr._condition?.Invoke() ?? false;
                    diag = value ? "TRUE" : "false";
                }
                catch (Exception ex)
                {
                    diag = $"ERROR: {ex.Message}";
                    value = false;
                }

                sb.AppendLine($" - {label}: {diag}");
            }

            // Write diagnostics to blackboard (so you can inspect it in the inspectors debug text)
            var bb = GetComponent<BossBlackboard>();
            if (bb != null)
            {
                bb.DebugNotes = sb.ToString();
            }

            // Optionally print to an on-screen text (assign in inspector)
            if (StateText != null)
            {
                StateText.text = sb.ToString();
            }

            // Console logging
            if (EnableDebug)
            {
                Debug.Log(sb.ToString());
            }
        }

        #region Gizmos
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !DrawGizmos) return;

            // Draw raged range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Blackboard.RangedRange);

            // Draw melee range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Blackboard.MeleeRange);

            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                Gizmos.DrawWireSphere(_currentTarget.position, 0.25f);

                foreach (Transform t in _targets)
                {
                    if (t == _currentTarget) continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, t.position);
                    Gizmos.DrawWireSphere(t.position, 0.25f);
                }
            }
            else
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
                Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
            }
        }
        #endregion
    }
}