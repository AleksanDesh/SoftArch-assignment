using DungeonCrawler.Gameplay.Combat;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Core.Events;
using System.Collections;

namespace DungeonCrawler.Gameplay.Boss
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class CaveBossSpike : NetworkBehaviour
    {
        [Header("Spike damage / hit")]
        [Tooltip("Default damage dealt when the spike hits a player (will be overwritten by boss if boss has a different value from - 1)")]
        public int Damage = 12;

        [Tooltip("Which layers the spike can hit (players/characters)")]
        public LayerMask HitLayers = ~0; // default: everything
        readonly HashSet<uint> _alreadyHit = new HashSet<uint>();

        float _startDelay;
        float _delayTimer;
        bool _activated;

        float _spawnDepth = 2f;
        float _riseTime = 0.18f;
        float _forwardSpeed = 12f;
        float _decelTime = 0.45f;
        float _lifeAfterStop = 4f;
        bool _isRetreating;
        float _retreatTimer;

        // new: how many units above ground to end up after rising
        float _riseHeight = 0.5f;

        Vector3 _forward = Vector3.forward;

        // internal state
        Vector3 _startPos;
        float _timer;
        float _moveTimer;
        bool _hasRisen;
        float _movedWhileForward;
        Entity _myEntity;
        Collider _myCollider;
        // Called on server immediately after Instantiate and before NetworkServer.Spawn
        // NOTE: added riseHeight parameter
        [Server]
        public void Initialize(Vector3 forward, float spawnDepth, float riseTime, float forwardSpeed, float decelTime, float lifeAfterStop, float riseHeight, int damage = -1, float startDelay = 0)
        {
            _forward = forward.normalized;
            _spawnDepth = spawnDepth;
            _riseTime = Mathf.Max(0.001f, riseTime);
            _forwardSpeed = forwardSpeed;
            _decelTime = Mathf.Max(0.01f, decelTime);
            _lifeAfterStop = Mathf.Max(0f, lifeAfterStop);
            _riseHeight = riseHeight;
            _startDelay = startDelay;
            if (damage != -1)
                Damage = damage;

            // Align rotation to forward
            transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

            _startPos = transform.position; // expected to be ground - spawnDepth
            _timer = 0f;
            _moveTimer = 0f;
            _hasRisen = false;
            _movedWhileForward = 0f;
            _myEntity = this.GetComponent<Entity>();
            _myCollider = this.GetComponent<Collider>();
        }

        // Server authoritative movement
        [ServerCallback]
        void FixedUpdate()
        {
            _delayTimer += Time.fixedDeltaTime;
            if (!_activated)
            {
                if (_delayTimer < _startDelay)
                    return;

                _activated = true;
                _myCollider.enabled = true;
            }

            _timer += Time.deltaTime;

            if (!_hasRisen)
            {
                float t = Mathf.Clamp01(_timer / _riseTime);

                Vector3 targetPos = _startPos + Vector3.up * (_spawnDepth + _riseHeight);
                transform.position = Vector3.Lerp(_startPos, targetPos, t);

                if (t >= 1f)
                {
                    _hasRisen = true;
                    _moveTimer = 0f;
                }
                return;
            }

            // Smooth forward movement at the end (unchanged)
            _moveTimer += Time.deltaTime;
            float decelT = Mathf.Clamp01(_moveTimer / _decelTime);
            float currentSpeed = Mathf.Lerp(_forwardSpeed, 0f, decelT);
            Vector3 delta = _forward * currentSpeed * Time.deltaTime;
            transform.position += delta;

            if (decelT >= 1f)
            {
                _myCollider.enabled = false;
                _isRetreating = true;
            }


            if (_isRetreating)
            {
                _retreatTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(_retreatTimer / _decelTime);
                float retreatSpeed = Mathf.Lerp(_forwardSpeed, 0f, t);
                Vector3 delta2 = -_forward * retreatSpeed * Time.deltaTime;

                transform.position += delta2;
                if (t >= 1f)
                {
                    ServerDestroy();
                    enabled = false;
                }
                return;
            }
        }

        [Server]
        void ServerDestroy()
        {
            if (isServer)
                NetworkServer.Destroy(gameObject);
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            if ((HitLayers.value & (1 << other.gameObject.layer)) == 0) return;
            if (other.transform.IsChildOf(transform)) return;
            var ni = other.GetComponentInParent<NetworkIdentity>();
            if (ni != null)
            {
                if (_alreadyHit.Contains(ni.netId)) return; // already applied
                _alreadyHit.Add(ni.netId);
            }

            // apply damage
            Entity ent = other.GetComponent<Entity>();
            if (ent != null)
            {
                if (EventBus.Instance != null) EventBus.Instance.Enqueue(new DamageEvent(ent, _myEntity, Damage));
            }
        }
    }
}
