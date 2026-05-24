using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private Button playButton;
    private Button optionsButton;
    private Button quitButton;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        playButton = root.Q<Button>("PlayButton");
        optionsButton = root.Q<Button>("OptionsButton");
        quitButton = root.Q<Button>("QuitButton");
        
        playButton.clicked += OnPlayClicked;
        optionsButton.clicked += OnOptionsClicked;
        quitButton.clicked += OnQuitClicked;
    }
    
    void OnDisable()
    {
        playButton.clicked -= OnPlayClicked;
        optionsButton.clicked -= OnOptionsClicked;
        quitButton.clicked -= OnQuitClicked;
    }
    
    void OnPlayClicked()
    {
        // Load your game scene – replace "GameScene" with actual scene name
        SceneManager.LoadScene("GameScene");
    }
    
    void OnOptionsClicked()
    {
        Debug.Log("Options menu – not implemented yet.");
        // You can expand later
    }
    
    void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}