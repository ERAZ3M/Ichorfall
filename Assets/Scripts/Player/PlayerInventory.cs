using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerInventory : MonoBehaviour
{
    [Header("Starting Unlocks")]
    [SerializeField] private List<AbilityData> startingAbilities;

    private List<AbilityData> unlockedAbilities = new List<AbilityData>();

    public UnityEvent<AbilityData> OnAbilityUnlocked;

    private void Awake()
    {
        // Start with any abilities you want the player to have from the beginning
        foreach (var ability in startingAbilities)
        {
            if (ability != null && !unlockedAbilities.Contains(ability))
                unlockedAbilities.Add(ability);
        }
    }

    public bool HasAbility(AbilityData ability)
    {
        if (ability == null) return false;
        return unlockedAbilities.Contains(ability);
    }

    public void UnlockAbility(AbilityData ability)
    {
        if (ability == null) return;

        if (!unlockedAbilities.Contains(ability))
        {
            unlockedAbilities.Add(ability);
            Debug.Log($"Unlocked ability: {ability.abilityName}");
            OnAbilityUnlocked?.Invoke(ability);
        }
    }
}