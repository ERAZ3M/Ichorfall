using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private GameObject sword;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerStats playerStats;
    private InputAction attackAction;
    
    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private float attackActiveDuration = 0.5f;
    [SerializeField] private string trigger = "TestAttack";
    
    private Animator swordAnimator;
    private bool canAttack = true;
    public bool isAttacking = false;
    
    public int Damage => playerStats != null ? playerStats.damage : 0;


    void Awake()
    {
        attackAction = playerInput.actions["Attack"];
        
    }
    void Start()
    {
        if (sword != null)
            swordAnimator = sword.GetComponent<Animator>();
        else
            Debug.LogError("Sword GameObject not assigned!");

        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>(); 
        }
    }
    
    private void OnEnable()
    {
        attackAction.Enable();
    }

    private void OnDisable()
    {
        attackAction.Disable();
    }


    void Update()
    {
        if (attackAction.WasPressedThisFrame() && canAttack)
        {
            SwordAttack();
        }
    }

    void SwordAttack()
    {
        isAttacking = true;
        canAttack = false;
        swordAnimator.SetTrigger(trigger);
        StartCoroutine(ResetAttackCooldown());
    }

    IEnumerator ResetAttackCooldown()
    {
        
        // so the hitbox active time and cooldown can be tuned independently in the Inspector.
        yield return new WaitForSeconds(attackActiveDuration);
        isAttacking = false;
 
        // Wait out the remainder of the cooldown before allowing another attack
        yield return new WaitForSeconds(attackCooldown - attackActiveDuration);
        canAttack = true;
    }
}