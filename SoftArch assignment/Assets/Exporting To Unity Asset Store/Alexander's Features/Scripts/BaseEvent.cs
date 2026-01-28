using UnityEngine;

namespace AlexanderFeatures
{
    public class BaseEvent
    {
        // Identity references
        public Identity SourceIdentity;
        public Identity TargetIdentity;

        // Control and metadata
        public bool Consumed = false; // set true to stop propagation
        public int Priority = 0;
        public float TimeCreated = 0f;

        public BaseEvent()
        {
            TimeCreated = Time.time;
        }

        public BaseEvent(Identity source, Identity target)
        {
            SourceIdentity = source;
            TargetIdentity = target;
            TimeCreated = Time.time;
        }
    }


    /// <summary>
    /// Source is who died, target is who killed
    /// </summary>
    public class DeathEvent : BaseEvent
    {
        public int xp;
        public DeathEvent(Identity whoDied, Identity whoKilled = null, int xp = 0)
            : base(whoDied, whoKilled)
        {
            this.xp = xp;
        }
    }
    // Fired when an item is picked up into an inventory.
    // SourceIdentity = picker (the Identity who picked it up)
    // TargetIdentity = optional item-world Identity (null if no Identity)
    public class ItemPickedEvent : BaseEvent
    {
        public ExampleItem ItemDef;
        public int Quantity; // how many were added to inventory

        public ItemPickedEvent(Identity picker, Identity itemWorldEntity, ExampleItem def, int quantity)
            : base(picker, itemWorldEntity)
        {
            ItemDef = def;
            Quantity = quantity;
        }
    }
}