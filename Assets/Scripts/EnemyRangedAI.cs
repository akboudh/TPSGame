using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyRangedAI : MonoBehaviour
{
    [Header("Ranged AI Settings")]
    [SerializeField] private float detectionRange = 25f;
    [SerializeField] private float shootRange = 10f; // Stop and shoot at this distance
    [SerializeField] private float resumeChaseRange = 14f; // Resume chasing if player moves farther
    [SerializeField] private float fireRate = 1.2f;
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 360f;
    
    [Header("Surround Settings")]
    [SerializeField] private float offsetRadius = 3.0f;
    
    private Transform playerTransform;
    private Transform muzzlePoint;
    private NavMeshAgent navAgent;
    private bool useNavMesh = false;
    private float lastShotTime = 0f;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
    // Unique orbit angle per enemy
    private float orbitAngle = 0f;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
        
        // Create or find muzzle point
        CreateMuzzlePoint();
        
        // Compute unique orbit angle
        int instanceHash = gameObject.GetInstanceID();
        int positionHash = (int)(transform.position.x * 100f) + (int)(transform.position.z * 100f);
        int combinedHash = instanceHash ^ positionHash;
        orbitAngle = (combinedHash % 360) * Mathf.Deg2Rad;
        
        // Setup NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            useNavMesh = true;
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = shootRange;
            navAgent.radius = 0.6f;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            navAgent.avoidancePriority = 30 + (instanceHash % 41);
        }
        
        // Find GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        // Setup layers
        SetupLayers();
    }
    
    void SetupLayers()
    {
        // Ensure Enemy layer exists and set this enemy to it
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer == -1)
        {
            // Layer doesn't exist, create it (this is just a fallback - should be set in Unity editor)
            Debug.LogWarning("Enemy layer not found. Please create 'Enemy' layer in Unity Editor.");
        }
        else
        {
            gameObject.layer = enemyLayer;
        }
    }
    
    void CreateMuzzlePoint()
    {
        // Find existing muzzle point
        muzzlePoint = transform.Find("Muzzle");
        
        if (muzzlePoint == null)
        {
            // Create muzzle point slightly in front of enemy
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0f, 1.2f, 0.8f); // Slightly forward and up
            muzzleObj.transform.localRotation = Quaternion.identity;
            muzzlePoint = muzzleObj.transform;
        }
    }
    
    void Update()
    {
        // Stop if game is over or level complete
        if (IsGameOverOrLevelComplete())
        {
            StopMoving();
            return;
        }
        
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Face player always when in range
        if (distanceToPlayer <= detectionRange)
        {
            FacePlayer();
        }
        
        // Check if player is within detection range
        if (distanceToPlayer <= detectionRange)
        {
            // If player is too far, resume chasing
            if (distanceToPlayer > resumeChaseRange)
            {
                ChasePlayer();
            }
            // If within shoot range, stop and shoot
            else if (distanceToPlayer <= shootRange)
            {
                StopMoving();
                
                // Check line of sight before shooting
                if (HasLineOfSight())
                {
                    Shoot();
                }
            }
            // Between shoot range and resume chase range, continue approaching
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            StopMoving();
        }
    }
    
    bool HasLineOfSight()
    {
        if (muzzlePoint == null || playerTransform == null) return false;
        
        Vector3 rayOrigin = muzzlePoint.position;
        Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;
        float maxDistance = Vector3.Distance(rayOrigin, playerTransform.position);
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance))
        {
            // Check if ray hit the player
            if (hit.collider.CompareTag("Player") || hit.collider.name == "Player")
            {
                return true;
            }
        }
        
        return false;
    }
    
    void FacePlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f;
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void ChasePlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        Vector3 offsetDirection = new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle));
        Vector3 targetPosition = playerPos + offsetDirection * offsetRadius;
        
        if (useNavMesh && navAgent != null)
        {
            if (navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(targetPosition);
            }
        }
        else
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            directionToTarget.y = 0f;
            transform.position += directionToTarget * moveSpeed * Time.deltaTime;
        }
    }
    
    void StopMoving()
    {
        if (useNavMesh && navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = true;
        }
    }
    
    void Shoot()
    {
        if (Time.time - lastShotTime < fireRate) return;
        if (muzzlePoint == null || playerTransform == null) return;
        
        lastShotTime = Time.time;
        
        // Create muzzle flash
        StartCoroutine(CreateMuzzleFlash());
        
        // Spawn bullet
        SpawnBullet();
    }
    
    void SpawnBullet()
    {
        if (muzzlePoint == null || playerTransform == null) return;
        
        // Create bullet
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "EnemyBullet";
        bullet.transform.position = muzzlePoint.position;
        bullet.transform.localScale = Vector3.one * 0.12f;
        
        // Set layer to EnemyBullet
        int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (bulletLayer != -1)
        {
            bullet.layer = bulletLayer;
        }
        
        // Add Rigidbody
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
        
        // Set velocity toward player
        Vector3 direction = (playerTransform.position - muzzlePoint.position).normalized;
        rb.linearVelocity = direction * 22f;
        
        // Add EnemyBullet component
        EnemyBullet bulletScript = bullet.AddComponent<EnemyBullet>();
        bulletScript.Initialize(bulletDamage);
    }
    
    IEnumerator CreateMuzzleFlash()
    {
        if (muzzlePoint == null) yield break;
        
        // Create small bright sphere for muzzle flash
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MuzzleFlash";
        flash.transform.position = muzzlePoint.position;
        flash.transform.localScale = Vector3.one * 0.1f;
        
        Renderer renderer = flash.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.9f, 0.3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2f, 1.8f, 0.6f));
            renderer.material = mat;
        }
        
        // Add light
        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.9f, 0.3f);
        flashLight.intensity = 2f;
        flashLight.range = 1f;
        
        // Remove collider
        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // Destroy after 0.05s
        yield return new WaitForSeconds(0.05f);
        if (flash != null)
        {
            Destroy(flash);
        }
    }
    
    bool IsGameOverOrLevelComplete()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
        }
        
        // Check if game is over by player health
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            return true;
        }
        
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }
        
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            return true;
        }
        
        return false;
    }
}