// DungeonCrawler.Systems.Rewards
using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Items;
using UnityEngine;

namespace DungeonCrawler.Systems.Gameflow
{
    [DefaultExecutionOrder(-300)]
    public class LootSystem : MonoBehaviour
    {
        void Start()
        {
            if (EventBus.Instance != null) EventBus.Instance.Subscribe<DeathEvent>(OnDeath);
        }
        void OnDestroy()
        {
            if (EventBus.Instance != null) EventBus.Instance.Unsubscribe<DeathEvent>(OnDeath);
        }

        void OnDeath(DeathEvent ev)
        {
            if (ev == null || ev.SourceEntity == null) return;

            // find LootDropper on the dead entity 
            var dropper = ev.SourceEntity.GetComponent<LootDropper>();
            if (dropper != null)
            {
                Vector3 spawnPos = ev.SourceEntity.transform.position;
                dropper.HandleLoot(ev.SourceEntity, ev.TargetEntity, spawnPos);
            }
        }
    }
}
