using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerKnockback : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackUpwardForce = 5f;  // NEW: upward launch strength

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private bool isKnockedBack;

    public bool IsKnockedBack => isKnockedBack;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void ApplyKnockback(Vector3 direction, float forceMultiplier = 1f)
    {
        if (!isKnockedBack)
            StartCoroutine(KnockbackRoutine(direction, knockbackForce * forceMultiplier));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        isKnockedBack = true;

        // Kill any existing momentum so the knockback feels crisp
        playerMovement.ResetVelocity();

        // ADD: Launch the player upward like a jump
        // Multiply by forceMultiplier if you want the upward force to scale with knockback strength
        float upward = knockbackUpwardForce; // * (force / knockbackForce) optional scaling
        playerMovement.SetVerticalVelocity(upward);

        direction.y = 0f;
        direction.Normalize();

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            // Move horizontally each frame – gravity is still applied by PlayerMovement
            Vector3 move = direction * (force * Time.deltaTime / knockbackDuration);
            controller.Move(move);
            timer += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;
    }
}