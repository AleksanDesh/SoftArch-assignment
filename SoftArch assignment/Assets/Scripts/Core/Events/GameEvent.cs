using DungeonCrawler.Core.Utils;
using UnityEngine;

namespace DungeonCrawler.Core.Events
{
    /// <summary>
    /// Base class for all in-game events.
    /// Now uses Entity references instead of IDs for stronger typing and direct access.
    /// </summary>
    public class GameEvent
    {
        // Identity references
        public Entity SourceEntity;
        public Entity TargetEntity;

        // Control and metadata
        public bool Consumed = false; // set true to stop propagation
        public int Priority = 0;
        public float TimeCreated = 0f;

        public GameEvent()
        {
            TimeCreated = Time.time;
        }

        public GameEvent(Entity source, Entity target)
        {
            SourceEntity = source;
            TargetEntity = target;
            TimeCreated = Time.time;
        }
    }

    #region Movement events
    public class MoveIntentEvent : GameEvent
    {
        public Vector3 Destination;
        public bool UseNavMesh = true;

        public MoveIntentEvent(Entity source, Vector3 dest, bool useNavMesh = true)
            : base(source, null)
        {
            Destination = dest;
            UseNavMesh = useNavMesh;
        }
    }

    public class MoveAttemptEvent : GameEvent
    {
        public Vector3 Destination;

        public MoveAttemptEvent(Entity source, Vector3 dest)
            : base(source, null)
        {
            Destination = dest;
        }
    }

    public class MoveCompleteEvent : GameEvent
    {
        public Vector3 Position;

        public MoveCompleteEvent(Entity source, Vector3 pos)
            : base(source, null)
        {
            Position = pos;
        }
    }
    #endregion

    #region Combat / traps
    public class TrapTriggeredEvent : GameEvent
    {
        public int Damage;
        public Vector3 Position;

        public TrapTriggeredEvent(Entity target, int damage, Vector3 pos, Entity trapSource = null)
            : base(trapSource, target)
        {
            Damage = damage;
            Position = pos;
        }
    }

    public class DamageEvent : GameEvent
    {
        public int Amount;

        public DamageEvent(Entity target, Entity damager, int amount)
            : base(damager, target)
        {
            Amount = amount;
        }
    }

    public class DeathEvent : GameEvent
    {
        public DeathEvent(Entity dead, Entity killer = null)
            : base(killer, dead)
        {
        }
    }
    #endregion
}
