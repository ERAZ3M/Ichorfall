using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : CharacterStats
{
    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 3f;

    [Header("UI")]
    [SerializeField] private GameObject deathScreenUI;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void Die()
    {
        Debug.Log("Player died.");
        deathScreenUI.SetActive(true);
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        ResetHealth();
        
        controller.enabled = false;
        transform.position = respawnPoint.position;
        controller.enabled = true;

        // Re-enable collider (was disabled in base Die())
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        deathScreenUI.SetActive(false);
    }
}