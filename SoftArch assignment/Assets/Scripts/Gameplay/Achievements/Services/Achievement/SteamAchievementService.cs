using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SteamAchievementSystem : AchievementService
{
    public override void DisplayAchievement(string achievementID)
    {
        achievmentText.text = "You've achieved the achievement " + achievementID + " in Steam";
    }
}
