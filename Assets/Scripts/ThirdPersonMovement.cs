using System;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public CharacterController controller;
    public Transform cam;

    [Header("Movement")]
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    [Header("Gravity & Jump")]
    public float gravity = -9.81f;
    public float fallGravityMultiplier = 1.5f;
    public float lowJumpMultiplier = 2f;

    public float jumpStartForce = 5f;
    public float jumpHoldForce = 25;
    public float maxJumpHoldTime = 0.25f;
    public float maxFallSpeed = -25f;

    public float coyoteTime = 0.15f;
    float coyoteTimer = 0f;

    public float jumpBufferTime = 0.25f;
    float jumpBufferTimer = 0f;

    [Header("Grapple")]
    public float grappleMaxDistance = 30f;
    public float grapplePullSpeed = 18f;
    public LayerMask grappleLayer;

    Vector3 grapplePoint;

    Vector3 velocity;

    bool isGrounded;
    float jumpHoldTimer;
    bool isJumping;

    enum MovementState
    {
        Normal,
        Grappling
    }

    MovementState currentState = MovementState.Normal;

    void Update()
    {
        // =========================
        // INPUT: GRAPPLE START
        // =========================
        if (Input.GetMouseButtonDown(1) && currentState == MovementState.Normal)
        {
            TryStartGrapple();
        }

        // =========================
        // GRAPPLE MODE
        // =========================
        if (currentState == MovementState.Grappling)
        {
            HandleGrappleMovement();

            // ONLY end when right click released
            if (Input.GetMouseButtonUp(1))
            {
                currentState = MovementState.Normal;
            }

            return; // Skip normal movement completely
        }

        // =========================
        // GROUND CHECK
        // =========================
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            if (velocity.y < 0)
            {
                velocity.y = -2f;
                isJumping = false;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // =========================
        // MOVEMENT
        // =========================
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }

        // =========================
        // JUMP BUFFER
        // =========================
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // =========================
        // JUMP START
        // =========================
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpStartForce;
            isJumping = true;
            jumpHoldTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        // =========================
        // JUMP HOLD
        // =========================
        if (Input.GetButton("Jump") && isJumping && velocity.y > 0f)
        {
            if (jumpHoldTimer < maxJumpHoldTime)
            {
                velocity.y += jumpHoldForce * Time.deltaTime;
                jumpHoldTimer += Time.deltaTime;
            }
        }

        // =========================
        // JUMP RELEASE
        // =========================
        if (Input.GetButtonUp("Jump") && isJumping)
        {
            isJumping = false;
        }

        // =========================
        // GRAVITY
        // =========================
        if (velocity.y < 0)
        {
            velocity.y += gravity * fallGravityMultiplier * Time.deltaTime;
        }
        else if (velocity.y > 0 && !Input.GetButton("Jump"))
        {
            velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        if (velocity.y < maxFallSpeed)
        {
            velocity.y = maxFallSpeed;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    // =========================
    // GRAPPLE FUNCTIONS
    // =========================
    void TryStartGrapple()
    {
        RaycastHit hit;

        if (Physics.Raycast(cam.position, cam.forward, out hit, grappleMaxDistance, grappleLayer))
        {
            grapplePoint = hit.point;
            velocity = Vector3.zero; // Cancel vertical motion
            currentState = MovementState.Grappling;
        }
    }

    void HandleGrappleMovement()
    {
        Vector3 direction = (grapplePoint - transform.position).normalized;
        controller.Move(direction * grapplePullSpeed * Time.deltaTime);
    }
}
