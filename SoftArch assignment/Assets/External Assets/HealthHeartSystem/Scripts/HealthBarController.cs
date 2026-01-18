using DungeonCrawler.Gameplay.Combat;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthBarController (rewritten to use AniDrag.CharacterComponents.HealthComponent)
/// - Attach to a HUD GameObject
/// - heartContainerPrefab must contain a child Image named "HeartFill"
/// - Assign a HealthComponent (usually the player's HealthComponent) in the inspector
/// </summary>
public class HealthBarController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("HealthComponent to display. Typically the player's HealthComponent.")]
    public Health healthComponent;

    [Tooltip("Parent transform where heart prefabs will be instantiated.")]
    public Transform heartsParent;

    [Tooltip("Prefab containing the heart visuals. It must include a child Image named 'HeartFill'.")]
    public GameObject heartContainerPrefab;

    [Header("Layout / Mapping")]
    [Tooltip("How many health points does one heart represent? E.g. 25 means each heart = 25 HP.")]
    public int healthPerHeart = 25;

    [Tooltip("Optional manual override for number of heart containers. If zero, calculated from maxHealth / healthPerHeart.")]
    public int fixedHeartCount = 0;

    // runtime lists (dynamic, safer than fixed arrays)
    private readonly List<GameObject> heartContainers = new List<GameObject>();
    private readonly List<Image> heartFills = new List<Image>();

    void Start()
    {
        if (heartContainerPrefab == null || heartsParent == null)
        {
            Debug.LogError("HealthBarController: heartContainerPrefab and heartsParent must be assigned.", this);
            enabled = false;
            return;
        }

        if (healthComponent == null)
        {
            // try to find player HealthComponent automatically
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                healthComponent = playerGo.GetComponent<Health>();
        }

        if (healthComponent == null)
        {
            Debug.LogError("HealthBarController: No HealthComponent assigned or found on a GameObject tagged 'Player'.", this);
            enabled = false;
            return;
        }

        if (healthPerHeart <= 0) healthPerHeart = 25; // safe default

        BuildHearts();
        // subscribe to health changes
        healthComponent.onHealthChanged += OnHealthChanged;

        // initial update
        UpdateHeartsHUD();
    }

    void OnDestroy()
    {
        if (healthComponent != null)
            healthComponent.onHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged(Health hc)
    {
        // ignore parameter, always refresh display from the referenced healthComponent
        UpdateHeartsHUD();
    }

    /// <summary>
    /// Main public method that updates both container visibility and fill amounts.
    /// </summary>
    public void UpdateHeartsHUD()
    {
        if (healthComponent == null) return;

        int heartCount = GetHeartCount();
        EnsureHeartCount(heartCount);
        SetHeartContainers(heartCount);
        SetFilledHearts(heartCount);
    }

    int GetHeartCount()
    {
        if (fixedHeartCount > 0) return fixedHeartCount;
        return Mathf.Max(1, Mathf.CeilToInt((float)healthComponent.GetMaxHP() / healthPerHeart));
    }

    void BuildHearts()
    {
        heartContainers.Clear();
        heartFills.Clear();

        int heartCount = GetHeartCount();

        for (int i = 0; i < heartCount; i++)
        {
            GameObject temp = Instantiate(heartContainerPrefab, heartsParent, false);
            heartContainers.Add(temp);

            var fill = temp.transform.Find("HeartFill")?.GetComponent<Image>();
            if (fill == null)
            {
                Debug.LogError($"HealthBarController: heart prefab must contain a child Image named 'HeartFill'. Missing on instance #{i}.", temp);
                fill = temp.GetComponentInChildren<Image>(); // best-effort fallback
            }
            heartFills.Add(fill);
        }
    }

    void EnsureHeartCount(int required)
    {
        // If we already have the required number, nothing to do
        if (heartContainers.Count == required) return;

        // If we have fewer, instantiate more
        if (heartContainers.Count < required)
        {
            int toCreate = required - heartContainers.Count;
            for (int i = 0; i < toCreate; i++)
            {
                GameObject temp = Instantiate(heartContainerPrefab, heartsParent, false);
                heartContainers.Add(temp);
                var fill = temp.transform.Find("HeartFill")?.GetComponent<Image>();
                if (fill == null) fill = temp.GetComponentInChildren<Image>();
                heartFills.Add(fill);
            }
        }
        else // we have more than required -> destroy extras
        {
            for (int i = heartContainers.Count - 1; i >= required; i--)
            {
                var go = heartContainers[i];
                heartContainers.RemoveAt(i);
                heartFills.RemoveAt(i);
                if (go != null) Destroy(go);
            }
        }
    }

    void SetHeartContainers(int visibleCount)
    {
        for (int i = 0; i < heartContainers.Count; i++)
        {
            heartContainers[i].SetActive(i < visibleCount);
        }
    }

    void SetFilledHearts(int heartCount)
    {
        // calculate fill amount per heart using currentHealth and healthPerHeart
        int current = healthComponent.GetCurrentHp();
        for (int i = 0; i < heartCount; i++)
        {
            float heartMin = i * healthPerHeart;
            float fill = Mathf.Clamp01((current - heartMin) / (float)healthPerHeart);

            // safety: if we don't have a fill image for this index, continue
            if (i >= heartFills.Count || heartFills[i] == null) continue;
            heartFills[i].fillAmount = fill;
        }
    }
}
