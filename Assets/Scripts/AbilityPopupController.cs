using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class AbilityPopupController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument abilityPopUpDocument;   // Drag your UIDocument here

    private VisualElement container;
    private Image abilityIcon;
    private Label abilityNameLabel;
    private Label abilityDescriptionLabel;
    private Label dismissPrompt;

    private bool isVisible = false;

    private void Awake()
    {
        if (abilityPopUpDocument == null)
        {
            Debug.LogError("AbilityPopupController: No UIDocument assigned!");
            return;
        }

        // Get the elements – DO NOT disable the GameObject.
        var root = abilityPopUpDocument.rootVisualElement;
        container = root.Q<VisualElement>("AbilityPopupContainer");
        abilityIcon = root.Q<Image>("AbilityIcon");
        abilityNameLabel = root.Q<Label>("AbilityName");
        abilityDescriptionLabel = root.Q<Label>("AbilityDescription");
        dismissPrompt = root.Q<Label>("DismissPrompt");

        // Hide the container only – the UIDocument stays active.
        container.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (!isVisible) return;

        // Dismiss with any keyboard key or left mouse click (works at timeScale 0)
        if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
        {
            HidePopup();
        }
    }

    public void ShowPopup(AbilityData ability)
    {
        if (ability == null)
        {
            Debug.LogError("ShowPopup called with null ability!");
            return;
        }

        // Fill UI data – references are still valid because we never rebuilt the tree
        abilityIcon.sprite = ability.icon;                 // use sprite, not .texture
        abilityNameLabel.text = ability.abilityName;
        abilityDescriptionLabel.text = ability.description;

        // Pause game
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Show the container
        container.style.display = DisplayStyle.Flex;
        isVisible = true;
    }

    private void HidePopup()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        container.style.display = DisplayStyle.None;
        isVisible = false;
    }

    private void OnDestroy()
    {
        if (isVisible) HidePopup();
    }
}