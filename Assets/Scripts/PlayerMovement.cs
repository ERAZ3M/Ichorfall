using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    
    public CharacterController controller;
    public Transform cam;

    public float speed = 6f;
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
    
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;

    Vector3 velocity;
    
    bool isGrounded;
    float jumpHoldTimer;
    bool isJumping;
    
    
    
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

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


        if (Input.GetButtonDown("Jump"))
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
        if (Input.GetButton("Jump") && isJumping && velocity.y > 0f)
        {
            if (jumpHoldTimer < maxJumpHoldTime)
            {
                velocity.y += jumpHoldForce * Time.deltaTime;
                jumpHoldTimer += Time.deltaTime;
            }
        }
        
        // Jump release
        if (Input.GetButtonUp("Jump") && isJumping)
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
