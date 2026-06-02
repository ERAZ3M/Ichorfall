using UnityEngine;

public class WinZone : MonoBehaviour
{
    [SerializeField] private WinScreenController winScreenController;

    private void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;

        // Find the controller if not assigned
        if (winScreenController == null)
            winScreenController = FindObjectOfType<WinScreenController>();

        if (winScreenController != null)
            winScreenController.ShowWinScreen();
        else
            Debug.LogError("WinScreenController not found in scene!");

        // Prevent multiple triggers
        GetComponent<Collider>().enabled = false;
    }
}