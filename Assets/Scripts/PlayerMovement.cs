using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private  Transform cam;
    
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    [Header("Movement")]
    [SerializeField] private  float speed = 6f;
    [SerializeField] private  float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    
    [Header("Jumping")]
    [SerializeField] private  float jumpStartForce = 5f;
    [SerializeField] private  float jumpHoldForce = 25;
    [SerializeField] private  float maxJumpHoldTime = 0.25f;
    [SerializeField] private  float gravity = -9.81f;
    [SerializeField] private  float fallGravityMultiplier = 1.5f;
    [SerializeField] private  float lowJumpMultiplier = 2f;
    [SerializeField] private  float maxFallSpeed = -25f;

    [Header("Coyote & Jump buffer")]
    [SerializeField] private  float coyoteTime = 0.15f;
    private float coyoteTimer;
    [SerializeField] private  float jumpBufferTime = 0.25f;
    private float jumpBufferTimer;

    Vector3 velocity;
    
    bool isGrounded;
    float jumpHoldTimer;
    bool isJumping;
    
    [HideInInspector] public bool isLockedOn;
    [HideInInspector] public Transform lockOnTarget;


    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
    }

    
    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }
    
    // Update is called once per frame
    void Update()
    {
        
        // Ground checking
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
        
            
        // Movement
        Vector2 input = moveAction.ReadValue<Vector2>();
        float horizontal = input.x;
        float vertical = input.y;
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (!isLockedOn)
        {

            if (direction.magnitude >= 0.1f)
            {
                
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
                float currentAngle = transform.eulerAngles.y;
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));
                float angle;

                if (angleDifference > 85f)
                {
                    angle = targetAngle;
                    turnSmoothVelocity = 0f;
                }
                else
                {
                    angle = Mathf.SmoothDampAngle(
                        currentAngle,
                        targetAngle,
                        ref turnSmoothVelocity,
                        turnSmoothTime
                    );
                }

                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                controller.Move(moveDir.normalized * speed * Time.deltaTime);
            }
            
        }
        
        else
        {
                // LOCK-ON ROTATION
                Vector3 toTarget = lockOnTarget.position - transform.position;
                toTarget.y = 0f;

                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );

                // Strafing movement relative to player forward
                Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
                controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }


        if (jumpAction.WasPressedThisFrame())
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        
        // JUMPING
        // Jump start
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpStartForce;
            isJumping = true;
            jumpHoldTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
        
        // Jump hold
        if (jumpAction.IsPressed() && isJumping && velocity.y > 0f)
        {
            if (jumpHoldTimer < maxJumpHoldTime)
            {
                velocity.y += jumpHoldForce * Time.deltaTime;
                jumpHoldTimer += Time.deltaTime;
            }
        }
        
        // Jump release
        if (jumpAction.WasReleasedThisFrame() && isJumping)
        {
            isJumping = false;
            
        }

        // Gravity
        if (velocity.y < 0) // Falling
        { velocity.y += gravity * fallGravityMultiplier * Time.deltaTime;
        
        } else if (velocity.y > 0 && !Input.GetButton("Jump")) // Jump released - faster drop
        { velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        
        } else
        { velocity.y += gravity * Time.deltaTime; }
        
        
        // Downward clamping
        if (velocity.y < maxFallSpeed)
        {
            velocity.y = maxFallSpeed;
        }
        
        
        controller.Move(velocity * Time.deltaTime);
        
    }
}
