using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private LayerMask shootableLayers = -1; // All layers by default
    [SerializeField] private bool showDebugRays = true;
    
    private Camera playerCamera;
    private Animator animator;
    
    void Start()
    {
        // Find Main Camera
        playerCamera = Camera.main;
        
        if (playerCamera == null)
        {
            // Fallback: find camera by name or tag
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj != null)
            {
                playerCamera = cameraObj.GetComponent<Camera>();
            }
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerShooting: Camera not found! Cannot shoot.");
        }
        
        // Find Animator for shooting animation
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.GetComponentInChildren<Animator>();
            }
        }
    }
    
    void Update()
    {
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            Fire();
        }
    }
    
    void Fire()
    {
        if (playerCamera == null) return;
        
        // Trigger shoot animation
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
        
        // Get camera position and forward direction
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        
        // Perform raycast
        RaycastHit hit;
        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, maxRange, shootableLayers);
        
        if (didHit)
        {
            // Hit something - draw green debug ray
            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.green, 1f);
            }
            
            // Check if we hit an enemy - search more thoroughly
            // For Swat model: collider might be on child, EnemyHealth on root
            EnemyHealth enemyHealth = null;
            
            // Method 1: Try on hit object directly
            enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            
            // Method 2: Walk up parent hierarchy until we find EnemyHealth or reach root
            if (enemyHealth == null)
            {
                Transform current = hit.collider.transform;
                int depth = 0;
                while (current != null && depth < 10) // Safety limit
                {
                    enemyHealth = current.GetComponent<EnemyHealth>();
                    if (enemyHealth != null) break;
                    current = current.parent;
                    depth++;
                }
            }
            
            // Method 3: Try root transform
            if (enemyHealth == null)
            {
                GameObject rootObj = hit.collider.transform.root.gameObject;
                if (rootObj != null)
                {
                    enemyHealth = rootObj.GetComponent<EnemyHealth>();
                }
            }
            
            // Method 4: Find all enemies and check if hit collider belongs to any enemy
            if (enemyHealth == null)
            {
                EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
                foreach (EnemyHealth eh in allEnemies)
                {
                    if (eh == null || eh.gameObject == null) continue;
                    
                    // Check if hit collider is part of this enemy's hierarchy
                    Collider[] enemyColliders = eh.GetComponentsInChildren<Collider>();
                    foreach (Collider col in enemyColliders)
                    {
                        if (col == hit.collider)
                        {
                            enemyHealth = eh;
                            break;
                        }
                    }
                    if (enemyHealth != null) break;
                }
            }
            
            if (enemyHealth != null)
            {
                // Enemy hit - deal damage
                enemyHealth.TakeDamage(25);
                Debug.Log($"Player shot {hit.collider.name} -> Found EnemyHealth on {enemyHealth.gameObject.name} - Enemy took 25 damage! (Health now: {enemyHealth.GetCurrentHealth()})");
            }
            else
            {
                // Log what was hit for debugging
                string parentName = hit.collider.transform.parent != null ? hit.collider.transform.parent.name : "none";
                string rootName = hit.collider.transform.root.name;
                Debug.Log($"Shot hit: {hit.collider.name} (parent: {parentName}, root: {rootName}) - No EnemyHealth found");
            }
        }
        else
        {
            // Missed - draw red debug ray to max range
            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, rayDirection * maxRange, Color.red, 1f);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        // Optional: Visualize in Scene view when not playing
        if (playerCamera != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxRange);
        }
    }
}
