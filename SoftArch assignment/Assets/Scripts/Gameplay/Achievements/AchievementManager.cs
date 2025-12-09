using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public void DisplayAchievement(string achievementID)
    {
        ServiceLocator.GetService<AchievementService>().DisplayAchievement(achievementID);
    }
}
