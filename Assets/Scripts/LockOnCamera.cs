using UnityEngine;
using Unity.Cinemachine;

public class LockOnCamera : MonoBehaviour
{
   
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CinemachineCamera cam;
    
    [Header("Lock On Settings")]
    [SerializeField] private float lockOnRange = 10f;
    
    private Transform currentTarget;
    private bool isLockedOn = false;
    

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!isLockedOn)
                TryLockOn();
            else
                StopLockOn();
        }
        
        // If we're locked on, keep camera looking at target
        if (isLockedOn && currentTarget != null)
        {

            if (cam != null)
            {
                cam.LookAt = currentTarget;
                cam.Follow = transform;

            }
            
            Debug.Log("Locking on " + currentTarget.name);
            Vector3 direction = currentTarget.position - transform.position;
            direction.y = 0; // Don't tilt up/down
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            }
        }
    }
    
    void TryLockOn()
    {
        // Find all enemies in the scene
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (allEnemies.Length == 0)
        {
            Debug.Log("No enemies found!");
            return;
        }
        
        // Find the closest enemy
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in allEnemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            
            if (distance < lockOnRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        
        // Lock onto the closest enemy
        if (closestEnemy != null)
        {
            currentTarget = closestEnemy;
            isLockedOn = true;
            Debug.Log("Locked onto: " + currentTarget.name);
            
        }
    }
    
    void StopLockOn()
    {
        
        currentTarget = null;
        isLockedOn = false;

        if (cam != null)
        {
            cam.LookAt = transform;
        }
        
        Debug.Log("Lock released");
    }
    
    // Visualize lock-on range in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
    }
}