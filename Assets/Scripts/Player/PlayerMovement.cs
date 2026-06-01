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
    [SerializeField] private PlayerInventory playerInventory;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;

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
    [SerializeField] private float vaultLungeDelay = 0.15f;

    // For fresh forward press detection while hanging
    private bool wasMovingForwardLastFrame;
    private bool hasJustPressedForwardInHang;

    // ─────────────────────────────────────────────
    //  Lock-On
    // ─────────────────────────────────────────────
    [HideInInspector] public bool isLockedOn;
    [HideInInspector] public Transform lockOnTarget;

    // ─────────────────────────────────────────────
    //  Lunge Attack
    // ─────────────────────────────────────────────
    [Header("Lunge Attack")]
    [SerializeField] private float lungeForce = 18f;
    [SerializeField] private float lungeGravitySuppressDuration = 0.15f;

    private WeaponController weaponController;
    private float lungeGravityTimer;

    // ─────────────────────────────────────────────
    //  Dash
    // ─────────────────────────────────────────────
    [Header("Dash")]
    [SerializeField] private AbilityData dashAbility;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashExitBoostDecay = 0.3f;
    [SerializeField] private float dashSteeringSpeed = 180f;
    [SerializeField] private float dashFacingRotationSpeed = 360f;

    [Header("Dash Gravity")]
    [SerializeField] private float dashGravityReductionDuration = 0.1f;
    [SerializeField] private float dashGravityMultiplier = 0.3f;

    [Header("Dash Reverse Brake")]
    [SerializeField] private float reverseBrakeDuration = 0.1f;
    private float reverseBrakeTimer = 0f;

    private bool isDashing;
    private float dashStartTime;
    private float dashEndTime;
    private Vector3 dashDirection;
    private float lastDashTime = -999f;
    private bool dashedInAir;

    // Speed boost after dash
    private float currentSpeedMultiplier = 1f;
    private float speedMultiplierDecay = 0f;
    private Vector3 lastDashDirection;

    // Jump buffering during dash
    [Header("Dash Jump Buffer")]
    [SerializeField] private float dashJumpBufferTime = 0.2f;
    private float dashJumpBufferTimer = 0f;
    private bool hasQueuedJumpDuringDash = false;

    public bool IsDashing => isDashing;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isHanging;
    private bool isVaultJump;

    private float gravity;
    private float jumpForce;
    private float hangCooldownTimer;
    private float vaultCooldownTimer;

    private bool controlsEnabled = true;

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
        dashAction  = playerInput.actions["Dash"];
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
    
    public void SetVerticalVelocity(float y) => velocity.y = y;
    
    public void ResetVelocity()
    {
        velocity = Vector3.zero;
        lungeGravityTimer = 0f;
        isJumping = false;
        isVaultJump = false;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        dashJumpBufferTimer = 0f;
        hasQueuedJumpDuringDash = false;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            dashJumpBufferTimer = 0f;
            hasQueuedJumpDuringDash = false;
        }
    }

    private void OnEnable()  { moveAction.Enable();  jumpAction.Enable(); dashAction.Enable(); }
    private void OnDisable() { moveAction.Disable(); jumpAction.Disable(); dashAction.Disable(); }

    // ─────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────
    private void Update()
    {
        HandleGrounding();

        if (!controlsEnabled)
        {
            ApplyGravity();
            ClampFallSpeed();
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        PlayerKnockback knockback = GetComponent<PlayerKnockback>();
        bool isKnockedBack = (knockback != null && knockback.IsKnockedBack);

        vaultCooldownTimer -= Time.deltaTime;

        // --- Dash handling ---
        if (isDashing)
        {
            // Jump buffering during dash
            if (jumpAction.WasPressedThisFrame())
            {
                hasQueuedJumpDuringDash = true;
                dashJumpBufferTimer = dashJumpBufferTime;
            }

            float step = dashSpeed * Time.deltaTime;
            controller.Move(dashDirection * step);
            
            // Custom gravity during dash
            float dashTimeElapsed = Time.time - dashStartTime;
            float gravityMultiplier = (dashTimeElapsed < dashGravityReductionDuration) ? dashGravityMultiplier : 1f;
            velocity.y += gravity * gravityMultiplier * Time.deltaTime;
            ClampFallSpeed();
            
            controller.Move(velocity * Time.deltaTime);
            
            SteerDash();
            RotatePlayerTowardDashDirection();

            // Check for ledge hang during dash – if detected, cancel dash and enter hang
            // We only check if not already hanging and if we're moving toward a wall (forward)
            if (!isHanging && IsMovingTowardWall())
            {
                // Temporarily store original positions for ledge detection
                // We'll call HandleLedgeDetection() to see if we should grab
                // But we need to avoid infinite recursion. We'll just run the detection logic manually.
                CheckAndGrabLedgeDuringDash();
            }

            if (Time.time >= dashEndTime && !isHanging) // don't end dash if we transitioned to hang
            {
                isDashing = false;
                TryActivateExitBoostOrBrake();
            }
            else if (isHanging)
            {
                // Dash is cancelled by ledge grab
                CancelDash();
            }
            return;
        }

        // --- Decay dash jump buffer ---
        if (hasQueuedJumpDuringDash)
        {
            dashJumpBufferTimer -= Time.deltaTime;
            if (dashJumpBufferTimer <= 0f)
                hasQueuedJumpDuringDash = false;
        }

        // --- Decay speed boost or reverse brake ---
        if (reverseBrakeTimer > 0f)
        {
            reverseBrakeTimer -= Time.deltaTime;
            currentSpeedMultiplier = 0f;
            if (reverseBrakeTimer <= 0f)
                currentSpeedMultiplier = 1f;
        }
        else if (speedMultiplierDecay > 0f)
        {
            if (!IsMovingForwardRelativeToDash())
            {
                currentSpeedMultiplier = 1f;
                speedMultiplierDecay = 0f;
            }
            else
            {
                speedMultiplierDecay -= Time.deltaTime;
                if (speedMultiplierDecay <= 0f)
                    currentSpeedMultiplier = 1f;
                else
                    currentSpeedMultiplier = Mathf.Lerp(1f, dashSpeed / speed, speedMultiplierDecay / dashExitBoostDecay);
            }
        }

        // --- Try to start a new dash ---
        if (dashAction.WasPressedThisFrame() && CanDash())
        {
            StartDash();
            return;
        }

        // --- Execute queued jump from dash ---
        if (hasQueuedJumpDuringDash && CanJumpNow())
        {
            PerformJump();
            hasQueuedJumpDuringDash = false;
            dashJumpBufferTimer = 0f;
        }

        // --- Normal movement ---
        if (isKnockedBack)
        {
            ApplyGravity();
            ClampFallSpeed();
            controller.Move(velocity * Time.deltaTime);
            velocity.x = 0f;
            velocity.z = 0f;
            return;
        }

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

    // Helper to check ledge grab during dash (copy of detection logic without side effects)
    private void CheckAndGrabLedgeDuringDash()
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
        {
            // Enter hang immediately, cancel dash
            EnterHang();
        }
    }

    private void CancelDash()
    {
        isDashing = false;
        // Reset any dash‑related timers
        speedMultiplierDecay = 0f;
        reverseBrakeTimer = 0f;
        currentSpeedMultiplier = 1f;
        // Optionally reset dash jump buffer
        hasQueuedJumpDuringDash = false;
        dashJumpBufferTimer = 0f;
    }

    // Helper to check if jump can be performed (grounded or coyote)
    private bool CanJumpNow()
    {
        return isGrounded || coyoteTimer > 0f;
    }

    private void PerformJump()
    {
        velocity.y = jumpForce;
        isJumping = true;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
    }

    // ─────────────────────────────────────────────
    //  Exit Boost or Reverse Brake
    // ─────────────────────────────────────────────
    private void TryActivateExitBoostOrBrake()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (moveInput.magnitude < 0.1f)
        {
            currentSpeedMultiplier = 1f;
            speedMultiplierDecay = 0f;
            reverseBrakeTimer = 0f;
            return;
        }

        float camYaw = cam.eulerAngles.y;
        Vector3 desiredDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        desiredDir.Normalize();

        float angle = Vector3.Angle(dashDirection, desiredDir);
        if (angle < 90f)   // forward or slight turn → boost
        {
            speedMultiplierDecay = dashExitBoostDecay;
            currentSpeedMultiplier = dashSpeed / speed;
            lastDashDirection = dashDirection;
            reverseBrakeTimer = 0f;
        }
        else               // hard turn (≥90°) → brake
        {
            reverseBrakeTimer = reverseBrakeDuration;
            currentSpeedMultiplier = 0f;
            speedMultiplierDecay = 0f;
        }
    }

    private bool IsMovingForwardRelativeToDash()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (moveInput.magnitude < 0.1f) return false;

        float camYaw = cam.eulerAngles.y;
        Vector3 desiredDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        desiredDir.Normalize();

        float angle = Vector3.Angle(lastDashDirection, desiredDir);
        return angle < 90f;
    }

    // ─────────────────────────────────────────────
    //  Dash Steering and Facing
    // ─────────────────────────────────────────────
    private void SteerDash()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        if (moveInput.magnitude < 0.1f) return;
        
        float camYaw = cam.eulerAngles.y;
        Vector3 desiredDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
        desiredDir.Normalize();
        
        float angle = Vector3.SignedAngle(dashDirection, desiredDir, Vector3.up);
        float maxTurn = dashSteeringSpeed * Time.deltaTime;
        float turnAngle = Mathf.Clamp(angle, -maxTurn, maxTurn);
        
        dashDirection = Quaternion.Euler(0f, turnAngle, 0f) * dashDirection;
        dashDirection.Normalize();
    }

    private void RotatePlayerTowardDashDirection()
    {
        if (dashDirection.magnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(dashDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            dashFacingRotationSpeed * Time.deltaTime
        );
    }

    // ─────────────────────────────────────────────
    //  Dash helpers
    // ─────────────────────────────────────────────
    private bool CanDash()
    {
        if (playerInventory == null) return false;
        if (!playerInventory.HasAbility(dashAbility)) return false;
        if (Time.time < lastDashTime + dashCooldown) return false;
        if (isDashing) return false;
        if (!isGrounded && dashedInAir) return false;
        return true;
    }

    private void StartDash()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 dashDir;

        if (moveInput.magnitude > 0.1f)
        {
            float camYaw = cam.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(moveInput.x, 0f, moveInput.y);
            dashDir = moveDir.normalized;
        }
        else
        {
            dashDir = transform.forward;
        }

        isDashing = true;
        dashStartTime = Time.time;
        dashDirection = dashDir;
        dashEndTime = Time.time + dashDuration;
        lastDashTime = Time.time;

        transform.rotation = Quaternion.LookRotation(dashDirection);

        if (!isGrounded)
            dashedInAir = true;

        if (isJumping) isJumping = false;
        if (isHanging) ExitHang();
        velocity.y = 0f;

        // Cancel any leftover boost/brake
        currentSpeedMultiplier = 1f;
        speedMultiplierDecay = 0f;
        reverseBrakeTimer = 0f;
        hasQueuedJumpDuringDash = false;
        dashJumpBufferTimer = 0f;
    }

    // ─────────────────────────────────────────────
    //  Grounding (resets air dash flag)
    // ─────────────────────────────────────────────
    private void HandleGrounding()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            dashedInAir = false;

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

        // Reset air dash allowance so you can dash again after hanging
        dashedInAir = false;

        // Record forward input state for fresh press detection
        wasMovingForwardLastFrame = IsMovingTowardWall();
        hasJustPressedForwardInHang = false;
    }

    private void ExitHang()
    {
        isHanging         = false;
        hangCooldownTimer = 0.3f;
        // Reset for next hang
        wasMovingForwardLastFrame = false;
        hasJustPressedForwardInHang = false;
    }

    // ─────────────────────────────────────────────
    //  Hang inputs (no buffering, only fresh press)
    // ─────────────────────────────────────────────
    private void HandleHang()
    {
        // Drop: pressing S (backward)
        if (IsMovingBackward())
        {
            ExitHang();
            velocity.y = -hangDropSpeed;
            controller.Move(velocity * Time.deltaTime);
            return;
        }

        // --- Fresh forward press detection ---
        bool currentlyMovingForward = IsMovingTowardWall();
        if (currentlyMovingForward && !wasMovingForwardLastFrame)
            hasJustPressedForwardInHang = true;
        wasMovingForwardLastFrame = currentlyMovingForward;

        // Vault via movement (only if we pressed forward while hanging)
        bool vaultViaMovement = hasJustPressedForwardInHang;
        bool vaultViaJump     = jumpAction.WasPressedThisFrame();

        if (vaultViaMovement || vaultViaJump)
        {
            // Clear any pending jump buffer to avoid double vault
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            vaultCooldownTimer = vaultLungeDelay;

            ExitHang();
            velocity.y = ledgeVaultForce;
            isVaultJump = true;
            controller.Move(velocity * Time.deltaTime);
            // Reset the flag so you can't vault again without a new press
            hasJustPressedForwardInHang = false;
        }
    }

    private bool IsMovingBackward()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.magnitude < 0.1f) return false;

        float camYaw = cam.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(input.x, 0f, input.y);
        moveDir.Normalize();

        return Vector3.Dot(moveDir, transform.forward) < -0.5f;
    }

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

        float effectiveSpeed = speed * currentSpeedMultiplier;

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
                controller.Move(moveDir.normalized * effectiveSpeed * Time.deltaTime);
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
            controller.Move(moveDir.normalized * effectiveSpeed * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    //  Jump Buffer (normal)
    // ─────────────────────────────────────────────
    private void HandleJumpBuffer()
    {
        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  Jump (normal)
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

        if (jumpAction.WasReleasedThisFrame() && isJumping && velocity.y > 0f && !isVaultJump)
            isJumping = false;
    }

    // ─────────────────────────────────────────────
    //  Gravity (normal)
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