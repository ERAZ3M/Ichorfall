using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private PlayerStats playerStats;   // assign in inspector

    [Header("Settings")]
    [SerializeField] private int maxHearts = 5;          // e.g. number of heart slots
    [SerializeField] private int healthPerHeart = 2;    // 2 HP per full heart (half-heart = 1 HP)

    // Pre‑queried elements
    private VisualElement root;
    private List<VisualElement> heartSlots = new List<VisualElement>();

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        root = hudDocument.rootVisualElement;
        // Cache heart slots by their class (or by name if you used names)
        var slots = root.Query<VisualElement>(className: "heart-slot").ToList();
        heartSlots.AddRange(slots);

        // Subscribe to health changes on the player
        if (playerStats != null)
            playerStats.OnHealthChanged.AddListener(UpdateHearts);

        // Initial update
        UpdateHearts(playerStats.currentHealth, playerStats.maxHealth);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged.RemoveListener(UpdateHearts);
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Calculate how many hearts we should actually show
        int totalHP = maxHealth;
        int usedSlots = Mathf.Min(heartSlots.Count, Mathf.CeilToInt(totalHP / (float)healthPerHeart));

        // For each heart slot, determine its state
        for (int i = 0; i < heartSlots.Count; i++)
        {
            if (i >= usedSlots)
            {
                // Hide extra slots not used by max health
                heartSlots[i].style.display = DisplayStyle.None;
                continue;
            }
            else
            {
                heartSlots[i].style.display = DisplayStyle.Flex;
            }

            // Health left for this heart index (0‑based)
            int heartHP = currentHealth - i * healthPerHeart;
            string state;
            if (heartHP >= healthPerHeart)
                state = "full";
            else if (heartHP <= 0)
                state = "empty";
            else
                state = "half";   // hearts that have exactly 1 point remaining

            // Apply the state
            SetHeartState(heartSlots[i], state);
        }
    }

    private void SetHeartState(VisualElement slot, string state)
    {
        // Grab the three child elements
        var full = slot.Q<VisualElement>(className: "heart-full");
        var half = slot.Q<VisualElement>(className: "heart-half");
        var empty = slot.Q<VisualElement>(className: "heart-empty");

        // Set opacity directly (the USS transition handles the fade)
        full.style.opacity = (state == "full") ? 1f : 0f;
        half.style.opacity = (state == "half") ? 1f : 0f;
        empty.style.opacity = (state == "empty") ? 1f : 0f;
    }
}