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
                if (canSeePlayer)
                    ChangeState(State.Chase);
                break;
            case State.Patrol:
                HandlePatrol();
                if (canSeePlayer)
                    ChangeState(State.Chase);
                break;
            case State.Chase:
                HandleChase();
                float dist = player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
                if (dist > loseSightRange || player == null)
                    ChangeState(State.Idle);
                else if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown && !isHurt)
                    ChangeState(State.Attack);
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
            isMoving = false;   // <-- ensure Idle pose
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
                else if (dist <= detectionRange)
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
            if (player != null && Vector3.Distance(transform.position, player.position) <= detectionRange)
                ChangeState(State.Chase);
            else
                ChangeState(State.Idle);
        }
    }

    // ------- MOVEMENT -------
    void MoveEnemy(Vector3 direction, float speed)
    {
        direction.y = 0;
        if (direction.magnitude > 0.05f)
        {
            direction.Normalize();
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
        // Only set IsWalking when actually moving
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

            // Knockback – use playerStats.gameObject to get the component
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

    // This is called by your CollisionDetection script
    public void TakeHit(Vector3 hitPoint)
    {
        if (currentState == State.Dead)
            return;

        // The actual damage is already applied by CharacterStats.TakeDamage.
        // Here we just ensure the hurt animation plays (if not already hurt).
        if (currentState != State.Hurt)
            ChangeState(State.Hurt);

        // Optional: you could add knockback using (transform.position - hitPoint) later.
    }
    
    public void Die()
    {
        if (currentState == State.Dead)
            return; // already dead, nothing to do

        ChangeState(State.Dead);
        StartCoroutine(DeathRoutine());
    }
    
    private void DisableAllHitboxes()
    {
        // Find all EnemyHitbox scripts in children (on the body and hands)
        EnemyHitbox[] hitboxes = GetComponentsInChildren<EnemyHitbox>();
        foreach (EnemyHitbox hb in hitboxes)
        {
            // Disable the whole hitbox GameObject so the trigger won't fire
            hb.gameObject.SetActive(false);
        }
    }

    private IEnumerator DeathRoutine()
    {
        // Wait for the death animation to finish
        yield return new WaitForSeconds(deathAnimationLength);

        // --- Sink effect ---
        float sinkDuration = 2f;
        float sinkDistance = 3f;             // How far down to sink (in world units)
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

        // Snap to final position
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

        // Optionally, wait a tiny bit more or just destroy
        Destroy(gameObject);
    }

    // Optional: draw detection/attack ranges in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}