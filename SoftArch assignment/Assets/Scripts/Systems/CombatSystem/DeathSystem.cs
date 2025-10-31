using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using UnityEngine;
namespace DungeonCrawler.Systems.CombatSystem
{
    public class DeathSystem : MonoBehaviour
    {
        void Start()
        {
            EventBus.Instance.Subscribe<DeathEvent>(OnDeath);
            Debug.Log("DeathEvent subscribe type = " + typeof(DeathEvent).FullName);
        }

        void OnDestroy()
        {
            if (EventBus.Instance != null) EventBus.Instance.Unsubscribe<DeathEvent>(OnDeath);
        }

        void OnDeath(DeathEvent ev)
        {
            if (ev.Consumed) return;
            var entity = ev.TargetEntity;
            if (entity == null)
            {
                Debug.LogWarning($"DeathSystem: No entity found for id {ev.TargetEntity.Id}");
                ev.Consumed = true;
                return;
            }

            Debug.Log($"{entity.name} was slain by {ev.SourceEntity}.");
            Debug.Log($"Die call {ev.TimeCreated}, current time {Time.time}");
            entity.gameObject.SetActive(false);
        }
    }
}