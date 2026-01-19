using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // Player transform (root)
    public float followSpeed = 10f;
    public float rotationSpeed = 2f;
    
    [Header("Normal Camera Settings")]
    public Vector3 normalOffset = new Vector3(0.6f, 0f, -3.2f); // Relative to AimPivot
    public float normalFOV = 60f;
    public float normalMouseSensitivity = 1f; // Reduced for WebGL/browser compatibility
    
    [Header("Aim Camera Settings")]
    public Vector3 aimOffset = new Vector3(0.25f, 0f, -2.4f); // Relative to AimPivot (lower height, closer)
    public float aimFOV = 50f;
    public float aimMouseSensitivity = 0.75f; // Reduced for WebGL/browser compatibility
    
    [Header("Mouse Settings")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    public float aimMinVerticalAngle = -45f; // More upward range in aim mode
    public float aimMaxVerticalAngle = 75f;  // More upward range in aim mode
    
    [Header("Transition Settings")]
    public float transitionDuration = 0.1f; // 0.08-0.12s smooth transition
    
    private float currentX = 0f;
    private float currentY = 0f;
    private bool isAiming = false;
    private Camera cam;
    
    private Transform aimPivot; // Actual camera target (child of Player)
    private Vector3 targetOffset;
    private float targetFOV;
    private float currentSensitivity;
    
    private Vector3 currentOffset;
    private float currentFOV;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Find or create AimPivot on Player
        SetupAimPivot();
        
        // Initialize rotation based on current camera rotation
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
        
        // If camera is child of player, get initial yaw from parent
        if (transform.parent != null && transform.parent == target)
        {
            currentX = target.eulerAngles.y;
        }
        
        // Initialize camera settings
        if (cam != null)
        {
            currentFOV = normalFOV;
            cam.fieldOfView = normalFOV;
        }
        currentOffset = normalOffset;
        currentSensitivity = normalMouseSensitivity;
        targetOffset = normalOffset;
        targetFOV = normalFOV;
    }
    
    void SetupAimPivot()
    {
        if (target == null) return;
        
        // Look for existing AimPivot
        aimPivot = target.Find("AimPivot");
        
        if (aimPivot == null)
        {
            // Create AimPivot child on Player
            GameObject pivotObj = new GameObject("AimPivot");
            pivotObj.transform.SetParent(target);
            pivotObj.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            pivotObj.transform.localRotation = Quaternion.identity;
            aimPivot = pivotObj.transform;
            Debug.Log("ThirdPersonCamera: Created AimPivot at local position (0, 1.55, 0) on Player");
        }
    }
    
    void LateUpdate()
    {
        if (target == null || aimPivot == null) return;
        
        HandleAimInput();
        HandleMouseInput();
        UpdateCameraPosition();
        UpdatePlayerRotation();
        UpdateCameraCollision();
    }
    
    void HandleAimInput()
    {
        bool wasAiming = isAiming;
        isAiming = Input.GetMouseButton(1); // Right mouse button
        
        if (isAiming != wasAiming)
        {
            // Transition between normal and aim mode
            if (isAiming)
            {
                // Enter aim mode
                targetOffset = aimOffset;
                targetFOV = aimFOV;
                currentSensitivity = aimMouseSensitivity;
            }
            else
            {
                // Exit aim mode
                targetOffset = normalOffset;
                targetFOV = normalFOV;
                currentSensitivity = normalMouseSensitivity;
            }
        }
        
        // Smoothly lerp offset and FOV towards target values
        float lerpSpeed = 1f / transitionDuration; // Convert duration to speed
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, lerpSpeed * Time.deltaTime);
        
        if (cam != null)
        {
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, lerpSpeed * Time.deltaTime);
            cam.fieldOfView = currentFOV;
        }
    }
    
    void HandleMouseInput()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * currentSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * currentSensitivity;
        
        currentX += mouseX; // Yaw (horizontal rotation around player)
        currentY -= mouseY; // Pitch (vertical rotation)
        
        // Clamp vertical rotation - use different limits in aim mode for better upward viewing
        float minAngle = isAiming ? aimMinVerticalAngle : minVerticalAngle;
        float maxAngle = isAiming ? aimMaxVerticalAngle : maxVerticalAngle;
        currentY = Mathf.Clamp(currentY, minAngle, maxAngle);
    }
    
    void UpdateCameraPosition()
    {
        // AimPivot is the center - camera orbits around AimPivot
        // Calculate rotation (yaw around player + pitch up/down)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // Calculate desired position: AimPivot position + rotated offset
        // Offset is relative to AimPivot, so if Z is -2.4, it goes 2.4 units behind (backward)
        Vector3 desiredPosition = aimPivot.position + rotation * currentOffset;
        
        // Smoothly move camera to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        
        // Camera always looks at AimPivot (which is at proper height on player)
        transform.LookAt(aimPivot.position);
    }
    
    void UpdatePlayerRotation()
    {
        // Player rotates to match camera's yaw (horizontal rotation)
        if (target != null)
        {
            Quaternion playerRotation = Quaternion.Euler(0, currentX, 0);
            target.rotation = Quaternion.Slerp(target.rotation, playerRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void UpdateCameraCollision()
    {
        if (aimPivot == null) return;
        
        // Check for walls/obstacles between AimPivot and camera
        Vector3 pivotPos = aimPivot.position;
        Vector3 cameraPos = transform.position;
        Vector3 directionToCamera = cameraPos - pivotPos;
        float distance = directionToCamera.magnitude;
        
        if (distance < 0.01f) return; // Avoid division by zero
        
        RaycastHit hit;
        if (Physics.Raycast(pivotPos, directionToCamera.normalized, out hit, distance))
        {
            // Something is blocking the camera - move it closer to AimPivot
            transform.position = hit.point - directionToCamera.normalized * 0.2f; // Small offset to avoid clipping
        }
    }
    
    public float GetCurrentYRotation()
    {
        return currentX;
    }
    
    public bool IsAiming()
    {
        return isAiming;
    }
}
