using UnityEngine;

public class CharacterStats : MonoBehaviour
{

    public int maxHealth;
    public int currentHealth { get; set; } 
    
    public int damage;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(transform.name + " takes " + damage + " damage.");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    

    public virtual void Die()
    {
        Debug.Log(transform.name + " died.");

        // Placeholder for death animation trigger
        // e.g., GetComponent<Animator>().SetTrigger("Die");

        if (CompareTag("Player"))
        {
            GameManager.Instance.OnPlayerDied();
        } else {
        
        // Disable collider so no further hits can register
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // Destroy after a short delay (allows particles/animations to play)
        Destroy(gameObject, 2f);
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

}
