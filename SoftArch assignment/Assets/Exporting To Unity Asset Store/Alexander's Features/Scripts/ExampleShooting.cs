
using UnityEngine;

namespace AlexanderFeatures
{
    public class ExampleShooting : MonoBehaviour
    {
        public Identity MyIdentity;
        public Identity OtherIdentity;
        public ItemHolder Pickup;
        public void KillEnemy()
        {
            QuestBus.Instance.Enqueue(new DeathEvent(OtherIdentity, MyIdentity));
        }

        public void GetItem()
        {
            QuestBus.Instance.Enqueue(new ItemPickedEvent(MyIdentity, OtherIdentity, Pickup.Item, 1));
        }
    }
}
