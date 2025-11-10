using DungeonCrawler.Core.Events;
using DungeonCrawler.Core.Utils;
using UnityEngine;

public class WeaponCollisionCheck : MonoBehaviour
{
    [SerializeField] private Entity userEntity;

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
        if (other.gameObject.TryGetComponent<Entity>(out var otherEntity) && otherEntity != userEntity)
        {
            //Debug.Log("Triggered by " + other.gameObject.name);
            if (EventBus.Instance != null)
            {
                EventBus.Instance.Enqueue(new DamageEvent(otherEntity, userEntity, 10));
                //Debug.Log($"Weapon is trying to damage {otherEntity.name}");
            }
        }
        
    }
}
