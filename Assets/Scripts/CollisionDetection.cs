using System;
using UnityEngine;
using System.Collections.Generic;

public class CollisionDetection : MonoBehaviour
{

    [SerializeField] private WeaponController wc;
    [SerializeField] private GameObject hitParticles;

    private HashSet<Collider> enemiesHit = new HashSet<Collider>(); // To track the enemies already hit
    
    void OnTriggerEnter(Collider other)
    {
        bool anyAttack = wc.isAttacking || wc.isLunging;
        if (other.CompareTag("Enemy") && anyAttack &&  !enemiesHit.Contains(other))
        {
            
            enemiesHit.Add(other); // Mark enemy as hit
            
            CharacterStats stats = other.GetComponent<CharacterStats>();
            if (stats != null) {
                stats.TakeDamage(wc.Damage);
                Debug.Log("Hit" +  other.name);
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
        if (!wc.isAttacking && !wc.isLunging)
        {
            enemiesHit.Clear();
        }
    }
}
