using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    public State currentState = State.Idle;

    [Header("References")]
    private Transform player;
    private CharacterController controller;
    private CharacterStats stats;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Detection")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    private float attackTimer = 0f;
    [SerializeField] private int attackDamage = 1;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    [SerializeField] private float patrolWaitTime = 1.5f;
    private float patrolWaitTimer = 0f;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.3f;
    private float hurtTimer = 0f;
    [SerializeField] private float knockbackForce = 4f;
    private Vector3 knockbackDirection;

    void Start()
    {
        
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
        
        controller = GetComponent<CharacterController>();
        stats = GetComponent<CharacterStats>();

        // Auto-find player if not assigned
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        currentState = patrolPoints.Length > 0 ? State.Patrol : State.Idle;
    }

    void Update()
    {
        // Apply gravity always
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
            case State.Hurt:   UpdateHurt();   break;
        }
    }

    // ─── IDLE ───────────────────────────────────────────────
    void UpdateIdle()
    {
        if (CanSeePlayer())
            currentState = State.Chase;
    }

    // ─── PATROL ─────────────────────────────────────────────
    void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            currentState = State.Chase;
            return;
        }

        if (patrolPoints.Length == 0) return;

        Transform target = patrolPoints[currentPatrolIndex];
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;

        if (dir.magnitude < patrolPointReachedDistance)
        {
            // Reached point — wait, then move to next
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolWaitTimer = patrolWaitTime;
            }
        }
        else
        {
            MoveInDirection(dir.normalized);
        }
    }

    // ─── CHASE ──────────────────────────────────────────────
    void UpdateChase()
    {
        float distToPlayer = DistanceToPlayer();

        if (distToPlayer <= attackRange && attackTimer <= 0f)
        {
            currentState = State.Attack;
            return;
        }

        if (distToPlayer > chaseRange * 1.5f) // Leash range — give up chase
        {
            currentState = patrolPoints.Length > 0 ? State.Patrol : State.Idle;
            return;
        }

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        MoveInDirection(dir.normalized);
        FaceDirection(dir.normalized);
    }

    // ─── ATTACK ─────────────────────────────────────────────
    void UpdateAttack()
    {
        FaceDirection((player.position - transform.position).normalized);

        // Deal damage if player is still in range
        if (DistanceToPlayer() <= attackRange)
        {
            CharacterStats playerStats = player.GetComponent<CharacterStats>();
            if (playerStats != null)
                playerStats.TakeDamage(attackDamage);
        }

        attackTimer = attackCooldown;
        currentState = State.Chase; // Return to chase after attacking
    }

    // ─── HURT ───────────────────────────────────────────────
    public void TakeHit(Vector3 attackerPosition)
    {
        if (currentState == State.Dead) return;

        knockbackDirection = (transform.position - attackerPosition).normalized;
        knockbackDirection.y = 0f;
        hurtTimer = hurtDuration;
        currentState = State.Hurt;
        
        if (stats.currentHealth <= 0)
        {
            currentState = State.Dead;
            controller.enabled = false; // Stops all movement
        }
    }

    void UpdateHurt()
    {
        hurtTimer -= Time.deltaTime;

        // Apply knockback
        Vector3 knockback = knockbackDirection * knockbackForce;
        knockback.y = verticalVelocity;
        controller.Move(knockback * Time.deltaTime);

        if (hurtTimer <= 0f)
            currentState = State.Chase;
    }

    // ─── HELPERS ────────────────────────────────────────────
    void MoveInDirection(Vector3 dir)
    {
        Vector3 move = dir * moveSpeed;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    bool CanSeePlayer()
    {
        return DistanceToPlayer() <= chaseRange;
    }

    float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}