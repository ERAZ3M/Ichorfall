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
        if (player != null)
        {
            playerStats = player.GetComponent<CharacterStats>();
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerActionMap = playerInput.actions.FindActionMap("Player");
        }

        if (fadeController == null)
            fadeController = FindObjectOfType<FadeController>();

        // Fade in from black when the scene starts (including after death reload)
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

        yield return new WaitForSecondsRealtime(0.5f);

        // Fade out, then reload
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
    }
}