using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollisionCheck : MonoBehaviour
{
    [SerializeField] private Entity userEntity;

    HashSet<Entity> affectedEntities = new HashSet<Entity>();

    void Start()
    {
        if (userEntity == null)
            Debug.LogWarning($"{this} doesn't have a userEntity, please assign it in the inspector");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (userEntity == null)
        {
            Debug.LogWarning($"{this} requires userEntity to function, please assign it in the inspector");
            return;
        }
        if (other.gameObject.TryGetComponent<Entity>(out var otherEntity) && otherEntity != userEntity && !affectedEntities.Contains(otherEntity))
        {
            //Debug.Log("Triggered by " + other.gameObject.name);
            if (EventBus.Instance != null)
            {
                affectedEntities.Add(otherEntity);
                EventBus.Instance.Enqueue(new DamageEvent(otherEntity, userEntity, 10));
                //Debug.Log($"Weapon is trying to damage {otherEntity.name}");
            }
        }
        
    }

    public void ListenForAttack()
    {
        //Debug.Log($"Clearing attacked entities information, that was containing {affectedEntities.Count}");
        affectedEntities.Clear();
        // Reset the hastable
    }
}
