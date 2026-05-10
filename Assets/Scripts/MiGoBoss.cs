using UnityEngine;

public class EnemyAI_MiGo : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Hurt, Dead }
    public State currentState = State.Idle;

    [Header("References")]
    private Transform player;
    private CharacterController controller;
    private CharacterStats stats;
    private Animator animator;

    [Header("Movement")]
    [SerializeField] private float groundMoveSpeed = 3.5f;
    [SerializeField] private float flyMoveSpeed = 5f;
    private float currentMoveSpeed;
    [SerializeField] private float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Detection")]
    [SerializeField] private float chaseRange = 12f;
    [SerializeField] private float attackRange = 2.5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    private float attackTimer = 0f;
    [SerializeField] private int attackDamage = 15;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    [SerializeField] private float patrolWaitTime = 1.5f;
    private float patrolWaitTimer = 0f;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;

    [Header("Hurt / Knockback")]
    [SerializeField] private float hurtDuration = 0.4f;
    private float hurtTimer = 0f;
    [SerializeField] private float knockbackForce = 5f;
    private Vector3 knockbackDirection;

    [Header("Mi-Go Specific")]
    [SerializeField] private int maxHealth = 200;
    [SerializeField] private float phase2HealthPercent = 40f; // below 40% health = flying phase
    private bool isFlyingPhase = false;
    [SerializeField] private int extendedHeadDamageThreshold = 10; // every X total damage
    private float accumulatedDamageSinceLastRoar = 0f;
    [Range(0f,1f)] [SerializeField] private float extendedHeadChance = 0.4f;

    // Animation triggers (rename to match your Animator)
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animIsFlying = Animator.StringToHash("IsFlying");
    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animHurt = Animator.StringToHash("Hurt");
    private readonly int animDead = Animator.StringToHash("Dead");
    private readonly int animExtendedHead = Animator.StringToHash("ExtendedHead"); // your roar trigger

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        controller = GetComponent<CharacterController>();
        stats = GetComponent<CharacterStats>();
        animator = GetComponent<Animator>();

        if (stats != null) stats.currentHealth = maxHealth;

        currentMoveSpeed = groundMoveSpeed;
        currentState = patrolPoints.Length > 0 ? State.Patrol : State.Idle;
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        // Apply gravity only when grounded or not flying phase
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        if (!isFlyingPhase) // grounded phase uses gravity
            verticalVelocity += gravity * Time.deltaTime;
        else // flying phase: no gravity, but allow vertical movement? I'll keep zero vertical change for simplicity
            verticalVelocity = 0f;

        attackTimer -= Time.deltaTime;

        // Phase transition check (grounded → flying)
        if (!isFlyingPhase && stats != null && stats.currentHealth <= maxHealth * (phase2HealthPercent / 100f))
            TransitionToFlyingPhase();

        switch (currentState)
        {
            case State.Idle:   UpdateIdle();   break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
            case State.Hurt:   UpdateHurt();   break;
        }

        // Update animator parameters
        animator.SetBool(animIsFlying, isFlyingPhase);
        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        animator.SetFloat(animSpeed, currentHorizontalSpeed / currentMoveSpeed);
    }

    void TransitionToFlyingPhase()
    {
        isFlyingPhase = true;
        currentMoveSpeed = flyMoveSpeed;
        // Optional: set animator trigger for takeoff
    }

    void UpdateIdle()
    {
        if (CanSeePlayer())
            currentState = State.Chase;
    }

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

    void UpdateChase()
    {
        float distToPlayer = DistanceToPlayer();

        if (distToPlayer <= attackRange && attackTimer <= 0f && currentState != State.Hurt)
        {
            currentState = State.Attack;
            return;
        }

        if (distToPlayer > chaseRange * 1.5f)
        {
            currentState = patrolPoints.Length > 0 ? State.Patrol : State.Idle;
            return;
        }

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        MoveInDirection(dir.normalized);
        FaceDirection(dir.normalized);
    }

    void UpdateAttack()
    {
        FaceDirection((player.position - transform.position).normalized);

        if (DistanceToPlayer() <= attackRange)
        {
            CharacterStats playerStats = player.GetComponent<CharacterStats>();
            if (playerStats != null)
                playerStats.TakeDamage(attackDamage);
        }

        attackTimer = attackCooldown;
        animator.SetTrigger(animAttack);
        currentState = State.Chase;
    }

    // Called from outside when the boss takes damage
    public void TakeDamage(int damageAmount, Vector3 attackerPosition)
    {
        if (currentState == State.Dead) return;
        if (stats == null) return;

        // Apply damage
        stats.currentHealth -= damageAmount;
        accumulatedDamageSinceLastRoar += damageAmount;

        // Check for extended head roar (interrupts current action if not dead/hurt)
        if (accumulatedDamageSinceLastRoar >= extendedHeadDamageThreshold && 
            currentState != State.Dead && currentState != State.Hurt &&
            Random.value <= extendedHeadChance)
        {
            accumulatedDamageSinceLastRoar = 0f;
            StartCoroutine(ExtendedHeadRoar());
            return; // skip regular hurt state
        }

        // Normal hurt reaction
        knockbackDirection = (transform.position - attackerPosition).normalized;
        knockbackDirection.y = 0f;
        hurtTimer = hurtDuration;
        currentState = State.Hurt;
        animator.SetTrigger(animHurt);

        if (stats.currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator ExtendedHeadRoar()
    {
        State previousState = currentState;
        currentState = State.Hurt; // prevents movement/attacks during roar
        animator.SetTrigger(animExtendedHead);
        // Wait for animation length (approx 1-1.5 sec)
        float roarLength = GetAnimationClipLength("extendedhead2idle");
        yield return new WaitForSeconds(roarLength);
        if (currentState != State.Dead)
            currentState = previousState == State.Hurt ? State.Chase : previousState;
    }

    void UpdateHurt()
    {
        hurtTimer -= Time.deltaTime;

        Vector3 knockback = knockbackDirection * knockbackForce;
        knockback.y = verticalVelocity;
        controller.Move(knockback * Time.deltaTime);

        if (hurtTimer <= 0f)
            currentState = State.Chase;
    }

    void Die()
    {
        currentState = State.Dead;
        animator.SetTrigger(animDead);
        controller.enabled = false;
        // Optionally destroy after a delay
        Destroy(gameObject, 5f);
    }

    // --- helpers ---
    void MoveInDirection(Vector3 dir)
    {
        Vector3 move = dir * currentMoveSpeed;
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
        if (player == null) return 999f;
        return Vector3.Distance(transform.position, player.position);
    }

    float GetAnimationClipLength(string clipName)
    {
        if (animator == null) return 1f;
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
            if (clip.name.ToLower().Contains(clipName.ToLower()))
                return clip.length;
        return 1f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}