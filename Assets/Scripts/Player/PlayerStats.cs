using UnityEngine;

public class PlayerStats : CharacterStats
{
    public bool IsDead { get; private set; } = false;
    private DissolveController dissolveController;

    private void Start()
    {
        // Search for DissolveController on this object OR any child
        dissolveController = GetComponentInChildren<DissolveController>();
        if (dissolveController == null)
            Debug.LogWarning("PlayerStats: No DissolveController found on " + gameObject.name + " or its children.");
        else
            Debug.Log("PlayerStats: Found DissolveController on " + dissolveController.gameObject.name);
    }

    public override void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Disable player controls
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
            // Fallback – no dissolve, just die
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDied();
            else
                Debug.LogError("GameManager.Instance not found!");
        }
    }

    private void DisablePlayerControls()
    {
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null) pm.SetControlsEnabled(false);

        WeaponController wc = GetComponent<WeaponController>();
        if (wc != null) wc.enabled = false;

        UnityEngine.InputSystem.PlayerInput pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = false;
    }

    public override void TakeDamage(int damage)
    {
        if (IsDead) return;
        base.TakeDamage(damage);
    }
}