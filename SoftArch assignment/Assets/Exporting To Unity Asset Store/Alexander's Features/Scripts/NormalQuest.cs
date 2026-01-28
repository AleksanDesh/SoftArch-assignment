using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlexanderFeatures
{
    [CreateAssetMenu(menuName = "AlexanderFeatures/Quests/Quest", fileName = "Quest")]
    public class Quest : ScriptableObject
    {
        public string questID;
        public string questName;
        public string description;
        public List<QuestObjective> objectives;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questID))
            {
                questID = questName + Guid.NewGuid().ToString();
            }
        }

    }
    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveID;
        public string description;
        public ObjectiveType type;

        public int requiredAmount;
        public int currentAmount;

        public string targetName;

        public bool IsCompleted => currentAmount >= requiredAmount;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectiveID))
            {
                objectiveID = description + Guid.NewGuid().ToString();
            }
        }
    }

    public enum ObjectiveType
    {
        CollectItem,
        DefeatEnemy,
        ReachLocation,
        TalkNPC,
        Custom
    }



    /// <summary>
    /// an active quest, that has progress stored inside
    /// </summary>
    [System.Serializable]
    public class QuestProgress
    {
        public Quest quest;
        public List<QuestObjective> objectives;
        public event Action<QuestProgress> OnUpdated;

        public QuestProgress(Quest quest)
        {
            this.quest = quest;
            objectives = new List<QuestObjective>();

            foreach (var obj in quest.objectives)
            {
                objectives.Add(new QuestObjective
                {
                    objectiveID = obj.objectiveID,
                    description = obj.description,
                    type = obj.type,
                    requiredAmount = obj.requiredAmount,
                    currentAmount = 0,
                    targetName = obj.targetName

                });

                switch (obj.type)
                { // *Event bus checks if duplicate*
                    case ObjectiveType.CollectItem:
                        if (QuestBus.Instance != null) QuestBus.Instance.Subscribe<ItemPickedEvent>(TryIncrement);
                        break;
                    case ObjectiveType.DefeatEnemy:
                        if (QuestBus.Instance != null) QuestBus.Instance.Subscribe<DeathEvent>(TryIncrement);
                        break;
                    case ObjectiveType.ReachLocation:
                        break;
                    case ObjectiveType.TalkNPC:
                        break;
                    case ObjectiveType.Custom:
                        break;
                    default:
                        break;
                }
            }

        }

        ~QuestProgress()
        {
            UnsubscribeFromAll();
        }

        public bool IsCompleted => objectives.TrueForAll(o => o.IsCompleted);

        public string QuestID => quest.questID;

        /// <summary>
        /// Increments objective counter if GameEvent is relevant to the quest
        /// </summary>
        /// <param name="ev"></param>
        void TryIncrement(BaseEvent ev)
        {
            if (IsCompleted) return;
            bool anyProgress = false;
            foreach (var obj in objectives)
            {
                if (obj.IsCompleted) continue;
                switch (ev)
                {
                    case DeathEvent de:
                        if (de.SourceIdentity.EntityTag == obj.targetName)
                        {
                            obj.currentAmount++;
                            anyProgress = true;
                        }
                        break;
                    case ItemPickedEvent ipe:
                        if (ipe.ItemDef.ItemName == obj.targetName)
                        {
                            obj.currentAmount += ipe.Quantity;
                            anyProgress = true;
                        }
                        break;
                    default:
                        break;

                }
            }
            if (anyProgress)
            {
                if (IsCompleted)
                {
                    UnsubscribeFromAll();
                }
                OnUpdated?.Invoke(this);
                // somehow make the UI update me.
            }
        }

        void UnsubscribeFromAll()
        {
            foreach (var obj in objectives)
            {
                switch (obj.type)
                {
                    case ObjectiveType.CollectItem:
                        if (QuestBus.Instance != null) QuestBus.Instance.Unsubscribe<ItemPickedEvent>(TryIncrement);
                        break;
                    case ObjectiveType.DefeatEnemy:
                        if (QuestBus.Instance != null) QuestBus.Instance.Unsubscribe<DeathEvent>(TryIncrement);
                        break;
                    case ObjectiveType.ReachLocation:
                        break;
                    case ObjectiveType.TalkNPC:
                        break;
                    case ObjectiveType.Custom:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
