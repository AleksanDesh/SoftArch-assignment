using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AlexanderFeatures
{
    public class QuestUIExample : MonoBehaviour
    {
        [SerializeField] private Transform questListContent;
        [SerializeField] private GameObject questEntryPrefab;
        [SerializeField] private GameObject objectiveTextPrefab;

        public Quest testQuest;
        public int testQuestAmount;
        private List<QuestProgress> _quests = new();
        private Dictionary<QuestProgress, GameObject> _entryByProgress = new();


        void Start()
        {
            // Debug quest spawning
            for (int i = 0; i < testQuestAmount; i++)
            {
                CreateQuest(new QuestProgress(testQuest));
            }
        }

        /// <summary>
        /// Setting values for the quest. 
        /// </summary>
        /// <param name="entry"> quest GO </param>
        /// <param name="quest"> quest progress </param>
        void CreateQuest(QuestProgress quest)
        {
            // Spawn the quest and save a reference
            _quests.Add(quest);
            var entry = Instantiate(questEntryPrefab, questListContent);
            _entryByProgress[quest] = entry;
            TMP_Text questNameText = entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            questNameText.text = quest.quest.questName;

            // Spawn all objectives of this quest
            Transform objectiveList = entry.transform.Find("ObjectiveList");
            foreach (var objective in quest.objectives)
            {
                GameObject go = Instantiate(objectiveTextPrefab, objectiveList);
                go.name = objective.objectiveID;
            }
            UpdateAllObjectives(entry, quest);
            quest.OnUpdated += HandleQuestUpdated;
        }

        /// <summary>
        /// Updates existing quest values.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="quest"></param>
        void UpdateQuest(GameObject entry, QuestProgress quest)
        {
            //Debug.Log($"Update entry called");
            TMP_Text questNameText = entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            if (quest.IsCompleted)
            {
                //Debug.Log("QUEST COMPLETED");
                questNameText.text = quest.quest.questName + " DONE";
                questNameText.color = Color.green;
                UpdateAllObjectives(entry, quest);
                quest.OnUpdated -= HandleQuestUpdated;
                return;
            }
            UpdateAllObjectives(entry, quest);
        }

        void UpdateAllObjectives(GameObject entry, QuestProgress quest)
        {
            Transform objectiveList = entry.transform.Find("ObjectiveList");
            foreach (var objective in quest.objectives)
            {
                Transform child = objectiveList.Find(objective.objectiveID);
                TMP_Text objText = child.GetComponent<TMP_Text>();
                if (!objective.IsCompleted)
                { // description + progress
                    objText.text = $"{objective.description} ({objective.currentAmount} / {objective.requiredAmount})";
                }
                else
                { // Mark the description as done
                    objText.text = $"{objective.description} DONE";
                    objText.color = Color.green;
                }
            }
        }

        /// <summary>
        /// Call this from Quest itself to update the UI
        /// </summary>
        /// <param name="progress"> the quest</param>
        void HandleQuestUpdated(QuestProgress progress)
        {
            if (!_entryByProgress.TryGetValue(progress, out var entry)) return;
            UpdateQuest(entry, progress);
        }

        void OnDestroy()
        {
            foreach (var progress in _entryByProgress.Keys)
            {
                progress.OnUpdated -= HandleQuestUpdated;
            }
            _entryByProgress.Clear();
        }
    }
}