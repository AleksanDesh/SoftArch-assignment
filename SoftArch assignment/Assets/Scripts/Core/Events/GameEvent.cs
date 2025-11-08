using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items.Data;
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
        public int xp;
        public DeathEvent(Entity whoDied, Entity whoKilled = null, int xp = 0)
            : base(whoDied, whoKilled)
        {
            this.xp = xp;
        }
    }
    #endregion

    #region Inventory
    // Fired when an item is picked up into an inventory.
    // SourceEntity = picker (the entity who picked it up)
    // TargetEntity = optional item-world entity (null if no entity)
    public class ItemPickedEvent : GameEvent
    {
        public ItemDefinition ItemDef;
        public int Quantity; // how many were added to inventory

        public ItemPickedEvent(Entity picker, Entity itemWorldEntity, ItemDefinition def, int quantity)
            : base(picker, itemWorldEntity)
        {
            ItemDef = def;
            Quantity = quantity;
        }
    }

    // Fired when an item is used from an inventory
    public class ItemUsedEvent : GameEvent
    {
        public ItemDefinition ItemDef;
        public int Quantity;

        public ItemUsedEvent(Entity user, ItemDefinition def, int quantity = 1)
            : base(user, null)
        {
            ItemDef = def;
            Quantity = quantity;
        }
    }

    // Fired when an item is dropped from inventory into the world (spawned prefab).
    // TargetEntity can be the spawned world entity.
    public class ItemDroppedEvent : GameEvent
    {
        public ItemDefinition ItemDef;
        public int Quantity;

        public ItemDroppedEvent(Entity dropper, Entity worldEntity, ItemDefinition def, int quantity)
            : base(dropper, worldEntity)
        {
            ItemDef = def;
            Quantity = quantity;
        }
    }
    #endregion

    #region EXP
    // Fired when an entity gains experience.
    // SourceEntity = from (attacker, quest giver, etc.)
    // TargetEntity = to (player, pet)
    public class ExperienceGainedEvent : GameEvent
    {
        public int Amount;

        public ExperienceGainedEvent(Entity target, int amount, Entity source = null)
            : base(source, target)
        {
            Amount = amount;
        }
    }

    // Fired when an entity levels up.
    // SourceEntity = from (usually same as target or null)
    // TargetEntity = the leveled entity
    public class LevelUpEvent : GameEvent
    {
        public int OldLevel;
        public int NewLevel;
        public int RemainingXpInLevel; // XP carried over into the next level after level up
        // Maybe also make something happen when we level up few levels at once?
        public LevelUpEvent(Entity target, int oldLevel, int newLevel, int remainingXp)
            : base(null, target)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
            RemainingXpInLevel = remainingXp;
        }
    }
    #endregion
}
