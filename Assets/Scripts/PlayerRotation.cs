using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;
    
    private ThirdPersonCamera thirdPersonCamera;
    private Camera playerCamera;
    
    void Start()
    {
        // Find ThirdPersonCamera to get yaw rotation
        thirdPersonCamera = FindObjectOfType<ThirdPersonCamera>();
        
        // Fallback to camera
        if (thirdPersonCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
    }
    
    void Update()
    {
        RotateTowardCameraDirection();
    }
    
    void RotateTowardCameraDirection()
    {
        if (thirdPersonCamera != null)
        {
            // Use ThirdPersonCamera's yaw rotation
            float cameraYaw = thirdPersonCamera.GetCurrentYRotation();
            Quaternion targetRotation = Quaternion.Euler(0, cameraYaw, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (playerCamera != null)
        {
            // Fallback: use camera forward direction
            Vector3 cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0f; // Keep rotation only on Y axis
            cameraForward.Normalize();
            
            if (cameraForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
