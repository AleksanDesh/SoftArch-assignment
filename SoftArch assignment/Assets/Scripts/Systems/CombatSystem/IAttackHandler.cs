using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Enemy.Types;

namespace DungeonCrawler.Systems.CombatSystem
{
    /// <summary>
    /// Attack abstraction: performs attacks against a target entity. Implementations manage cooldowns, animations, and damage application.
    /// </summary>
    public interface IAttackHandler
    {
        /// <summary>Initialize the handler with owning entity and archetype.</summary>
        void Initialize(Entity owner, EnemyType archetype);

        /// <summary>Try to attack the target entity. Returns true if an attack was performed (cooldown consumed).</summary>
        bool TryAttack(Entity target);
    }
}
