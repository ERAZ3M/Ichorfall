using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerKnockback : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackUpwardForce = 5f;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private PlayerStats playerStats;   // <-- add this
    private bool isKnockedBack;

    public bool IsKnockedBack => isKnockedBack;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerStats = GetComponent<PlayerStats>();   // <-- get reference
    }

    public void ApplyKnockback(Vector3 direction, float forceMultiplier = 1f)
    {
        // Don't start a new knockback if the player is already dead
        if (playerStats != null && playerStats.IsDead) return;

        if (!isKnockedBack)
            StartCoroutine(KnockbackRoutine(direction, knockbackForce * forceMultiplier));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        isKnockedBack = true;

        playerMovement.ResetVelocity();
        float upward = knockbackUpwardForce;
        playerMovement.SetVerticalVelocity(upward);

        direction.y = 0f;
        direction.Normalize();

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            Vector3 move = direction * (force * Time.deltaTime / knockbackDuration);
            controller.Move(move);
            timer += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;
    }
}