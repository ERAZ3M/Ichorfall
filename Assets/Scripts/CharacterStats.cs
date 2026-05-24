using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CharacterStats : MonoBehaviour
{
    public int maxHealth;
    public int currentHealth { get; set; }
    public int damage;

    // Event: currentHealth, maxHealth
    public UnityEvent<int, int> OnHealthChanged;

    [Header("Hit Freeze (Player only)")]
    [SerializeField] private float hitFreezeDuration = 0.05f; // about 1-3 frames at 60fps
    private bool isHitFrozen = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // Only apply hit freeze to the player, and only if not already frozen
        if (CompareTag("Player") && !isHitFrozen && damage > 0)
        {
            StartCoroutine(HitFreezeRoutine());
        }

        currentHealth -= damage;
        Debug.Log(transform.name + " takes " + damage + " damage.");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator HitFreezeRoutine()
    {
        isHitFrozen = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Use unscaled time to wait for the freeze duration
        yield return new WaitForSecondsRealtime(hitFreezeDuration);

        Time.timeScale = originalTimeScale;
        isHitFrozen = false;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public virtual void Die()
    {
        Debug.Log(transform.name + " died.");

        if (CompareTag("Player"))
        {
            GameManager.Instance.OnPlayerDied();
        }
        else if (CompareTag("Enemy"))
        {
            EnemyAI enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.Die();
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Destroy(gameObject, 2f);
            }
        }
    }
}