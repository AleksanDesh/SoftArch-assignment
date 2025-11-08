using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using DungeonCrawler.Gameplay.Stats;
using UnityEngine;

namespace DungeonCrawler.Systems.Stats
{
    [DefaultExecutionOrder(-400)]
    public class LevelSystem : MonoBehaviour
    {
        [Tooltip("Default LevelConfig used if an ActorStats has no override.")]
        public LevelConfig DefaultConfig;

        void Start()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Subscribe<ExperienceGainedEvent>(OnExperienceGained);
        }

        void OnDestroy()
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Unsubscribe<ExperienceGainedEvent>(OnExperienceGained);
        }

        void OnExperienceGained(ExperienceGainedEvent ev)
        {
            if (ev == null || ev.Consumed) return;
            if (ev.TargetEntity == null) return;

            // Find ActorStats on the target entity
            var actor = ev.TargetEntity.GetComponent<ActorStats>();
            if (actor == null) return;

            // Add experience (actor handles leveling math). It returns LevelUpEvent(s).
            Debug.Log($"Level system sends experience to {ev.TargetEntity.name} from {ev.SourceEntity.name}");
            var lvlEvents = actor.AddExperience(ev.Amount, DefaultConfig);

            // Enqueue level up events (one per level gained)
            if (lvlEvents != null && lvlEvents.Count > 0 && EventBus.Instance != null)
            {
                foreach (var lev in lvlEvents)
                {
                    EventBus.Instance.Enqueue(lev);
                }
            }

            // Optionally consume the original XP event
            ev.Consumed = true;
        }

        // Helper API
        public void AwardExperience(Entity target, int amount, Entity source = null)
        {
            if (EventBus.Instance != null)
                EventBus.Instance.Enqueue(new ExperienceGainedEvent(target, amount, source));
        }
    }
}
