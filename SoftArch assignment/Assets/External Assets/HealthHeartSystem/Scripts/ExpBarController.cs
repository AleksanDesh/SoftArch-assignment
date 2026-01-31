using DungeonCrawler.Gameplay.Stats;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ExpBarController
/// - Attach to a HUD GameObject
/// - ExpContainerPrefab must contain a child Image named "ExpFill"
/// - Assign an ActorStats in the inspector (usually the player's ActorStats)
/// </summary>
public class ExpBarController : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI ExpText;

    [Tooltip("ActorStats to display. Typically the player's ActorStats.")]
    public ActorStats ActorComponent;

    [Tooltip("Parent transform where orb prefabs will be instantiated.")]
    public Transform ExpParent;

    [Tooltip("Prefab containing the orb visuals. It must include a child Image named 'ExpFill'.")]
    public GameObject ExpContainerPrefab;

    [Header("Layout / Mapping")]
    [Tooltip("How much XP does one orb represent?")]
    public int ExpPerOrb = 25;

    [Tooltip("Optional manual override for number of orb containers. If zero, calculated from required XP / ExpPerOrb.")]
    public int FixedOrbCount = 0;

    [Tooltip("When true, each orb will be filled as a percentage of XP required for the next level (sequentially: first full, next partial, others empty).")]
    public bool UsePercentFill = false;

    // runtime lists
    private readonly List<GameObject> orbContainers = new List<GameObject>();
    private readonly List<Image> orbFills = new List<Image>();

    void Start()
    {
        if (ExpContainerPrefab == null || ExpParent == null)
        {
            Debug.LogError("ExpBarController: ExpContainerPrefab and ExpParent must be assigned.", this);
            enabled = false;
            return;
        }

        if (ActorComponent == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                ActorComponent = playerGo.GetComponent<ActorStats>();
        }

        if (ActorComponent == null)
        {
            Debug.LogError("ExpBarController: No ActorStats assigned or found on a GameObject tagged 'Player'.", this);
            enabled = false;
            return;
        }

        if (ExpPerOrb <= 0) ExpPerOrb = 25; // safe default

        BuildOrbs();
        ActorComponent.onExpChanged += OnExpChanged;
        UpdateOrbsHUD();
    }

    void OnDestroy()
    {
        if (ActorComponent != null)
            ActorComponent.onExpChanged -= OnExpChanged;
    }

    void OnExpChanged(ActorStats stats)
    {
        UpdateOrbsHUD();
    }

    public void UpdateOrbsHUD()
    {
        if (ActorComponent == null) return;

        int orbCount = GetOrbCount();
        EnsureOrbCount(orbCount);
        SetOrbContainers(orbCount);
        SetFilledOrbs(orbCount);

        if (ExpText != null)
            ExpText.text = ActorComponent.GetLevel.ToString();
    }

    int GetOrbCount()
    {
        if (FixedOrbCount > 0) return FixedOrbCount;
        return Mathf.Max(1, Mathf.CeilToInt((float)ActorComponent.GetRequiredExpForNextLevel() / ExpPerOrb));
    }

    void BuildOrbs()
    {
        orbContainers.Clear();
        orbFills.Clear();

        int orbCount = GetOrbCount();

        for (int i = 0; i < orbCount; i++)
        {
            GameObject temp = Instantiate(ExpContainerPrefab, ExpParent, false);
            orbContainers.Add(temp);

            var fill = temp.transform.Find("ExpFill")?.GetComponent<Image>();
            if (fill == null)
            {
                // fallback: search children (including inactive)
                fill = temp.GetComponentInChildren<Image>(true);
            }

            if (fill == null)
            {
                Debug.LogError($"ExpBarController: prefab must contain a child Image named 'ExpFill'. Missing on instance #{i}.", temp);
            }
            else
            {
                if (fill.type != Image.Type.Filled) fill.type = Image.Type.Filled;
            }

            orbFills.Add(fill);
        }
    }

    void EnsureOrbCount(int required)
    {
        if (orbContainers.Count == required) return;

        if (orbContainers.Count < required)
        {
            int toCreate = required - orbContainers.Count;
            for (int i = 0; i < toCreate; i++)
            {
                GameObject temp = Instantiate(ExpContainerPrefab, ExpParent, false);
                orbContainers.Add(temp);

                var fill = temp.transform.Find("ExpFill")?.GetComponent<Image>();
                if (fill == null) fill = temp.GetComponentInChildren<Image>(true);
                if (fill != null && fill.type != Image.Type.Filled) fill.type = Image.Type.Filled;
                orbFills.Add(fill);
            }
        }
        else
        {
            for (int i = orbContainers.Count - 1; i >= required; i--)
            {
                var go = orbContainers[i];
                orbContainers.RemoveAt(i);
                orbFills.RemoveAt(i);
                if (go != null) Destroy(go);
            }
        }
    }

    void SetOrbContainers(int visibleCount)
    {
        for (int i = 0; i < orbContainers.Count; i++)
        {
            orbContainers[i].SetActive(i < visibleCount);
        }
    }

    void SetFilledOrbs(int orbCount)
    {
        // Use XP inside the current level so progress resets on level-up
        int totalXp = ActorComponent.GetCurrentXp;
        int req = ActorComponent.GetRequiredExpForNextLevel();
        int currentInLevel = (req > 0) ? (totalXp % req) : totalXp;

        if (UsePercentFill)
        {
            if (req <= 0 || orbCount <= 0) return;

            // divide the total required XP evenly across the visible orbs,
            // then fill them sequentially using currentInLevel.
            float capacityPerOrb = (float)req / orbCount;

            for (int i = 0; i < orbCount; i++)
            {
                if (i >= orbFills.Count || orbFills[i] == null) continue;

                float orbMin = i * capacityPerOrb;
                float fill = Mathf.Clamp01((currentInLevel - orbMin) / capacityPerOrb);
                orbFills[i].fillAmount = fill;
            }
            return;
        }

        // Default chunked behavior (each orb represents ExpPerOrb)
        int current = currentInLevel;
        for (int i = 0; i < orbCount; i++)
        {
            float orbMin = i * ExpPerOrb;
            float fill = Mathf.Clamp01((current - orbMin) / (float)ExpPerOrb);

            if (i >= orbFills.Count || orbFills[i] == null) continue;
            orbFills[i].fillAmount = fill;
        }
    }
}
