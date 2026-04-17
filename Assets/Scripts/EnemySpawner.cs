using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyParent;

    private GameObject[] spawnPoints;

    void Start()
    {
        CacheSpawnPoints();
        SpawnEnemies(); // Initial spawn on game start
    }

    // Cache spawn points to avoid Find calls every respawn
    private void CacheSpawnPoints()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
        if (spawnPoints.Length == 0)
            Debug.LogWarning("EnemySpawner: No spawn points found with tag 'EnemySpawnPoint'.");
    }

    // Public method so GameManager can trigger respawn
    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: No enemy prefab assigned!");
            return;
        }

        // If spawn points weren't cached (e.g., called before Start), do it now
        if (spawnPoints == null || spawnPoints.Length == 0)
            CacheSpawnPoints();

        foreach (GameObject spawnPoint in spawnPoints)
        {
            Transform spawnTransform = spawnPoint.transform;
            GameObject newEnemy = Instantiate(enemyPrefab, spawnTransform.position, spawnTransform.rotation);

            if (enemyParent != null)
                newEnemy.transform.parent = enemyParent;
        }

        Debug.Log($"Spawned {spawnPoints.Length} enemies.");
    }
}