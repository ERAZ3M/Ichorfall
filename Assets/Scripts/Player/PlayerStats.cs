using UnityEngine;

public class PlayerStats : CharacterStats
{
    public bool IsDead { get; private set; } = false;

    public override void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Tell the GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied();
        else
            Debug.LogError("GameManager.Instance not found!");
    }

    // Optional: override TakeDamage to ignore damage when dead
    // (if CharacterStats.TakeDamage is virtual)
    public override void TakeDamage(int damage)
    {
        if (IsDead) return;
        base.TakeDamage(damage);
    }
}