using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public abstract class AchievementService : MonoBehaviour
{
    [SerializeField]
    protected TextMeshProUGUI achievmentText;
    public abstract void DisplayAchievement(string achievementID);
}
