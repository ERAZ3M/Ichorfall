using UnityEngine;
using System.Collections.Generic;

public class CollisionDetection : MonoBehaviour
{

    [SerializeField] private WeaponController wc;
    [SerializeField] private GameObject hitParticles;

    private HashSet<Collider> enemiesHit = new HashSet<Collider>(); // To track the enemies already hit
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && wc.isAttacking &&  !enemiesHit.Contains(other))
        {
            
            enemiesHit.Add(other); // Mark enemy as hit
            
            CharacterStats stats = other.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(wc.Damage);
            }

            EnemyAI enemyAI = other.GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.TakeHit(transform.position);

            //other.GetComponent<Animator>().SetTrigger("Hit");
            /*Instantiate(hitParticles, new Vector3(
                    other.transform.position.x,
                    transform.position.y,
                    other.transform.position.z),
                other.transform.rotation);
            */

        }
    }
    

    void Update()
    {
        if (!wc.isAttacking)
        {
            enemiesHit.Clear();
        }
    }
}
