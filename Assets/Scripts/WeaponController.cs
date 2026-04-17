using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject sword;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerMovement playerMovement;
    private InputAction attackAction;
    private InputAction jumpAction;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 0.67f;
    [SerializeField] private float attackActiveDuration = 0.5f;
    [SerializeField] private string trigger = "TestAttack";

    [Header("Lunge Attack")]
    [SerializeField] private string lungeTrigger = "LungeAttack";
    [SerializeField] private float lungeWindUp = 0.2f;
    [SerializeField] private float lungeActiveDuration = 0.3f;

    private Animator swordAnimator;
    private bool canAttack = true;
    public bool isAttacking = false;
    public bool isLunging = false;

    private bool canLunge = true;
    private Coroutine lungeCoroutine;

    public int Damage => playerStats != null ? playerStats.damage : 0;

    private void Awake()
    {
        attackAction = playerInput.actions["Attack"];
        jumpAction   = playerInput.actions["Jump"];
    }

    private void Start()
    {
        if (sword != null)
            swordAnimator = sword.GetComponent<Animator>();
        else
            Debug.LogError("Sword GameObject not assigned!");

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()  => attackAction.Enable();
    private void OnDisable() => attackAction.Disable();

    private void Update()
    {
        // Reset lunge when grounded OR hanging
        if ((playerMovement.IsGrounded || playerMovement.IsHanging) && !canLunge)
        {
            ResetLunge();
        }

        if (attackAction.WasPressedThisFrame() && canAttack)
        {
            SwordAttack();
        }

        // Lunge only when airborne, NOT hanging, NOT in a normal jump, after vault cooldown, and lunge available
        if (!playerMovement.IsGrounded && 
            !playerMovement.IsHanging && 
            !playerMovement.IsJumping && 
            playerMovement.CanLungeImmediately && 
            canLunge && 
            !isLunging && 
            jumpAction.WasPressedThisFrame())
        {
            lungeCoroutine = StartCoroutine(LungeAttack());
        }
    }

    private void SwordAttack()
    {
        isAttacking = true;
        canAttack = false;
        swordAnimator.SetTrigger(trigger);
        StartCoroutine(ResetAttackCooldown());
    }

    private IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackActiveDuration);
        isAttacking = false;

        float remaining = Mathf.Max(0f, attackCooldown - attackActiveDuration);
        yield return new WaitForSeconds(remaining);
        canAttack = true;
    }

    private IEnumerator LungeAttack()
    {
        isLunging = true;
        canLunge  = false;
        swordAnimator.SetTrigger(lungeTrigger);

        yield return new WaitForSeconds(lungeWindUp);

        playerMovement.PerformLunge();

        yield return new WaitForSeconds(lungeActiveDuration);
        isLunging = false;
        // canLunge remains false – reset happens in Update()
    }

    public void ResetLunge()
    {
        if (canLunge) return;

        if (lungeCoroutine != null)
        {
            StopCoroutine(lungeCoroutine);
            lungeCoroutine = null;
        }
        isLunging = false;
        canLunge  = true;
    }
}