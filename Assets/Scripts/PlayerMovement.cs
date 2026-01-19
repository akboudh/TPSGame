using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    private CharacterController characterController;
    private Animator animator;
    private Vector3 moveDirection;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // Configure CharacterController to keep player on ground
        characterController.height = 2f;
        characterController.radius = 0.5f;
        
        // Find ground level by raycasting
        Vector3 currentPos = transform.position;
        RaycastHit hit;
        float groundY = 0f;
        
        if (Physics.Raycast(new Vector3(currentPos.x, currentPos.y + 5f, currentPos.z), Vector3.down, out hit, 20f))
        {
            groundY = hit.point.y;
        }
        
        // CharacterController calculation:
        // - Height = 2m
        // - Center offset: (0, 0, 0) means center is at transform position
        // - Controller extends from (center.y - height/2) to (center.y + height/2) relative to transform
        // - With center = (0, 0, 0) and height = 2:
        //   Controller bottom = transform.y - 1, top = transform.y + 1
        //   So if transform.y = groundY + 1, feet will be at groundY ✓
        
        transform.position = new Vector3(currentPos.x, groundY + 1f, currentPos.z);
        
        // Center at (0, 0, 0): controller extends from y=-1 to y=+1 relative to transform
        // So if transform.y = groundY + 1, bottom (feet) = groundY + 1 - 1 = groundY ✓
        characterController.center = new Vector3(0, 0f, 0);
        
        // Find Animator in children (on Swat model)
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.applyRootMotion = false; // Movement controlled by CharacterController
        }
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleMovement();
        HandleGravity();
    }
    
    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        
        // Calculate movement direction relative to player's forward direction
        moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.Normalize();
        moveDirection *= moveSpeed;
        
        // Update animator - check if moving
        float inputMagnitude = Mathf.Sqrt(horizontal * horizontal + vertical * vertical);
        if (animator != null)
        {
            animator.SetBool("IsMoving", inputMagnitude > 0.1f);
        }
        
        // Apply movement
        if (characterController != null)
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }
    }
    
    void HandleGravity()
    {
        if (characterController == null) return;
        
        if (characterController.isGrounded)
        {
            // When grounded, set velocity to 0 (CharacterController handles staying on ground)
            verticalVelocity = 0f;
        }
        else
        {
            // Apply gravity when not grounded
            verticalVelocity += gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, gravity * 2f); // Cap fall speed
        }
        
        // Only apply gravity movement if not grounded
        if (!characterController.isGrounded)
        {
            Vector3 gravityVector = new Vector3(0, verticalVelocity, 0);
            characterController.Move(gravityVector * Time.deltaTime);
        }
    }
    
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }
}
