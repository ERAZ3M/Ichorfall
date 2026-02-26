using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private GameObject sword;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private string trigger = "TestAttack";

    [SerializeField] private PlayerInput playerInput;
    private InputAction attackAction;
    
    private Animator swordAnimator;
    private bool canAttack = true;

    void Start()
    {
        if (sword != null)
            swordAnimator = sword.GetComponent<Animator>();
        else
            Debug.LogError("Sword GameObject not assigned!");
        
        attackAction = playerInput.actions["Attack"];
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
        canAttack = false;
        swordAnimator.SetTrigger(trigger);
        StartCoroutine(ResetAttackCooldown());
    }

    IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
}