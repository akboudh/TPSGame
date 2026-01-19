using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private int attackDamage = 10;
    
    private Transform playerTransform;
    private Transform muzzleTransform;
    private NavMeshAgent navAgent;
    private Animator animator;
    private PlayerHealth playerHealth;
    private GameManager gameManager;
    
    private float lastAttackTime = 0f;
    private bool playerTagWarningLogged = false;
    private bool navMeshWarningLogged = false;
    
    void Start()
    {
        // Find player by tag or name
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.Find("Player");
        }
        
        if (playerObj == null && !playerTagWarningLogged)
        {
            Debug.LogError("EnemyAI: Player tag/name not found! Enemy will not work.");
            playerTagWarningLogged = true;
        }
        
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
        
        // Get NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            // Check if agent is on NavMesh
            if (!navAgent.isOnNavMesh && !navMeshWarningLogged)
            {
                Debug.LogWarning($"EnemyAI ({gameObject.name}): NavMeshAgent not on NavMesh. Bake NavMesh or ensure enemy starts on walkable area.");
                navMeshWarningLogged = true;
            }
            
            navAgent.speed = 3.5f;
            navAgent.stoppingDistance = attackRange;
        }
        
        // Get Animator (optional)
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Find or create muzzle point
        muzzleTransform = transform.Find("Muzzle");
        if (muzzleTransform == null)
        {
            GameObject muzzleObj = new GameObject("Muzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0f, 1.5f, 0.5f); // Slightly in front and up
            muzzleTransform = muzzleObj.transform;
        }
        
        // Find GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Face player
        FacePlayer();
        
        // Check line of sight before deciding to shoot
        bool hasLineOfSight = CheckLineOfSight();
        
        // Chase or attack
        if (distanceToPlayer <= attackRange && hasLineOfSight)
        {
            // Within attack range and has clear line of sight - stop and shoot
            StopMoving();
            
            // Shoot on cooldown
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                ShootAtPlayer();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            // Either too far, or blocked by obstacle - chase player (NavMesh will navigate around obstacles)
            ChasePlayer();
        }
        
        // Animator updated in ChasePlayer() method
    }
    
    void FacePlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        directionToPlayer.y = 0f; // Keep rotation on Y axis only
        
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }
    
    void ChasePlayer()
    {
        if (playerTransform == null) return;
        
        bool isMoving = false;
        
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(playerTransform.position);
            isMoving = navAgent.velocity.magnitude > 0.1f;
        }
        else if (navAgent != null)
        {
            // Fallback: simple movement (backup if no NavMesh)
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0f;
            transform.position += directionToPlayer * navAgent.speed * Time.deltaTime;
            isMoving = directionToPlayer.magnitude > 0.1f;
        }
        else
        {
            // No NavMeshAgent at all - use transform movement
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0f;
            transform.position += directionToPlayer * 3.5f * Time.deltaTime;
            isMoving = directionToPlayer.magnitude > 0.1f;
        }
        
        // Update animator with movement state
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }
    }
    
    void StopMoving()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
    }
    
    bool CheckLineOfSight()
    {
        if (playerTransform == null) return false;
        
        // Determine ray origin (use muzzle if available, otherwise transform forward)
        Vector3 rayOrigin = muzzleTransform != null ? muzzleTransform.position : transform.position + Vector3.up * 1.5f;
        Vector3 rayDirection = (playerTransform.position - rayOrigin).normalized;
        float maxDistance = Vector3.Distance(rayOrigin, playerTransform.position);
        
        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, maxDistance))
        {
            // Check if hit player directly (no obstacles in between)
            PlayerHealth hitPlayerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (hitPlayerHealth == null)
            {
                hitPlayerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            }
            
            // Also check by name/tag
            bool isPlayer = (hitPlayerHealth != null && hitPlayerHealth == playerHealth) ||
                           hit.collider.CompareTag("Player") ||
                           hit.collider.name == "Player" ||
                           hit.collider.transform.root.name == "Player";
            
            return isPlayer;
        }
        
        return false;
    }
    
    void ShootAtPlayer()
    {
        if (playerHealth == null) return;
        
        // Trigger shoot animation
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
        
        // Deal damage directly (we already checked line of sight before calling this)
        playerHealth.TakeDamage(attackDamage);
        Debug.Log($"{gameObject.name} shot Player for {attackDamage} damage");
    }
}
