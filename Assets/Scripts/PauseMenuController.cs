using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private UIDocument pauseDocument;
    [SerializeField] private InputActionReference pauseAction; // Reference to Global/Pause action

    private VisualElement root;
    private VisualElement pauseContainer;
    private Button resumeButton;
    private Button optionsButton;
    private Button mainMenuButton;
    private Button quitButton;

    private bool isPaused = false;
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;

    private void Awake()
    {
        root = pauseDocument.rootVisualElement;
        pauseContainer = root.Q<VisualElement>("PauseMenuContainer");

        resumeButton   = root.Q<Button>("ResumeButton");
        optionsButton  = root.Q<Button>("OptionsButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        quitButton     = root.Q<Button>("QuitButton");

        resumeButton.clicked   += ResumeGame;
        optionsButton.clicked  += OnOptionsClicked;
        mainMenuButton.clicked += GoToMainMenu;
        quitButton.clicked     += QuitGame;
    }

    private void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            playerActionMap = playerInput.actions.FindActionMap("Player"); // the gameplay map
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
        pauseContainer.style.display = DisplayStyle.Flex;

        // Mute all audio
        AudioListener.pause = true;

        // Disable gameplay input so player can’t move/attack while paused
        if (playerActionMap != null)
            playerActionMap.Disable();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        pauseContainer.style.display = DisplayStyle.None;

        // Unmute audio
        AudioListener.pause = false;

        // Re-enable gameplay input
        if (playerActionMap != null)
            playerActionMap.Enable();
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnOptionsClicked()
    {
        Debug.Log("Options – not implemented yet.");
    }

    private void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}