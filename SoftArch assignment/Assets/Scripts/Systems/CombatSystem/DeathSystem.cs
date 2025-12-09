using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using Mirror;
using UnityEngine;
namespace DungeonCrawler.Systems.CombatSystem
{
    public class DeathSystem : MonoBehaviour
    {
        void Start()
        {
            EventBus.Instance.Subscribe<DeathEvent>(OnDeath);
            //Debug.Log("DeathEvent subscribe type = " + typeof(DeathEvent).FullName);
        }

        void OnDestroy()
        {
            if (EventBus.Instance != null) EventBus.Instance.Unsubscribe<DeathEvent>(OnDeath);
        }

        void OnDeath(DeathEvent ev)
        {
            if (ev.Consumed) return;
            var entity = ev.SourceEntity;
            if (entity == null)
            {
                Debug.LogWarning($"DeathSystem: No entity found for id {ev.SourceEntity.Id}");
                ev.Consumed = true;
                return;
            }
            //Debug.Log($"{entity.name} was slain by {ev.TargetEntity}.");

            if (entity.tag == "Player")
            {
                NetworkServer.UnSpawn(entity.gameObject);
                return;
            }


            if (ev.xp > 0 && ev.TargetEntity != null)
            {
                if (EventBus.Instance != null)
                {
                    //Debug.Log("Sending experience gain event to " + ev.TargetEntity.name);
                    EventBus.Instance.Enqueue(new ExperienceGainedEvent(ev.TargetEntity, ev.xp, ev.SourceEntity));
                }
            }

            //Debug.Log($"Die call {ev.TimeCreated}, current time {Time.time}");
            entity.gameObject.SetActive(false);
            if (entity.gameObject.TryGetComponent<NetworkIdentity>(out var identity))
            {
                //NetworkServer.UnSpawn(entity.gameObject);
                NetworkServer.Destroy(entity.gameObject);
                
            }
        }
    }
}