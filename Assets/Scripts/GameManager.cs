using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private FadeController fadeController;

    private Transform respawnPoint;
    private CharacterStats playerStats;
    private bool isRespawning = false;

    // For input disabling
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find respawn point
        GameObject respawnObject = GameObject.FindGameObjectWithTag("Respawn");
        if (respawnObject != null)
            respawnPoint = respawnObject.transform;
        else
            Debug.LogError("GameManager: No Respawn point found!");

        if (player != null)
        {
            playerStats = player.GetComponent<CharacterStats>();
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerActionMap = playerInput.actions.FindActionMap("Player");
        }

        if (fadeController == null)
            fadeController = FindObjectOfType<FadeController>();
    }

    public void OnPlayerDied()
    {
        if (isRespawning) return;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isRespawning = true;

        // 1. Disable gameplay input (keep UI input alive)
        if (playerActionMap != null)
            playerActionMap.Disable();

        // Disable combat script and movement input (keep PlayerMovement alive for gravity!)
        WeaponController wc = player.GetComponent<WeaponController>();
        if (wc != null) wc.enabled = false;

        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null) pm.SetControlsEnabled(false);   // disables input, but gravity keeps working

        // 2. Death animation placeholder (3 seconds realtime)
        yield return new WaitForSecondsRealtime(3f);

        // 3. Fade out, reset world, fade in
        yield return StartCoroutine(fadeController.FadeOutIn(1f, 0.2f, 1f, () =>
        {
            // Destroy all enemies
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
                Destroy(enemy);

            // Respawn enemies via spawner
            if (enemySpawner != null)
                enemySpawner.SpawnEnemies();
            else
                Debug.LogError("EnemySpawner missing!");

            // Teleport player & restore health
            if (player != null && respawnPoint != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = respawnPoint.position;
                player.transform.rotation = respawnPoint.rotation;
                if (cc != null) cc.enabled = true;

                if (pm != null) pm.ResetVelocity();
                if (playerStats != null)
                    playerStats.ResetHealth();
            }
        }));

        // 4. Re-enable scripts and input
        if (wc != null) wc.enabled = true;
        if (pm != null)
        {
            pm.SetControlsEnabled(true);
            pm.ResetVelocity();   // optional, but safe
        }
        if (playerActionMap != null)
            playerActionMap.Enable();

        isRespawning = false;
    }
}