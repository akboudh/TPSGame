using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCombatAI : MonoBehaviour
{
    [Header("Distance Settings")]
    [SerializeField] private float minRange = 7f; // Too close => reposition backwards
    [SerializeField] private float preferredRange = 12f; // Target distance
    [SerializeField] private float maxRange = 20f; // Too far => chase
    
    [Header("Strafing Settings")]
    [SerializeField] private float strafeRadius = 5f;
    [SerializeField] private float strafeUpdateInterval = 0.25f; // Update destination ~4 times/sec
    
    [Header("Shooting Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstInterval = 0.12f;
    [SerializeField] private float burstCooldownMin = 0.9f;
    [SerializeField] private float burstCooldownMax = 1.4f;
    [SerializeField] private float spreadStanding = 1.0f;
    [SerializeField] private float spreadMoving = 3.0f;
    [SerializeField] private int bulletDamage = 10;
    
    [Header("LOS Settings")]
    [SerializeField] private float losCheckInterval = 0.1f;
    [SerializeField] private float losBlockedTimeout = 0.8f;
    
    [Header("Reposition Settings")]
    [SerializeField] private float repositionDistance = 4.5f; // 3-6 units away
    [SerializeField] private float repositionTimeout = 2f;
    
    private enum CombatState
    {
        Chase,
        CombatStrafe,
        Reposition,
        Dead
    }
    
    private CombatState currentState = CombatState.Chase;
    
    private Transform playerTransform;
    private Transform muzzlePoint;
    private NavMeshAgent navAgent;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
    // Movement
    private int strafeDirection = 1; // +1 or -1
    private float lastStrafeDirectionChange = 0f;
    private float strafeDirectionChangeInterval = 2f; // 1.5-3s randomized
    private float lastDestinationUpdate = 0f;
    
    // Shooting
    private bool isBursting = false;
    private float lastBurstEnd = 0f;
    private float nextBurstCooldown = 0f;
    
    // LOS
    private float losCheckTimer = 0f;
    private bool hasLineOfSight = true;
    private float losBlockedTime = 0f;
    
    // Reposition
    private Vector3 repositionTarget;
    private float repositionStartTime = 0f;
    
    // Unique offsets
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
        
        // Create muzzle point
        CreateMuzzlePoint();
        
        // Setup NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = 4f;
            navAgent.stoppingDistance = preferredRange - 1f;
            navAgent.radius = 0.65f;
            navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            
            // Random avoidance priority (30-70)
            int instanceHash = gameObject.GetInstanceID();
            navAgent.avoidancePriority = 30 + (instanceHash % 41);
            
            // Unique orbit angle
            orbitAngle = (instanceHash % 360) * Mathf.Deg2Rad;
        }
        
        // Find GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        // Initialize timers
        lastStrafeDirectionChange = Time.time;
        strafeDirectionChangeInterval = Random.Range(1.5f, 3f);
        nextBurstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
        
        // Setup layers
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            gameObject.layer = enemyLayer;
        }
    }
    
    void CreateMuzzlePoint()
    {
        muzzlePoint = transform.Find("Muzzle");
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0f, 1.2f, 0.8f);
            muzzleObj.transform.localRotation = Quaternion.identity;
            muzzlePoint = muzzleObj.transform;
        }
    }
    
    void Update()
    {
        // Stop if dead or game over
        if (currentState == CombatState.Dead || IsGameOverOrLevelComplete())
        {
            StopMoving();
            currentState = CombatState.Dead;
            return;
        }
        
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Always face player
        FacePlayer();
        
        // Update line of sight
        UpdateLineOfSight();
        
        // State machine
        switch (currentState)
        {
            case CombatState.Chase:
                UpdateChase(distanceToPlayer);
                break;
            case CombatState.CombatStrafe:
                UpdateCombatStrafe(distanceToPlayer);
                break;
            case CombatState.Reposition:
                UpdateReposition(distanceToPlayer);
                break;
        }
    }
    
    void UpdateChase(float distance)
    {
        // If within preferred range, switch to strafe
        if (distance <= preferredRange)
        {
            currentState = CombatState.CombatStrafe;
            return;
        }
        
        // Chase player
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            Vector3 playerPos = playerTransform.position;
            Vector3 offsetDirection = new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle));
            Vector3 targetPosition = playerPos + offsetDirection * 3f; // Small surround offset
            
            navAgent.isStopped = false;
            navAgent.SetDestination(targetPosition);
        }
    }
    
    void UpdateCombatStrafe(float distance)
    {
        // Check if too far -> chase
        if (distance > maxRange)
        {
            currentState = CombatState.Chase;
            return;
        }
        
        // Check if too close -> reposition
        if (distance < minRange)
        {
            StartReposition(true); // Reposition backwards
            return;
        }
        
        // Check if LOS blocked -> reposition
        if (!hasLineOfSight && losBlockedTime >= losBlockedTimeout)
        {
            StartReposition(false);
            return;
        }
        
        // Update strafe direction periodically
        if (Time.time - lastStrafeDirectionChange >= strafeDirectionChangeInterval)
        {
            strafeDirection = Random.value > 0.5f ? 1 : -1;
            lastStrafeDirectionChange = Time.time;
            strafeDirectionChangeInterval = Random.Range(1.5f, 3f);
        }
        
        // Update destination periodically (max 4 times/sec)
        if (Time.time - lastDestinationUpdate >= strafeUpdateInterval)
        {
            UpdateStrafeDestination(distance);
            lastDestinationUpdate = Time.time;
        }
        
        // Shoot if conditions met
        if (hasLineOfSight && distance >= minRange && distance <= maxRange)
        {
            TryShoot();
        }
    }
    
    void UpdateStrafeDestination(float distanceToPlayer)
    {
        if (navAgent == null || !navAgent.isActiveAndEnabled || playerTransform == null) return;
        
        Vector3 playerPos = playerTransform.position;
        Vector3 enemyPos = transform.position;
        Vector3 toPlayer = (playerPos - enemyPos).normalized;
        toPlayer.y = 0f;
        
        // Perpendicular direction for strafe
        Vector3 perpendicular = Vector3.Cross(toPlayer, Vector3.up).normalized * strafeDirection;
        
        // Distance adjustment to maintain preferredRange
        float distanceAdjustment = (distanceToPlayer - preferredRange) * 0.3f; // Move toward/away to maintain range
        Vector3 rangeAdjustment = -toPlayer * distanceAdjustment;
        
        // Target position: strafe + maintain range
        Vector3 targetPos = playerPos + perpendicular * strafeRadius + rangeAdjustment;
        
        // Sample NavMesh to ensure valid position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 3f, NavMesh.AllAreas))
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(hit.position);
        }
        else
        {
            // Fallback: just strafe without range adjustment
            targetPos = playerPos + perpendicular * strafeRadius;
            if (NavMesh.SamplePosition(targetPos, out hit, 3f, NavMesh.AllAreas))
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(hit.position);
            }
        }
    }
    
    void StartReposition(bool awayFromPlayer)
    {
        currentState = CombatState.Reposition;
        repositionStartTime = Time.time;
        
        if (playerTransform == null) return;
        
        Vector3 currentPos = transform.position;
        Vector3 playerPos = playerTransform.position;
        Vector3 direction = awayFromPlayer 
            ? (currentPos - playerPos).normalized 
            : (currentPos - playerPos + Vector3.Cross((currentPos - playerPos), Vector3.up).normalized * 2f).normalized;
        
        direction.y = 0f;
        
        // Pick reposition point: slightly sideways + away/toward
        Vector3 targetPos = currentPos + direction * repositionDistance;
        
        // Sample NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
        {
            repositionTarget = hit.position;
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = false;
                navAgent.SetDestination(repositionTarget);
            }
        }
        else
        {
            // Fallback: just move away from player
            targetPos = currentPos + direction * repositionDistance;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                repositionTarget = hit.position;
                if (navAgent != null && navAgent.isActiveAndEnabled)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(repositionTarget);
                }
            }
        }
    }
    
    void UpdateReposition(float distance)
    {
        // Timeout
        if (Time.time - repositionStartTime >= repositionTimeout)
        {
            currentState = CombatState.CombatStrafe;
            return;
        }
        
        // Check if reached destination
        if (navAgent != null && !navAgent.pathPending && navAgent.remainingDistance < 0.5f)
        {
            currentState = CombatState.CombatStrafe;
            return;
        }
        
        // Check if now at good distance
        if (distance >= minRange && distance <= maxRange && hasLineOfSight)
        {
            currentState = CombatState.CombatStrafe;
            return;
        }
    }
    
    void UpdateLineOfSight()
    {
        losCheckTimer += Time.deltaTime;
        if (losCheckTimer >= losCheckInterval)
        {
            losCheckTimer = 0f;
            
            if (muzzlePoint != null && playerTransform != null)
            {
                Vector3 rayOrigin = muzzlePoint.position;
                Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;
                float maxDistance = Vector3.Distance(rayOrigin, playerTransform.position);
                
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance))
                {
                    if (hit.collider.CompareTag("Player") || hit.collider.name == "Player")
                    {
                        hasLineOfSight = true;
                        losBlockedTime = 0f;
                    }
                    else
                    {
                        hasLineOfSight = false;
                        losBlockedTime += losCheckInterval;
                    }
                }
                else
                {
                    hasLineOfSight = false;
                    losBlockedTime += losCheckInterval;
                }
            }
        }
        else
        {
            if (!hasLineOfSight)
            {
                losBlockedTime += Time.deltaTime;
            }
        }
    }
    
    void FacePlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f;
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
        }
    }
    
    void TryShoot()
    {
        // Check burst cooldown
        if (Time.time - lastBurstEnd < nextBurstCooldown) return;
        if (isBursting) return;
        
        // Start burst
        StartCoroutine(BurstFire());
    }
    
    IEnumerator BurstFire()
    {
        isBursting = true;
        
        // Determine spread based on movement
        bool isMoving = navAgent != null && navAgent.velocity.magnitude > 0.1f;
        float spread = isMoving ? spreadMoving : spreadStanding;
        
        for (int i = 0; i < burstCount; i++)
        {
            if (playerTransform == null || muzzlePoint == null) break;
            
            // Fire bullet with spread
            FireBulletWithSpread(spread);
            
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }
        
        isBursting = false;
        lastBurstEnd = Time.time;
        nextBurstCooldown = Random.Range(burstCooldownMin, burstCooldownMax);
    }
    
    void FireBulletWithSpread(float spread)
    {
        if (muzzlePoint == null || playerTransform == null) return;
        
        // Base direction
        Vector3 baseDirection = (playerTransform.position - muzzlePoint.position).normalized;
        
        // Apply random spread
        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);
        Vector3 spreadDirection = Quaternion.Euler(spreadY, spreadX, 0f) * baseDirection;
        
        // Create bullet
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "EnemyBullet";
        bullet.transform.position = muzzlePoint.position;
        bullet.transform.localScale = Vector3.one * 0.12f;
        
        // Set layer
        int bulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (bulletLayer != -1)
        {
            bullet.layer = bulletLayer;
        }
        
        // Add Rigidbody
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
        rb.linearVelocity = spreadDirection * 22f;
        
        // Add EnemyBullet component
        EnemyBullet bulletScript = bullet.AddComponent<EnemyBullet>();
        bulletScript.Initialize(bulletDamage);
        
        // Create muzzle flash
        StartCoroutine(CreateMuzzleFlash());
    }
    
    IEnumerator CreateMuzzleFlash()
    {
        if (muzzlePoint == null) yield break;
        
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
        
        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.9f, 0.3f);
        flashLight.intensity = 2f;
        flashLight.range = 1f;
        
        Collider collider = flash.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        yield return new WaitForSeconds(0.05f);
        if (flash != null)
        {
            Destroy(flash);
        }
    }
    
    void StopMoving()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            navAgent.isStopped = true;
        }
    }
    
    bool IsGameOverOrLevelComplete()
    {
        // Check if player is dead
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