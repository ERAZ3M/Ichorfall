using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class WinScreenController : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private UIDocument winDocument;

    private VisualElement root;
    private VisualElement winContainer;
    private Button mainMenuButton;
    private Button quitButton;

    private bool isWinScreenActive = false;
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;
    private WeaponController weaponController;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (winDocument == null)
        {
            Debug.LogError("No UIDocument assigned");
            return;
        }

        root = winDocument.rootVisualElement;

        // Try to find by name first, fallback to class
        winContainer = root.Q<VisualElement>("WinScreenContainer");
        if (winContainer == null)
            winContainer = root.Q<VisualElement>(className: "win-container");

        if (winContainer == null)
        {
            Debug.LogError("WinScreenController: Could not find container element 'WinScreenContainer' or class 'win-container'");
            return;
        }

        mainMenuButton = root.Q<Button>("MainMenuButton");
        quitButton = root.Q<Button>("QuitButton");

        if (mainMenuButton == null) Debug.LogError("MainMenuButton not found");
        if (quitButton == null) Debug.LogError("QuitButton not found");

        mainMenuButton.clicked += GoToMainMenu;
        quitButton.clicked += QuitGame;

        // Start hidden
        winContainer.style.display = DisplayStyle.None;
    }

    private void Start()
    {
        // Cache player components for input disabling
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerActionMap = playerInput.actions.FindActionMap("Player");

            weaponController = player.GetComponent<WeaponController>();
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    public void ShowWinScreen()
    {
        if (isWinScreenActive) return;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Pause game
        Time.timeScale = 0f;

        // Disable player controls
        if (playerActionMap != null)
            playerActionMap.Disable();

        if (weaponController != null)
            weaponController.enabled = false;

        if (playerMovement != null)
            playerMovement.SetControlsEnabled(false);

        // Show UI
        if (winContainer != null)
            winContainer.style.display = DisplayStyle.Flex;
        
        isWinScreenActive = true;
        Debug.Log("Win screen shown");
    }

    private void GoToMainMenu()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (mainMenuButton != null)
            mainMenuButton.clicked -= GoToMainMenu;
        if (quitButton != null)
            quitButton.clicked -= QuitGame;
    }
}