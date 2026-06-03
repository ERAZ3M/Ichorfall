using UnityEngine;

public class PlayerStats : CharacterStats
{
    public bool IsDead { get; private set; } = false;
    private DissolveController dissolveController;

    private void Start()
    {
        // Find DissolveController on this object or any child
        dissolveController = GetComponentInChildren<DissolveController>();
        if (dissolveController == null)
            Debug.LogWarning("PlayerStats: No DissolveController found on player or its children.");
    }

    // Normal death (health reaches 0)
    public override void Die()
    {
        if (IsDead) return;
        IsDead = true;

        DisablePlayerControls();

        if (dissolveController != null)
        {
            dissolveController.StartDissolve(() =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OnPlayerDied();
                else
                    Debug.LogError("GameManager.Instance not found!");
            });
        }
        else
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDied();
            else
                Debug.LogError("GameManager.Instance not found!");
        }
    }

    // Lava death – lose 1 life and respawn at checkpoint
    public void DieByLava()
    {
        if (IsDead) return;
        IsDead = true;

        DisablePlayerControls();

        if (dissolveController != null)
        {
            dissolveController.StartDissolve(() =>
            {
                // Reduce health by 1 (half heart)
                currentHealth = Mathf.Max(0, currentHealth - 1);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);

                if (currentHealth <= 0)
                {
                    // No lives left, full death
                    GameManager.Instance.OnPlayerDied();
                }
                else
                {
                    // Respawn at checkpoint
                    GameManager.Instance.RespawnPlayer();
                    IsDead = false; // player is alive again
                }
            });
        }
        else
        {
            // Fallback without dissolve
            currentHealth = Mathf.Max(0, currentHealth - 1);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
                GameManager.Instance.OnPlayerDied();
            else
                GameManager.Instance.RespawnPlayer();

            IsDead = false;
        }
    }

    // Helper to refresh UI after respawn (called automatically by OnHealthChanged)
    public void RefreshHealthUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Disable movement, attacking, input
    private void DisablePlayerControls()
    {
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.SetControlsEnabled(false);

        WeaponController wc = GetComponent<WeaponController>();
        if (wc != null) wc.enabled = false;

        UnityEngine.InputSystem.PlayerInput pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = false;
    }

    // Prevent taking damage after death
    public override void TakeDamage(int damage)
    {
        if (IsDead) return;
        base.TakeDamage(damage);
    }
}