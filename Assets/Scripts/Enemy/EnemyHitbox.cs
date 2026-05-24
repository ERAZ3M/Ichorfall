using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    private EnemyAI enemyAI;

    void Awake()
    {
        // Grab the EnemyAI from the root enemy object (parent or grandparent)
        enemyAI = GetComponentInParent<EnemyAI>();
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (enemyAI == null) return;

        CharacterStats playerStats = other.GetComponent<CharacterStats>();
        if (playerStats != null)
        {
            // All hitboxes call the same centralized cooldown method
            enemyAI.TryDealContactDamage(playerStats);
        }
    }
}