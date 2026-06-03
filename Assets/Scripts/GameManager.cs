using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private FadeController fadeController;

    private CharacterStats playerStats;
    private bool isRespawning = false;
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;

    // Checkpoint system
    private Vector3 lastCheckpointPosition;
    private bool hasCheckpoint = false;

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
        if (player != null)
        {
            playerStats = player.GetComponent<CharacterStats>();
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerActionMap = playerInput.actions.FindActionMap("Player");
        }

        if (fadeController == null)
            fadeController = FindObjectOfType<FadeController>();

        if (fadeController != null)
            fadeController.FadeIn(1f);
    }

    public void OnPlayerDied()
    {
        if (isRespawning) return;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isRespawning = true;

        if (playerActionMap != null)
            playerActionMap.Disable();

        yield return new WaitForSecondsRealtime(0.3f);

        if (fadeController != null)
        {
            fadeController.FadeOut(0.8f, () =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // No need to reset isRespawning because scene reloads.
        yield break;
    }

    public void SetCheckpoint(Vector3 position)
    {
        lastCheckpointPosition = position;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint set at {position}");
    }

    public void RespawnPlayer()
    {
        if (isRespawning) return;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;

        // Disable movement during respawn
        PlayerMovement pm = player.GetComponent<PlayerMovement>();
        if (pm != null) pm.SetControlsEnabled(false);

        // Fallback checkpoint
        if (!hasCheckpoint)
        {
            lastCheckpointPosition = player.transform.position;
            hasCheckpoint = true;
        }

        // Teleport
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = lastCheckpointPosition;
        if (controller != null) controller.enabled = true;

        // Reset velocity
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // Reset dissolve effect
        DissolveController dissolve = player.GetComponentInChildren<DissolveController>();
        if (dissolve != null) dissolve.ResetDissolve();

        // Fade in (short wait for visual smoothness)
        if (fadeController != null)
            fadeController.FadeIn(0.5f);

        yield return new WaitForSecondsRealtime(0.1f); // brief pause before re-enabling controls

        // Re-enable controls
        if (pm != null) pm.SetControlsEnabled(true);
        if (playerActionMap != null) playerActionMap.Enable();

        isRespawning = false;
        yield break;
    }
}