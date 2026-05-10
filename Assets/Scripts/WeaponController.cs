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
    [SerializeField] private PlayerInventory playerInventory;
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

    [Header("Ability Requirements")]
    [SerializeField] private AbilityData swordAbility;   // Drag SwordAbility asset here
    [SerializeField] private AbilityData lungeAbility;   // Drag LungeAbility asset here

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
        {
            // Keep sword inactive until the sword ability is unlocked
            sword.SetActive(HasSwordAbility());
            swordAnimator = sword.GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("Sword GameObject not assigned!");
        }

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        // Listen for ability unlocks to enable the sword later
        if (playerInventory != null)
            playerInventory.OnAbilityUnlocked.AddListener(OnAnyAbilityUnlocked);
    }

    private void OnEnable()  => attackAction.Enable();
    private void OnDisable()
    {
        attackAction.Disable();
        if (playerInventory != null)
            playerInventory.OnAbilityUnlocked.RemoveListener(OnAnyAbilityUnlocked);
    }

    private void Update()
    {
        // Reset lunge when grounded OR hanging
        if ((playerMovement.IsGrounded || playerMovement.IsHanging) && !canLunge)
        {
            ResetLunge();
        }

        // Attack – gated by sword ability
        if (attackAction.WasPressedThisFrame() && canAttack && HasSwordAbility())
        {
            SwordAttack();
        }

        // Lunge – gated by lunge ability AND all existing conditions
        if (!playerMovement.IsGrounded && 
            !playerMovement.IsHanging && 
            !playerMovement.IsJumping && 
            playerMovement.CanLungeImmediately && 
            canLunge && 
            !isLunging && 
            jumpAction.WasPressedThisFrame() && 
            HasLungeAbility())
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

    private bool HasSwordAbility() => playerInventory != null && playerInventory.HasAbility(swordAbility);
    private bool HasLungeAbility() => playerInventory != null && playerInventory.HasAbility(lungeAbility);

    private void OnAnyAbilityUnlocked(AbilityData ability)
    {
        // Enable the sword GameObject when the sword ability is unlocked
        if (ability == swordAbility && sword != null)
            sword.SetActive(true);
    }
}