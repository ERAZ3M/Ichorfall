using UnityEngine;
using UnityEngine.SceneManagement; // Optional: if you want to reload the whole scene

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private EnemySpawner enemySpawner;

    private Transform respawnPoint;
    private CharacterStats playerStats;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find respawn point by tag
        GameObject respawnObject = GameObject.FindGameObjectWithTag("Respawn");
        if (respawnObject != null)
            respawnPoint = respawnObject.transform;
        else
            Debug.LogError("GameManager: No GameObject with tag 'Respawn' found!");

        // Get player stats to restore health later
        if (player != null)
            playerStats = player.GetComponent<CharacterStats>();
    }

    // Called by the player's health script when health reaches 0
    public void OnPlayerDied()
    {
        Debug.Log("Player died. Respawning...");
        ResetEnemies();
        RespawnPlayer();
    }

    private void ResetEnemies()
    {
        // 1. Destroy all existing enemies in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        // 2. Tell the spawner to create new enemies
        if (enemySpawner != null)
        {
            enemySpawner.SpawnEnemies();
        }
        else
        {
            Debug.LogError("GameManager: EnemySpawner reference is missing!");
        }
    }

    private void RespawnPlayer()
    {
        if (player == null || respawnPoint == null) return;

        // Move player to respawn point
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;

        // Restore full health
        if (playerStats != null)
        {
            playerStats.currentHealth = playerStats.maxHealth;
        }

        // Optional: Reset player velocity if using Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Optional: Full scene reload alternative (uncomment and use instead of ResetEnemies/RespawnPlayer)
    // private void ReloadScene()
    // {
    //     SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    // }
}