using UnityEngine;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    // ------- State Machine -------
    public enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    public State currentState = State.Idle;
    private State previousState;

    [Header("References")]
    private Animator anim;
    private CharacterController controller;
    [SerializeField] private Transform player;

    // ------- Movement / Detection -------
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float rotationSpeed = 5f;
    public float patrolRadius = 5f;
    public float patrolWaitTimeMin = 1f;
    public float patrolWaitTimeMax = 3f;

    [Header("Ledge Avoidance")]
    [Tooltip("Distance forward to check for ground before moving")]
    public float ledgeCheckDistance = 0.8f;
    [Tooltip("Layers considered as ground (usually Default and ground layers)")]
    public LayerMask groundLayers = ~0;
    [Tooltip("If player's Y is lower than enemy's Y by this much, stop chasing (prevents jitter at cliffs)")]
    public float minHeightDifferenceForLedge = 1.5f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float loseSightRange = 15f;

    [Header("Combat")]
    public int maxHealth = 5;
    private int currentHealth;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    public float attackAnimationLength = 1.2f;
    private float attackTimer;
    public float hurtAnimationLength = 0.8f;
    private float hurtTimer;
    private bool isHurt;
    public float hurtInvincibilityTime = 1.5f;
    public float deathAnimationLength = 2f;
    
    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float contactDamageCooldown = 0.5f;
    private float lastContactDamageTime = -999f;

    // ------- Patrol internal -------
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float patrolWaitTimer;
    private bool patrolWaiting;
    private bool isMoving;

    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        currentHealth = maxHealth;
        startPosition = transform.position;
        currentState = State.Idle;
        previousState = State.Idle;
        PickNewPatrolTarget();
    }

    void Update()
    {
        if (currentState == State.Dead)
            return;

        bool canSeePlayer = false;
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            canSeePlayer = distToPlayer <= detectionRange;
        }

        if (isHurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f)
                isHurt = false;
        }

        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                if (canSeePlayer && !IsPlayerBelowLedge())
                    ChangeState(State.Chase);
                break;
            case State.Patrol:
                HandlePatrol();
                if (canSeePlayer && !IsPlayerBelowLedge())
                    ChangeState(State.Chase);
                break;
            case State.Chase:
                HandleChase();
                float dist = player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
                if (dist > loseSightRange || player == null)
                    ChangeState(State.Idle);
                else if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown && !isHurt)
                    ChangeState(State.Attack);
                else if (IsPlayerBelowLedge())
                    ChangeState(State.Idle);  // Stop chasing if player is below a ledge
                break;
            case State.Attack:
                HandleAttack();
                break;
            case State.Hurt:
                HandleHurt();
                break;
        }

        SyncAnimator();
    }

    // ------- Helper: Detect if player is unreachable because they are too low -------
    private bool IsPlayerBelowLedge()
    {
        if (player == null) return false;
        // If player is significantly lower than the enemy, treat as "below ledge"
        return player.position.y < transform.position.y - minHeightDifferenceForLedge;
    }

    // ------- STATE CHANGES -------
    void ChangeState(State newState)
    {
        if (newState == currentState)
            return;

        previousState = currentState;
        currentState = newState;

        switch (newState)
        {
            case State.Idle:
                patrolWaitTimer = Random.Range(patrolWaitTimeMin, patrolWaitTimeMax);
                break;
            case State.Patrol:
                PickNewPatrolTarget();
                break;
            case State.Attack:
                lastAttackTime = Time.time;
                attackTimer = attackAnimationLength;
                anim.SetTrigger("OnAttack");
                break;
            case State.Hurt:
                hurtTimer = hurtAnimationLength;
                isHurt = true;
                anim.SetTrigger("OnHurt");
                break;
            case State.Dead:
                DisableAllHitboxes();
                anim.SetTrigger("OnDead");
                controller.enabled = false;
                break;
        }
    }

    void HandleIdle()
    {
        patrolWaitTimer -= Time.deltaTime;
        if (patrolWaitTimer <= 0f)
            ChangeState(State.Patrol);
    }

    void HandlePatrol()
    {
        if (patrolWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            isMoving = false;
            if (patrolWaitTimer <= 0f)
            {
                patrolWaiting = false;
                PickNewPatrolTarget();
            }
            return;
        }

        Vector3 direction = (patrolTarget - transform.position).normalized;
        MoveEnemy(direction, walkSpeed);

        if (direction != Vector3.zero)
            RotateTowards(direction);

        if (Vector3.Distance(transform.position, patrolTarget) < 0.5f)
        {
            patrolWaiting = true;
            patrolWaitTimer = Random.Range(patrolWaitTimeMin, patrolWaitTimeMax);
        }
    }

    void HandleChase()
    {
        if (player == null)
        {
            ChangeState(State.Idle);
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        MoveEnemy(direction, chaseSpeed);
        RotateTowards(direction);
    }

    void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown)
                    ChangeState(State.Attack);
                else if (dist <= detectionRange && !IsPlayerBelowLedge())
                    ChangeState(State.Chase);
                else
                    ChangeState(State.Idle);
            }
            else
            {
                ChangeState(State.Idle);
            }
        }
    }

    void HandleHurt()
    {
        hurtTimer -= Time.deltaTime;
        if (hurtTimer <= 0f)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) <= detectionRange && !IsPlayerBelowLedge())
                ChangeState(State.Chase);
            else
                ChangeState(State.Idle);
        }
    }

    // ------- MOVEMENT with Ledge Avoidance -------
    void MoveEnemy(Vector3 direction, float speed)
    {
        direction.y = 0;
        if (direction.magnitude > 0.05f)
        {
            direction.Normalize();

            // Check for ledge ahead before moving
            if (!IsGroundedAhead(direction))
            {
                // No ground ahead → stop moving
                isMoving = false;
                
                // If in patrol mode and stuck at ledge, pick a new patrol target to avoid being frozen
                if (currentState == State.Patrol)
                {
                    PickNewPatrolTarget();
                }
                return;
            }

            Vector3 move = direction * speed * Time.deltaTime;
            if (!controller.isGrounded)
                move.y -= 9.81f * Time.deltaTime;
            controller.Move(move);
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    /// <summary>
    /// Checks if there is ground directly ahead of the enemy within a short distance.
    /// </summary>
    /// <param name="direction">Normalized movement direction (horizontal only).</param>
    /// <returns>True if ground exists ahead, false if enemy would walk off a ledge.</returns>
    private bool IsGroundedAhead(Vector3 direction)
    {
        // Cast a ray from a point at the enemy's feet forward by ledgeCheckDistance,
        // then downward to detect ground.
        float checkHeight = 0.2f; // Slightly above the bottom of the capsule
        Vector3 rayOrigin = transform.position + Vector3.up * checkHeight;
        Vector3 forwardOffset = direction * ledgeCheckDistance;

        // Ray downward from the point ahead
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin + forwardOffset, Vector3.down, out hit, 2f, groundLayers))
        {
            // Ground found ahead
            return true;
        }
        else
        {
            // No ground → would fall
            return false;
        }
    }

    void RotateTowards(Vector3 direction)
    {
        direction.y = 0;
        if (direction == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    void PickNewPatrolTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        patrolTarget = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void SyncAnimator()
    {
        if (anim == null) return;
        bool shouldWalk = (currentState == State.Chase || currentState == State.Patrol) && isMoving;
        anim.SetBool("IsWalking", shouldWalk);
    }
    
    public void TryDealContactDamage(CharacterStats playerStats)
    {
        if (Time.time < lastContactDamageTime + contactDamageCooldown)
            return;

        if (playerStats != null)
        {
            playerStats.TakeDamage(contactDamage);
            lastContactDamageTime = Time.time;

            PlayerKnockback knockback = playerStats.GetComponent<PlayerKnockback>();
            if (knockback != null)
            {
                Vector3 direction = (playerStats.transform.position - transform.position).normalized;
                knockback.ApplyKnockback(direction);
            }
        }
    }

    // ------- PUBLIC DAMAGE METHODS -------
    public void TakeDamage(int damage)
    {
        if (currentState == State.Dead || isHurt)
            return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            ChangeState(State.Dead);
        }
        else
        {
            ChangeState(State.Hurt);
        }
    }

    public void TakeHit(Vector3 hitPoint)
    {
        if (currentState == State.Dead)
            return;

        if (currentState != State.Hurt)
            ChangeState(State.Hurt);
    }
    
    public void Die()
    {
        if (currentState == State.Dead)
            return;

        ChangeState(State.Dead);
        StartCoroutine(DeathRoutine());
    }
    
    private void DisableAllHitboxes()
    {
        EnemyHitbox[] hitboxes = GetComponentsInChildren<EnemyHitbox>();
        foreach (EnemyHitbox hb in hitboxes)
        {
            hb.gameObject.SetActive(false);
        }
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathAnimationLength);

        float sinkDuration = 2f;
        float sinkDistance = 3f;
        float startTime = Time.time;
        float startY = transform.position.y;
        float targetY = startY - sinkDistance;

        while (Time.time < startTime + sinkDuration)
        {
            float t = (Time.time - startTime) / sinkDuration;
            float newY = Mathf.Lerp(startY, targetY, t);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Visualize ledge check ray when selected
        Gizmos.color = Color.cyan;
        Vector3 checkOrigin = transform.position + Vector3.up * 0.2f;
        Vector3 forwardDir = transform.forward * ledgeCheckDistance;
        Gizmos.DrawLine(checkOrigin, checkOrigin + forwardDir);
        Gizmos.DrawLine(checkOrigin + forwardDir, checkOrigin + forwardDir + Vector3.down * 2f);
    }
}