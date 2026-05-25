using UnityEngine;

public class PlayerStats : CharacterStats
{
    public override void Die()
    {
        // Just tell the GameManager. No extra UI or coroutine.
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerDied();
        else
            Debug.LogError("GameManager.Instance not found!");
    }
}