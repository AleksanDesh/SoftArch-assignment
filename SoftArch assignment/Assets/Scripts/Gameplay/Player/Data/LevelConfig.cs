using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonCrawler.Gameplay.Player.Data
{
    [CreateAssetMenu(menuName = "DungeonCrawler/Levels/LevelConfig", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Tooltip("Explicit XP required to go from level N to N+1.\nIf empty, fallback to formula (BaseXP * level^Exponent).")]
        public List<int> xpPerLevel = new List<int>();

        [Header("Fallback formula (used if xpPerLevel is empty)")]
        public int BaseXP = 100;
        [Range(1f, 3f)]
        public float Exponent = 1.2f;

        /// <summary>
        /// Returns XP required to progress from 'level' to level+1.
        /// Levels are 1-based. If level index is beyond xpPerLevel list, uses formula.
        /// </summary>
        public int GetXpForLevel(int level)
        {
            if (level <= 0) return 0;
            int idx = level - 1;
            if (xpPerLevel != null && idx >= 0 && idx < xpPerLevel.Count)
                return Mathf.Max(0, xpPerLevel[idx]);

            // fallback formula
            float val = BaseXP * Mathf.Pow(level, Exponent);
            return Mathf.Max(1, Mathf.FloorToInt(val));
        }

        /// <summary>
        /// Optional helper: get cumulative XP required to reach the given level from level 1 (exclusive).
        /// E.g. cumulative XP to reach level 3 = xp(level1->2) + xp(level2->3)
        /// </summary>
        public int GetCumulativeXpForLevel(int level)
        {
            if (level <= 1) return 0;
            int total = 0;
            for (int l = 1; l < level; l++)
                total += GetXpForLevel(l);
            return total;
        }
    }
}
