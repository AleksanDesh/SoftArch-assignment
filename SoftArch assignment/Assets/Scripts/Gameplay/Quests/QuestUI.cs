using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using TMPro;

namespace DungeonCrawler.Quests
{
    public class QuestUI : MonoBehaviour
    {

        public Transform questListContent;
        public GameObject questEntryPrefab;
        public GameObject objectiveTextPrefab;

        public Quest testQuest;
        public int testQuestAmount;
        private List<QuestProgress> testQuests = new();
        private Dictionary<QuestProgress, GameObject> entryByProgress = new();


        void Start()
        {
            for (int i = 0; i < testQuestAmount; i++)
            {
                var progress = new QuestProgress(testQuest);
                testQuests.Add(progress);
                var entry = Instantiate(questEntryPrefab, questListContent);
                entryByProgress[progress] = entry;
                SetupEntry(entry, progress);
                progress.OnUpdated += HandleQuestUpdated;
            }
        }

        /// <summary>
        /// Setting values for the quest. 
        /// </summary>
        /// <param name="entry"> quest GO </param>
        /// <param name="quest"> quest progress </param>
        void SetupEntry(GameObject entry, QuestProgress quest)
        {
            TMP_Text questNameText = entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            Transform objectiveList = entry.transform.Find("ObjectiveList");
            questNameText.text = quest.quest.questName;
            foreach (Transform ch in objectiveList) Destroy(ch.gameObject);
            foreach (var objective in quest.objectives)
            {
                GameObject objTextGO = Instantiate(objectiveTextPrefab, objectiveList);
                objTextGO.name = objective.objectiveID;
                TMP_Text objText = objTextGO.GetComponent<TMP_Text>();
                if (objective.IsCompleted)
                {
                    objText.text = $"{objective.description} DONE";
                    objText.color = Color.green;
                }
                else
                {
                    objText.text = $"{objective.description} ({objective.currentAmount} / {objective.requiredAmount})";
                }
            }
        }

        /// <summary>
        /// Updates existing quest values.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="quest"></param>
        void UpdateEntry(GameObject entry, QuestProgress quest)
        {
            Debug.Log($"Update entry called");
            TMP_Text questNameText = entry.transform.Find("QuestNameText").GetComponent<TMP_Text>();
            Transform objectiveList = entry.transform.Find("ObjectiveList");

            if (quest.IsCompleted)
            {
                Debug.Log("QUEST COMPLETED");
                questNameText.text = quest.quest.questName + " DONE";
                questNameText.color = Color.green;
                foreach (Transform ch in objectiveList) Destroy(ch.gameObject);
                return;
            }

            //questNameText.text = quest.quest.questName;

            foreach (var objective in quest.objectives)
            {
                Transform child = objectiveList.Find(objective.objectiveID);
                TMP_Text objText = child.GetComponent<TMP_Text>();
                if (objective.IsCompleted)
                {
                    objText.text = $"{objective.description} DONE";
                    objText.color = Color.green;
                }
                else
                {
                    objText.text = $"{objective.description} ({objective.currentAmount} / {objective.requiredAmount})";
                }
            }
        }

        /// <summary>
        /// Call this from Quest itself to update the UI
        /// </summary>
        /// <param name="progress"> the quest</param>
        void HandleQuestUpdated(QuestProgress progress)
        {
            if (!entryByProgress.TryGetValue(progress, out var entry)) return;
            UpdateEntry(entry, progress);
        }
    }
}