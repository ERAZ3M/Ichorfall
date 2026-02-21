using UnityEngine;
using Unity.Cinemachine;

public class LockOnCamera : MonoBehaviour
{
   
    [Header("References")]
    [SerializeField] private CinemachineCamera freeLookCamera;
    [SerializeField] private CinemachineCamera lockOnCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    
    [Header("Lock On Settings")]
    [SerializeField] private float lockOnRange = 15f;
    [SerializeField] private LayerMask enemyLayer;
    
    private Transform currentTarget;
    private bool isLockedOn = false;

    

    void Start()
    {
        freeLookCamera.Priority = 20;
        lockOnCamera.Priority = 0;
        
        GameObject groupObj = new GameObject();
        targetGroup = groupObj.AddComponent<CinemachineTargetGroup>();
        targetGroup.AddMember(transform, 1f, 2f);
    }
    
    void Update()
    {

        if (Input.GetMouseButtonDown(2)) // Middle Mouse
        {
            if (isLockedOn)
                Unlock();
            else
                TryLockOn();
        }
        
        
    }
    
    void TryLockOn()
    {
        
        Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);
        if (hits.Length == 0)
            return;
        
        // Find closest enemy
        Transform closestEnemy = null;
        float closestDist = Mathf.Infinity;
        
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = hit.transform;
            }
        }
        
        currentTarget = closestEnemy;
        targetGroup.AddMember(currentTarget, 1f, 2f);
        
        lockOnCamera.LookAt = targetGroup.transform;
        lockOnCamera.Priority = 20;
        freeLookCamera.Priority = 10;

        isLockedOn = true;
        
        playerMovement.isLockedOn = true;
        playerMovement.lockOnTarget = currentTarget;
        
    }
    
    void Unlock()
    {
        
        freeLookCamera.ForceCameraPosition(
            lockOnCamera.transform.position,
            lockOnCamera.transform.rotation
            );
        
        lockOnCamera.Priority = 0;
        freeLookCamera.Priority = 20;
        
        lockOnCamera.LookAt = null;
        targetGroup.RemoveMember(currentTarget);
        currentTarget = null;
        
        isLockedOn = false;
        
        playerMovement.isLockedOn = false;
        playerMovement.lockOnTarget = null;
        
    }
    
    // Visualize lock-on range in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);
    }
}