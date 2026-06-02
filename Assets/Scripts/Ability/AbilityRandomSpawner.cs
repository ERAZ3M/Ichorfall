using UnityEngine;

public class AbilityRandomSpawner : MonoBehaviour
{
    [Header("Lunge Settings")]
    [SerializeField] private GameObject lungePickupPrefab;   // Prefab with AbilityUnlock (Lunge)
    [SerializeField] private Transform[] lungeSpawnPoints;   // Markers for possible Lunge spawn locations

    [Header("Dash Settings")]
    [SerializeField] private GameObject dashPickupPrefab;    // Prefab with AbilityUnlock (Dash)
    [SerializeField] private Transform[] dashSpawnPoints;    // Markers for possible Dash spawn locations

    private void Start()
    {
        SpawnAbility(lungePickupPrefab, lungeSpawnPoints);
        SpawnAbility(dashPickupPrefab, dashSpawnPoints);
    }

    private void SpawnAbility(GameObject prefab, Transform[] spawnPoints)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"AbilityRandomSpawner: Prefab is null, skipping.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"AbilityRandomSpawner: No spawn points assigned for {prefab.name}, skipping.");
            return;
        }

        // Pick one random spawn point
        int index = Random.Range(0, spawnPoints.Length);
        Transform chosen = spawnPoints[index];

        // Instantiate the pickup at the chosen point
        Instantiate(prefab, chosen.position, chosen.rotation);

        // Destroy all spawn point markers (including the chosen one)
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                Destroy(point.gameObject);
        }
    }
}