using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  References
    // ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cam;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    // ─────────────────────────────────────────────
    //  Movement
    // ─────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    // ─────────────────────────────────────────────
    //  Jump
    // ─────────────────────────────────────────────
    [Header("Jump — Core")]
    [SerializeField] private float jumpHeight = 3.5f;
    [SerializeField] private float timeToApex = 0.5f;

    [Header("Jump — Feel")]
    [SerializeField] private float fallGravityMultiplier = 1f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float apexThreshold = 1f;
    [Range(0f, 1f)] [SerializeField] private float apexGravityScale = 0.75f;
    [SerializeField] private float maxFallSpeed = -25f;

    // ─────────────────────────────────────────────
    //  Ledge Hang
    // ─────────────────────────────────────────────
    [Header("Ledge Hang")]
    [SerializeField] private float ledgeReachDistance = 0.80f;
    [SerializeField] private float ledgeCheckHeight = 1.5f;
    [SerializeField] private LayerMask ledgeLayerMask = ~0;
    [SerializeField] private float hangDropSpeed = 2f;
    [SerializeField] private float ledgeVaultForce = 13f;
    [SerializeField] private float hangSnapOffset = 1.5f;
    [SerializeField] private float hangActivationTolerance = 0.15f;
    [SerializeField] private float vaultLungeDelay = 0.15f;   // Time before lunge allowed after vault

    // ─────────────────────────────────────────────
    //  Lock-On (set by external system)
    // ─────────────────────────────────────────────
    [HideInInspector] public bool isLockedOn;
    [HideInInspector] public Transform lockOnTarget;

    // ─────────────────────────────────────────────
    //  Lunge (called by WeaponController)
    // ─────────────────────────────────────────────
    [Header("Lunge Attack")]
    [SerializeField] private float lungeForce = 18f;
    [SerializeField] private float lungeGravitySuppressDuration = 0.15f;

    private WeaponController weaponController;
    private float lungeGravityTimer;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isHanging;
    private bool isVaultJump;
    private bool vaultInputPressed;

    private float gravity;
    private float jumpForce;
    private float hangCooldownTimer;
    private float vaultCooldownTimer;

    // ─────────────────────────────────────────────
    //  Public state
    // ─────────────────────────────────────────────
    public bool IsGrounded => isGrounded;
    public bool IsJumping  => isJumping;
    public bool IsHanging  => isHanging;
    public bool CanLungeImmediately => vaultCooldownTimer <= 0f;

    // ─────────────────────────────────────────────
    //  Coyote Time & Jump Buffer
    // ─────────────────────────────────────────────
    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    private float coyoteTimer;
    private float jumpBufferTimer;

    // ─────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction  = playerInput.actions["Move"];
        jumpAction  = playerInput.actions["Jump"];
        RecalculateJumpPhysics();
    }

    private void Start()
    {
        weaponController = GetComponent<WeaponController>();
    }

    private void OnValidate() => RecalculateJumpPhysics();

    private void RecalculateJumpPhysics()
    {
        if (timeToApex <= 0f) return;
        gravity   = -(2f * jumpHeight) / (timeToApex * timeToApex);
        jumpForce =  (2f * jumpHeight) / timeToApex;
    }

    private void OnEnable()  { moveAction.Enable();  jumpAction.Enable();  }
    private void OnDisable() { moveAction.Disable(); jumpAction.Disable(); }

    // ─────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────
    private void Update()
    {
        // Timers
        vaultCooldownTimer -= Time.deltaTime;

        // Capture a fresh press toward the wall for vaulting
        if (moveAction.WasPressedThisFrame())
        {
            if (IsMovingTowardWall())
                vaultInputPressed = true;
        }

        HandleGrounding();

        if (isHanging)
        {
            HandleHang();
            return;
        }

        hangCooldownTimer  -= Time.deltaTime;
        lungeGravityTimer  -= Time.deltaTime;

        HandleMovement();
        HandleJumpBuffer();
        HandleJump();
        HandleLedgeDetection();

        if (isVaultJump && velocity.y <= 0f)
            isVaultJump = false;

        ApplyGravity();
        ClampFallSpeed();

        controller.Move(velocity * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Grounding
    // ─────────────────────────────────────────────
    private void HandleGrounding()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            if (velocity.y < 0f)
            {
                velocity.y = -2f;
                isJumping  = false;
            }

            if (isHanging) ExitHang();

            isVaultJump = false;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    // ─────────────────────────────────────────────
    //  Ledge Detection
    // ─────────────────────────────────────────────
    private void HandleLedgeDetection()
    {
        if (isGrounded) return;
        if (hangCooldownTimer > 0f) return;

        Vector3 origin      = transform.position;
        Vector3 aboveOrigin = origin + Vector3.up * ledgeCheckHeight;
        Vector3 forward     = transform.forward;

        RaycastHit wallHitInfo;
        bool wallHit  = Physics.Raycast(origin,       forward, out wallHitInfo, ledgeReachDistance, ledgeLayerMask);
        bool clearTop = !Physics.Raycast(aboveOrigin, forward, ledgeReachDistance, ledgeLayerMask);

        if (!wallHit || !clearTop) return;

        Vector3 downRayOrigin = aboveOrigin + forward * ledgeReachDistance;
        RaycastHit surfaceHit;
        float ledgeSurfaceY;
        if (Physics.Raycast(downRayOrigin, Vector3.down, out surfaceHit, ledgeCheckHeight + 0.5f, ledgeLayerMask))
            ledgeSurfaceY = surfaceHit.point.y;
        else
            ledgeSurfaceY = wallHitInfo.point.y;

        float targetY = ledgeSurfaceY - hangSnapOffset;

        if (Mathf.Abs(transform.position.y - targetY) <= hangActivationTolerance && velocity.y <= 0f)
            EnterHang();
    }

    private void EnterHang()
    {
        isHanging = true;
        isJumping = false;
        velocity  = Vector3.zero;
    }

    private void ExitHang()
    {
        isHanging         = false;
        hangCooldownTimer = 0.3f;
        vaultInputPressed = false;
    }

    // ─────────────────────────────────────────────
    //  Hang inputs
    // ─────────────────────────────────────────────
    private void HandleHang()
    {
        // Drop: pressing S (backward relative to player facing)
        if (IsMovingBackward())
        {
            ExitHang();
            velocity.y = -hangDropSpeed;
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        // Vault: movement key toward wall (buffered) OR Space pressed this frame
        bool vaultViaMovement = vaultInputPressed;
        bool vaultViaJump     = jumpAction.WasPressedThisFrame();

        if (vaultViaMovement || vaultViaJump)
        {
            vaultInputPressed = false;

            // Prevent accidental normal jump from firing after vault
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            vaultCooldownTimer = vaultLungeDelay;

            ExitHang();
            velocity.y = ledgeVaultForce;
            isVaultJump = true;          // bypass short‑hop gravity, full height
            // isJumping stays false → allows lunge after cooldown
            controller.Move(velocity * Time.deltaTime);
        }
    }

    /// <summary>
    /// Returns true if the player is pressing the key that would move them backward.
    /// </summary>
    private bool IsMovingBackward()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return false;

        float camYaw = cam.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(input.x, 0f, input.y);
        moveDir.Normalize();

        return Vector3.Dot(moveDir, transform.forward) < -0.5f;
    }

    /// <summary>
    /// Returns true if the player is pressing the key that would move them toward the wall
    /// (forward relative to player facing).
    /// </summary>
    private bool IsMovingTowardWall()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return false;

        float camYaw = cam.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(input.x, 0f, input.y);
        moveDir.Normalize();

        return Vector3.Dot(moveDir, transform.forward) > 0.5f;
    }

    // ─────────────────────────────────────────────
    //  Movement & Rotation
    // ─────────────────────────────────────────────
    private void HandleMovement()
    {
        Vector2 input      = moveAction.ReadValue<Vector2>();
        float   horizontal = input.x;
        float   vertical   = input.y;
        Vector3 direction  = new Vector3(horizontal, 0f, vertical).normalized;

        if (!isLockedOn)
        {
            if (direction.magnitude >= 0.1f)
            {
                float targetAngle  = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg
                                   + cam.eulerAngles.y;
                float currentAngle = transform.eulerAngles.y;
                float angleDiff    = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

                float angle;
                if (angleDiff > 85f)
                {
                    angle = targetAngle;
                    turnSmoothVelocity = 0f;
                }
                else
                {
                    angle = Mathf.SmoothDampAngle(currentAngle, targetAngle,
                                ref turnSmoothVelocity, turnSmoothTime);
                }

                transform.rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * speed * Time.deltaTime);
            }
        }
        else
        {
            Vector3 toTarget = lockOnTarget.position - transform.position;
            toTarget.y = 0f;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toTarget),
                Time.deltaTime * 10f);

            Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    //  Jump Buffer
    // ─────────────────────────────────────────────
    private void HandleJumpBuffer()
    {
        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  Jump
    // ─────────────────────────────────────────────
    private void HandleJump()
    {
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y      = jumpForce;
            isJumping       = true;
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
        }

        // Only cancel jump on release if it's NOT a vault jump
        if (jumpAction.WasReleasedThisFrame() && isJumping && velocity.y > 0f && !isVaultJump)
            isJumping = false;
    }

    // ─────────────────────────────────────────────
    //  Gravity
    // ─────────────────────────────────────────────
    private void ApplyGravity()
    {
        if (lungeGravityTimer > 0f) return;

        bool atApex = Mathf.Abs(velocity.y) < apexThreshold && !isGrounded;

        if (atApex)
            velocity.y += gravity * apexGravityScale * Time.deltaTime;
        else if (velocity.y < 0f)
            velocity.y += gravity * fallGravityMultiplier * Time.deltaTime;
        else if (!isJumping && velocity.y > 0f && !isVaultJump)
            velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;
    }

    private void ClampFallSpeed()
    {
        if (velocity.y < maxFallSpeed)
            velocity.y = maxFallSpeed;
    }

    // ─────────────────────────────────────────────
    //  Lunge
    // ─────────────────────────────────────────────
    public void PerformLunge()
    {
        velocity.y = lungeForce;
        lungeGravityTimer = lungeGravitySuppressDuration;
    }

    // ─────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 origin      = transform.position;
        Vector3 aboveOrigin = origin + Vector3.up * ledgeCheckHeight;
        Vector3 forward     = transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin,       forward * ledgeReachDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(aboveOrigin,  forward * ledgeReachDistance);
    }
}