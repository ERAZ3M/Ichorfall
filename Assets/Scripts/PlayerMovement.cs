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
    [SerializeField] private float speed = 6f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    // ─────────────────────────────────────────────
    //  Jump
    //
    //  jumpHeight   → apex height in world units.
    //  timeToApex   → seconds to reach apex. Higher = floatier rise.
    //  fallGravityMultiplier → 1 = same speed down as up.
    //  lowJumpMultiplier → extra gravity on tap (short hop).
    //  apexThreshold → speed window (m/s) at apex for hangtime. Zero = off.
    //  apexGravityScale → gravity scale at apex. 1 = no effect.
    // ─────────────────────────────────────────────
    [Header("Jump — Core")]
    [Tooltip("Apex height in world units on a full-hold jump.")]
    [SerializeField] private float jumpHeight = 3.5f;

    [Tooltip("Seconds to reach apex. Higher = floatier rise.")]
    [SerializeField] private float timeToApex = 0.5f;

    [Header("Jump — Feel")]
    [Tooltip("Fall gravity vs rise gravity. 1 = symmetric.")]
    [SerializeField] private float fallGravityMultiplier = 1f;

    [Tooltip("Extra gravity when button released mid-rise (short hop).")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Tooltip("Vertical speed window (m/s) for apex smoothing. Zero = off.")]
    [SerializeField] private float apexThreshold = 1f;

    [Tooltip("Gravity scale at the apex. 1 = no effect, 0 = full float.")]
    [Range(0f, 1f)]
    [SerializeField] private float apexGravityScale = 0.75f;

    [Tooltip("Maximum downward speed.")]
    [SerializeField] private float maxFallSpeed = -25f;

    // ─────────────────────────────────────────────
    //  Ledge Hang
    //
    //  How detection works:
    //   • Ray A (wall ray) fires forward from the player's center.
    //     It must HIT — confirming there is a wall in front.
    //   • Ray B (clear ray) fires forward from ledgeCheckHeight
    //     above the player's center. It must MISS — confirming
    //     there is open space above the wall top (i.e. a platform edge).
    //   • Both conditions together = a grabbable ledge.
    //   • Only triggers while airborne and not falling fast.
    //
    //  Tuning:
    //   ledgeReachDistance → how far the rays reach forward.
    //                        Match to roughly your character radius + a bit.
    //   ledgeCheckHeight   → height above center for Ray B.
    //                        Set to roughly your character's half-height.
    //   hangDropSpeed      → downward speed applied on S to exit.
    //   ledgeVaultForce    → tiny upward impulse on W/Space.
    //                        Player then steers onto the platform normally.
    // ─────────────────────────────────────────────
    [Header("Ledge Hang")]
    [Tooltip("How far forward the wall and clear rays reach.")]
    [SerializeField] private float ledgeReachDistance = 0.65f;

    [Tooltip("Height above player center for the 'clear above wall' ray. ~half character height.")]
    [SerializeField] private float ledgeCheckHeight = 1.5f;

    [Tooltip("Which layers count as climbable geometry.")]
    [SerializeField] private LayerMask ledgeLayerMask = ~0;

    [Tooltip("Downward speed when S is held during hang (drop off ledge).")]
    [SerializeField] private float hangDropSpeed = 2f;

    [Tooltip("Upward impulse on W/Space during hang. Keep small — player steers the rest.")]
    [SerializeField] private float ledgeVaultForce = 13f;
    
    [Tooltip("How far below the ledge top the character snaps when grabbing. " +
             "Increase until the character's hands sit at platform level rather than their body center. " +
             "Typically set to around half your character's height.")]
    [SerializeField] private float hangSnapOffset = 1.5f;

    [Tooltip("How close (in metres) the player's pivot must be to the target hang Y before the hang activates. " +
             "Smaller = more precise, larger = more forgiving.")]
    [SerializeField] private float hangActivationTolerance = 0.15f;

    // ─────────────────────────────────────────────
    //  Lock-On (set by external system)
    // ─────────────────────────────────────────────
    [HideInInspector] public bool isLockedOn;
    [HideInInspector] public Transform lockOnTarget;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isHanging;

    // Derived physics (from jumpHeight + timeToApex)
    private float gravity;
    private float jumpForce;


    // Prevents immediately re-grabbing the ledge after vaulting/dropping
    private float hangCooldownTimer;

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

    private void OnValidate() => RecalculateJumpPhysics();

    /// <summary>
    /// Derives gravity and jump impulse from designer-friendly values.
    /// Physics: v0 = 2h/t   g = 2h/t²
    /// </summary>
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
        HandleGrounding();

        if (isHanging)
        {
            // While hanging, only process hang inputs — everything else is frozen
            HandleHang();
            return;
        }

        hangCooldownTimer -= Time.deltaTime;

        HandleMovement();
        HandleJumpBuffer();
        HandleJump();
        HandleLedgeDetection();
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
                velocity.y = -2f; // small negative keeps CharacterController grounded
                isJumping  = false;
            }

            // Landing always cancels a hang (safety fallback)
            if (isHanging) ExitHang();
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    // ─────────────────────────────────────────────
    //  Ledge Detection
    //
    //  Ray A: from center forward — must HIT a wall.
    //  Ray B: from (center + ledgeCheckHeight up) forward — must MISS.
    //  Only active while airborne and not falling fast (velocity.y > -1).
    // ─────────────────────────────────────────────
    private void HandleLedgeDetection()
    {
        if (isGrounded) return;
        if (hangCooldownTimer > 0f) return; // still cooling down from last hang

        Vector3 origin      = transform.position;
        Vector3 aboveOrigin = origin + Vector3.up * ledgeCheckHeight;
        Vector3 forward     = transform.forward;

        RaycastHit wallHitInfo;
        bool wallHit  = Physics.Raycast(origin,       forward, out wallHitInfo, ledgeReachDistance, ledgeLayerMask);
        bool clearTop = !Physics.Raycast(aboveOrigin, forward, ledgeReachDistance, ledgeLayerMask);

        if (!wallHit || !clearTop) return;

        // Find the exact ledge surface Y via a downward ray from above the edge.
        Vector3 downRayOrigin = aboveOrigin + forward * ledgeReachDistance;
        RaycastHit surfaceHit;
        float ledgeSurfaceY;
        if (Physics.Raycast(downRayOrigin, Vector3.down, out surfaceHit, ledgeCheckHeight + 0.5f, ledgeLayerMask))
            ledgeSurfaceY = surfaceHit.point.y;
        else
            ledgeSurfaceY = wallHitInfo.point.y; // fallback

        // The Y the player's pivot needs to be at for hands to sit on the ledge.
        float targetY = ledgeSurfaceY - hangSnapOffset;

        // Only activate once the player has naturally drifted within tolerance of
        // the hang position — no snapping, it just locks in when they arrive there.
        // Also requires the player to be falling or stationary, not rising.
        if (Mathf.Abs(transform.position.y - targetY) <= hangActivationTolerance && velocity.y <= 0f)
            EnterHang();
    }

    private void EnterHang()
    {
        isHanging = true;
        isJumping = false;
        velocity  = Vector3.zero; // freeze in place — player is already at the right Y
    }

    private void ExitHang()
    {
        isHanging         = false;
        hangCooldownTimer = 0.2f; // window to clear the wall before re-grabbing is allowed
    }

    // ─────────────────────────────────────────────
    //  Hang State — inputs while frozen on ledge
    //
    //  Uses WasPressedThisFrame for both actions so that
    //  any input held at the moment of grabbing is ignored —
    //  the player must re-press to act.
    //
    //  S pressed  → drop off, slide down.
    //  W pressed or Space pressed → tiny upward hop.
    //  No input   → stay frozen.
    // ─────────────────────────────────────────────
    private void HandleHang()
    {
        // WasPressedThisFrame means held inputs from before the grab are ignored.
        // The player must physically re-press the button to act.
        bool dropPressed  = moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().y < -0.5f;
        bool vaultPressed = (moveAction.WasPressedThisFrame() && moveAction.ReadValue<Vector2>().y > 0.5f)
                         || jumpAction.WasPressedThisFrame();

        if (dropPressed)
        {
            ExitHang();
            velocity.y = -hangDropSpeed;
            controller.Move(velocity * Time.deltaTime);
        }
        else if (vaultPressed)
        {
            ExitHang();
            velocity.y = ledgeVaultForce;
            isJumping  = true;
            controller.Move(velocity * Time.deltaTime);
        }
        // else: frozen — do nothing this frame
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

        if (jumpAction.WasReleasedThisFrame() && isJumping && velocity.y > 0f)
            isJumping = false;
    }

    // ─────────────────────────────────────────────
    //  Gravity
    //   1. Apex  → reduced gravity (apexGravityScale)
    //   2. Fall  → fallGravityMultiplier
    //   3. Rise + button released → lowJumpMultiplier (short hop)
    //   4. Rise + button held → normal
    // ─────────────────────────────────────────────
    private void ApplyGravity()
    {
        bool atApex = Mathf.Abs(velocity.y) < apexThreshold && !isGrounded;

        if (atApex)
            velocity.y += gravity * apexGravityScale * Time.deltaTime;
        else if (velocity.y < 0f)
            velocity.y += gravity * fallGravityMultiplier * Time.deltaTime;
        else if (!isJumping && velocity.y > 0f)
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
    //  Gizmos — Scene view visualisation of ledge rays
    //  Yellow = wall ray (must hit)
    //  Cyan   = clear ray (must miss)
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