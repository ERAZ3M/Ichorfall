using UnityEngine;

public class AbilityUnlock : MonoBehaviour
{
    [SerializeField] private AbilityData abilityToGrant;
    [SerializeField] private GameObject pickupEffect; //  VFX
    [SerializeField] private AbilityPopupController popupController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // If not assigned, try to find it
        if (popupController == null)
            popupController = FindObjectOfType<AbilityPopupController>();

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && abilityToGrant != null)
        {
            // 1. Actually unlock the ability (still needed)
            inventory.UnlockAbility(abilityToGrant);
            Debug.Log("Ability granted " + abilityToGrant);

            // 2. Show the popup directly
            if (popupController != null)
                popupController.ShowPopup(abilityToGrant);
            else
                Debug.LogError("No AbilityPopupController found!");

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}