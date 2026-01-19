using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int enemiesWave1 = 5;
    [SerializeField] private int enemiesPerWaveIncrease = 2; // How many more enemies per wave
    
    [Header("Cover Settings")]
    [SerializeField] private int coverBlockCount = 8;
    [SerializeField] private float playAreaSize = 45f; // Half-size of playable area (roughly -45 to +45)
    
    [Header("UI References")]
    [SerializeField] private Text enemyCountText;
    [SerializeField] private Text levelCompleteText;
    
    private int enemiesRemaining;
    private bool isLevelComplete = false;
    private bool isGameOver = false;
    
    // Wave system
    private int currentWave = 0;
    private int highestWave = 0;
    private bool isWaitingToStart = true;
    private PlayerMovement playerMovement;
    private PlayerShooting playerShooting;
    
    // Cover blocks tracking
    private List<GameObject> coverBlocks = new List<GameObject>();
    
    // Boundary walls tracking
    private List<GameObject> boundaryWalls = new List<GameObject>();
    
    // Game Over UI
    private GameObject gameOverPanel;
    private Text gameOverText;
    
    // Health Bar UI
    private GameObject healthBarPanel;
    private Text healthLabelText;
    private Image healthBarFill;
    private Image healthBarBackground;
    
    // Wave/Start UI
    private GameObject startScreenPanel;
    private Text startScreenText;
    private GameObject waveAnnouncePanel;
    private Text waveAnnounceText;
    
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Create and apply PBR materials
        MaterialManager.CreateMaterials();
        MaterialManager.ApplyMaterialsToScene();
        
        // Create boundary walls to prevent falling
        CreateBoundaryWalls();
        
        // Generate cover blocks first
        CreateRandomCoverBlocks();
        
        // Find player components
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerShooting = player.GetComponent<PlayerShooting>();
        }
        
        // Setup UI
        SetupUI();
        
        // Start in waiting state - show start screen
        ShowStartScreen();
        
        // Disable player controls until game starts
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
    }
    
    void Update()
    {
        // Check for start input
        if (isWaitingToStart && Input.GetKeyDown(KeyCode.S))
        {
            StartFirstWave();
        }
        
        // Check for restart input
        if ((isLevelComplete || isGameOver) && Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }
    
    void SetupUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("GameManager: No Canvas found for UI!");
            return;
        }
        
        // Create enemy count text if not assigned
        if (enemyCountText == null)
        {
            GameObject enemyCountObj = new GameObject("EnemyCountText");
            enemyCountObj.transform.SetParent(canvas.transform, false);
            
            enemyCountText = enemyCountObj.AddComponent<Text>();
            RectTransform rectTransform = enemyCountObj.GetComponent<RectTransform>();
            
            // Position at top-left
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(10f, -10f);
            rectTransform.sizeDelta = new Vector2(300f, 30f);
            
            enemyCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            enemyCountText.fontSize = 24;
            enemyCountText.color = Color.white;
            enemyCountText.alignment = TextAnchor.UpperLeft;
        }
        
        // Create level complete text if not assigned
        if (levelCompleteText == null)
        {
            GameObject levelCompleteObj = new GameObject("LevelCompleteText");
            levelCompleteObj.transform.SetParent(canvas.transform, false);
            
            levelCompleteText = levelCompleteObj.AddComponent<Text>();
            RectTransform rectTransform = levelCompleteObj.GetComponent<RectTransform>();
            
            // Center on screen
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(400f, 60f);
            
            levelCompleteText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelCompleteText.fontSize = 48;
            levelCompleteText.color = Color.yellow;
            levelCompleteText.alignment = TextAnchor.MiddleCenter;
            levelCompleteText.text = "Level Complete!\nPress R to Restart";
            levelCompleteObj.SetActive(false);
        }
        
        // Create Game Over UI
        CreateGameOverUI(canvas);
        
        // Create Health Bar UI
        CreateHealthBarUI(canvas);
        
        // Create Start Screen UI
        CreateStartScreenUI(canvas);
        
        // Create Wave Announce UI
        CreateWaveAnnounceUI(canvas);
    }
    
    void CreateStartScreenUI(Canvas canvas)
    {
        startScreenPanel = new GameObject("StartScreenPanel");
        startScreenPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = startScreenPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 150f);
        
        startScreenText = startScreenPanel.AddComponent<Text>();
        startScreenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        startScreenText.fontSize = 64;
        startScreenText.color = Color.white;
        startScreenText.alignment = TextAnchor.MiddleCenter;
        startScreenText.text = "READY?\nPress S to Start";
    }
    
    void CreateWaveAnnounceUI(Canvas canvas)
    {
        waveAnnouncePanel = new GameObject("WaveAnnouncePanel");
        waveAnnouncePanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = waveAnnouncePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 120f);
        
        waveAnnounceText = waveAnnouncePanel.AddComponent<Text>();
        waveAnnounceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        waveAnnounceText.fontSize = 72;
        waveAnnounceText.color = Color.yellow;
        waveAnnounceText.alignment = TextAnchor.MiddleCenter;
        
        waveAnnouncePanel.SetActive(false);
    }
    
    void CreateGameOverUI(Canvas canvas)
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 200f); // Increased size to fit all text
        
        gameOverText = gameOverPanel.AddComponent<Text>();
        gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameOverText.fontSize = 48;
        gameOverText.color = Color.red;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.text = "GAME OVER"; // Initial text, will be updated in OnPlayerDied
        
        gameOverPanel.SetActive(false);
    }
    
    void CreateHealthBarUI(Canvas canvas)
    {
        // Create health bar panel
        healthBarPanel = new GameObject("HealthBarPanel");
        healthBarPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = healthBarPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(10f, -50f);
        panelRect.sizeDelta = new Vector2(200f, 30f);
        
        // Create label
        GameObject labelObj = new GameObject("HealthLabel");
        labelObj.transform.SetParent(healthBarPanel.transform, false);
        
        healthLabelText = labelObj.AddComponent<Text>();
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;
        
        healthLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthLabelText.fontSize = 20;
        healthLabelText.color = Color.white;
        healthLabelText.alignment = TextAnchor.MiddleLeft;
        healthLabelText.text = "HP";
        
        // Create background
        GameObject bgObj = new GameObject("HealthBarBackground");
        bgObj.transform.SetParent(healthBarPanel.transform, false);
        
        healthBarBackground = bgObj.AddComponent<Image>();
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 1f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = new Vector2(35f, 0f);
        bgRect.sizeDelta = new Vector2(-45f, -4f);
        
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Get background width for fill size calculation (before creating fill)
        float bgWidth = 165f; // Default width
        if (bgRect.rect.width > 0)
        {
            bgWidth = bgRect.rect.width;
        }
        else if (bgRect.sizeDelta.x > 0)
        {
            bgWidth = bgRect.sizeDelta.x;
        }
        
        // Create fill
        GameObject fillObj = new GameObject("HealthBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        
        healthBarFill = fillObj.AddComponent<Image>();
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        
        // Anchor to left side (0,0 to 0,1) so it shrinks from right to left
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f); // Pivot on left side
        fillRect.anchoredPosition = Vector2.zero;
        
        fillRect.sizeDelta = new Vector2(bgWidth, 0f); // Full width initially
        
        // Use simple Image type (not Filled) - we'll control width manually via sizeDelta
        healthBarFill.type = Image.Type.Simple;
        healthBarFill.color = Color.red;
        
        // Initialize with player's current health
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                UpdateHealthBarUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }
    }
    
    void InitializeComponents()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerShooting = player.GetComponent<PlayerShooting>();
            
            // Ensure PlayerHealth is attached
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.AddComponent<PlayerHealth>();
            }
            
            // Apply player material (PBR) - done in MaterialManager
        }
    }
    
    void SpawnAdditionalEnemies(int count)
    {
        // Find player's Swat model to clone for enemies
        GameObject player = GameObject.Find("Player");
        GameObject playerSwatModel = null;
        
        if (player != null)
        {
            Transform swatTransform = player.transform.Find("Swat");
            if (swatTransform != null)
            {
                playerSwatModel = swatTransform.gameObject;
            }
            else
            {
                // Try finding any child with Animator
                Animator playerAnimator = player.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerSwatModel = playerAnimator.gameObject;
                }
            }
        }
        
        // Spawn additional enemies at random positions across the map
        for (int i = 0; i < count; i++)
        {
            Vector3 position = GetRandomSpawnPosition();
            
            GameObject enemy;
            if (playerSwatModel != null)
            {
                // Clone player's Swat model for enemy
                enemy = Instantiate(playerSwatModel);
                enemy.name = $"Enemy_Spawned_{i + 1}";
                enemy.transform.position = position;
                enemy.transform.localScale = Vector3.one;
                enemy.transform.SetParent(null);
                
                // Disable root motion on enemy animator
                Animator enemyAnimator = enemy.GetComponent<Animator>();
                if (enemyAnimator == null)
                {
                    enemyAnimator = enemy.GetComponentInChildren<Animator>();
                }
                if (enemyAnimator != null)
                {
                    enemyAnimator.applyRootMotion = false;
                }
            }
            else
            {
                // Fallback to capsule if Swat model not found
                enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = $"Enemy_Spawned_{i + 1}";
                enemy.transform.position = position;
            }
            
            // Ensure enemy has a collider for raycast hits (add to root if missing)
            Collider enemyCollider = enemy.GetComponent<Collider>();
            if (enemyCollider == null)
            {
                // Check if any child has collider
                Collider[] childColliders = enemy.GetComponentsInChildren<Collider>();
                if (childColliders == null || childColliders.Length == 0)
                {
                    // No collider found - add CapsuleCollider to root for raycast hits
                    CapsuleCollider capsuleCollider = enemy.AddComponent<CapsuleCollider>();
                    capsuleCollider.height = 2f;
                    capsuleCollider.radius = 0.5f;
                    capsuleCollider.center = new Vector3(0, 1f, 0); // Offset to match character center
                    Debug.Log($"GameManager: Added CapsuleCollider to {enemy.name} for raycast hits");
                }
            }
            
            // Add required components
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
            {
                try
                {
                    agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
                }
                catch
                {
                    // NavMeshAgent creation failed (no NavMesh baked) - this is OK, EnemyAI will use simple movement
                    Debug.LogWarning($"GameManager: NavMeshAgent creation failed for {enemy.name}. EnemyAI will use simple movement fallback.");
                }
            }
            
            // Configure NavMeshAgent if it exists
            if (agent != null)
            {
                agent.height = 2f;
                agent.radius = 0.5f;
                agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
            
            // Add EnemyHealth component to root enemy GameObject (always on root)
            if (enemy.GetComponent<EnemyHealth>() == null)
            {
                enemy.AddComponent<EnemyHealth>();
            }
            
            // Add EnemyAI component
            if (enemy.GetComponent<EnemyAI>() == null)
            {
                enemy.AddComponent<EnemyAI>();
            }
            
            // Apply enemy material (PBR)
            Material enemyMat = MaterialManager.GetEnemyMaterial();
            if (enemyMat != null)
            {
                MaterialManager.ApplyMaterialToObject(enemy, enemyMat);
            }
        }
    }
    
    public void OnEnemyDied()
    {
        if (isLevelComplete || isGameOver || isWaitingToStart) return;
        
        enemiesRemaining--;
        enemiesRemaining = Mathf.Max(0, enemiesRemaining);
        
        UpdateEnemyCountUI();
        
        if (enemiesRemaining <= 0)
        {
            CompleteWave();
        }
    }
    
    void UpdateEnemyCountUI()
    {
        if (enemyCountText != null)
        {
            if (currentWave > 0)
            {
                enemyCountText.text = $"Wave {currentWave} - Enemies left: {enemiesRemaining}";
            }
            else
            {
                enemyCountText.text = $"Enemies left: {enemiesRemaining}";
            }
        }
    }
    
    void CompleteWave()
    {
        // Restore player health
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }
        }
        
        // Start next wave after a short delay
        StartCoroutine(StartNextWaveDelayed());
    }
    
    System.Collections.IEnumerator StartNextWaveDelayed()
    {
        // Wait 2 seconds before starting next wave
        yield return new WaitForSeconds(2f);
        
        // Start next wave
        currentWave++;
        highestWave = Mathf.Max(highestWave, currentWave);
        
        StartWave(currentWave);
    }
    
    void ShowStartScreen()
    {
        isWaitingToStart = true;
        if (startScreenPanel != null)
        {
            startScreenPanel.SetActive(true);
        }
    }
    
    void StartFirstWave()
    {
        isWaitingToStart = false;
        currentWave = 1;
        highestWave = 1;
        
        // Hide start screen
        if (startScreenPanel != null)
        {
            startScreenPanel.SetActive(false);
        }
        
        // Enable player controls
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;
        
        // Start first wave
        StartWave(1);
    }
    
    void StartWave(int waveNumber)
    {
        // Calculate enemies for this wave: Wave 1 = 5, Wave 2 = 7, Wave 3+ = 5 + (wave-1)*2
        int enemyCount = waveNumber == 1 ? enemiesWave1 : enemiesWave1 + (waveNumber - 1) * enemiesPerWaveIncrease;
        
        // Show wave announcement
        if (waveAnnouncePanel != null && waveAnnounceText != null)
        {
            waveAnnounceText.text = $"WAVE {waveNumber}";
            waveAnnouncePanel.SetActive(true);
            
            // Hide after 2 seconds
            StartCoroutine(HideWaveAnnounceDelayed());
        }
        
        // Destroy any existing enemies
        DestroyAllEnemies();
        
        // Spawn enemies for this wave
        SpawnAdditionalEnemies(enemyCount);
        enemiesRemaining = enemyCount;
        
        UpdateEnemyCountUI();
    }
    
    System.Collections.IEnumerator HideWaveAnnounceDelayed()
    {
        yield return new WaitForSeconds(2f);
        if (waveAnnouncePanel != null)
        {
            waveAnnouncePanel.SetActive(false);
        }
    }
    
    public void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateHealthBarUI(currentHealth, maxHealth);
    }
    
    void UpdateHealthBarUI(int currentHealth, int maxHealth)
    {
        // Ensure UI is initialized - try to find it if reference is null
        if (healthBarFill == null)
        {
            // Try to find it if it wasn't created yet
            GameObject fillObj = GameObject.Find("HealthBarFill");
            if (fillObj != null)
            {
                healthBarFill = fillObj.GetComponent<Image>();
                Debug.Log("GameManager: Found healthBarFill via GameObject.Find");
            }
        }
        
        // If still null, try to find health bar panel and get fill from hierarchy
        if (healthBarFill == null)
        {
            if (healthBarPanel != null)
            {
                Transform fillTransform = healthBarPanel.transform.Find("HealthBarBackground/HealthBarFill");
                if (fillTransform != null)
                {
                    healthBarFill = fillTransform.GetComponent<Image>();
                    Debug.Log("GameManager: Found healthBarFill via hierarchy search");
                }
            }
        }
        
        if (healthBarFill != null && maxHealth > 0)
        {
            float fillPercent = Mathf.Clamp01((float)currentHealth / maxHealth);
            
            // Get the RectTransform to update width manually
            RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
            if (fillRect != null)
            {
                // Get full width from parent background
                float fullWidth = 165f; // Default fallback
                Transform parent = fillRect.parent;
                if (parent != null)
                {
                    RectTransform parentRect = parent.GetComponent<RectTransform>();
                    if (parentRect != null)
                    {
                        // Try rect.width first (actual rendered width)
                        if (parentRect.rect.width > 0)
                        {
                            fullWidth = parentRect.rect.width;
                        }
                        // Fallback to sizeDelta
                        else if (parentRect.sizeDelta.x > 0)
                        {
                            fullWidth = parentRect.sizeDelta.x;
                        }
                    }
                }
                
                // Update width based on health percentage
                float newWidth = fullWidth * fillPercent;
                fillRect.sizeDelta = new Vector2(newWidth, 0f);
                
                Debug.Log($"✓ Health Bar Updated: {currentHealth}/{maxHealth} = {fillPercent * 100f:F1}% (width={newWidth:F1}/{fullWidth:F1})");
            }
            else
            {
                Debug.LogWarning("Health bar fill RectTransform is null!");
            }
        }
        else
        {
            Debug.LogError($"✗ Health Bar Update Failed: healthBarFill={healthBarFill} (null={healthBarFill == null}), maxHealth={maxHealth}, healthBarPanel={healthBarPanel}");
        }
    }
    
    public void OnPlayerDied()
    {
        isGameOver = true;
        
        // Show Game Over UI with highest wave and score
        if (gameOverPanel != null && gameOverText != null)
        {
            gameOverText.text = $"GAME OVER\nHigh Score: {highestWave} Waves\nPress R to Restart";
            gameOverPanel.SetActive(true);
        }
        
        // Disable player movement and shooting
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        if (playerShooting != null)
        {
            playerShooting.enabled = false;
        }
        
        // Destroy all enemies when game over
        DestroyAllEnemies();
        
        Debug.Log($"Game Over! Highest Wave: {highestWave}");
    }
    
    void DestroyAllEnemies()
    {
        // Find all enemies in scene and destroy them
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        
        // Also stop any enemy AI scripts
        EnemyAI[] allEnemyAI = FindObjectsOfType<EnemyAI>();
        foreach (EnemyAI ai in allEnemyAI)
        {
            if (ai != null)
            {
                ai.enabled = false;
            }
        }
        
        Debug.Log($"Game Over: Destroyed {allEnemies.Length} enemies");
    }
    
    void RestartScene()
    {
        // Clean up cover blocks and walls before restart
        ClearCoverBlocks();
        ClearBoundaryWalls();
        
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void CreateBoundaryWalls()
    {
        // Clear any existing walls first
        ClearBoundaryWalls();
        
        float wallHeight = 3f;
        float wallThickness = 1f;
        float wallY = wallHeight * 0.5f; // Half height above ground
        
        // Create 4 walls around the boundary
        // North wall (positive Z)
        CreateWall("BoundaryWall_North", 
            new Vector3(0, wallY, playAreaSize + wallThickness * 0.5f), 
            new Vector3(playAreaSize * 2 + wallThickness, wallHeight, wallThickness));
        
        // South wall (negative Z)
        CreateWall("BoundaryWall_South", 
            new Vector3(0, wallY, -playAreaSize - wallThickness * 0.5f), 
            new Vector3(playAreaSize * 2 + wallThickness, wallHeight, wallThickness));
        
        // East wall (positive X)
        CreateWall("BoundaryWall_East", 
            new Vector3(playAreaSize + wallThickness * 0.5f, wallY, 0), 
            new Vector3(wallThickness, wallHeight, playAreaSize * 2 + wallThickness));
        
        // West wall (negative X)
        CreateWall("BoundaryWall_West", 
            new Vector3(-playAreaSize - wallThickness * 0.5f, wallY, 0), 
            new Vector3(wallThickness, wallHeight, playAreaSize * 2 + wallThickness));
        
        Debug.Log($"Created 4 boundary walls around play area ({playAreaSize * 2}x{playAreaSize * 2})");
    }
    
    void CreateWall(string name, Vector3 position, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = size;
        
        // Set as static
        wall.isStatic = true;
        
        // Ensure collider is solid
        Collider col = wall.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
        
        // Add kinematic Rigidbody to block movement
        Rigidbody rb = wall.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = wall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Use cover material (concrete grey) for walls
        Material wallMat = MaterialManager.GetCoverMaterial();
        if (wallMat != null)
        {
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = wallMat;
            }
        }
        
        boundaryWalls.Add(wall);
    }
    
    void ClearBoundaryWalls()
    {
        foreach (GameObject wall in boundaryWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }
        boundaryWalls.Clear();
    }
    
    void CreateRandomCoverBlocks()
    {
        // Clear any existing cover blocks first
        ClearCoverBlocks();
        
        // Create random cover blocks
        for (int i = 0; i < coverBlockCount; i++)
        {
            // Random size variation
            float width = Random.Range(1.5f, 4f);
            float height = Random.Range(2f, 4f);
            float depth = Random.Range(1.5f, 4f);
            
            // Random position within play area, avoiding center where player starts
            Vector3 position = Vector3.zero;
            int attempts = 0;
            do
            {
                float x = Random.Range(-playAreaSize, playAreaSize);
                float z = Random.Range(-playAreaSize, playAreaSize);
                
                // Avoid center area where player spawns
                if (Mathf.Abs(x) < 5f) x = x >= 0 ? 5f : -5f;
                if (Mathf.Abs(z) < 5f) z = z >= 0 ? 5f : -5f;
                
                position = new Vector3(x, height * 0.5f, z);
                attempts++;
            }
            while (IsPositionBlocked(position, width * 0.5f + 2f) && attempts < 20);
            
            // Create cover block
            GameObject coverBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coverBlock.name = $"CoverBlock_{i + 1}";
            coverBlock.transform.position = position;
            coverBlock.transform.localScale = new Vector3(width, height, depth);
            
            // Set as static for optimization
            coverBlock.isStatic = true;
            
            // Ensure it has a collider (CreatePrimitive already adds BoxCollider)
            // Make sure it's not a trigger so bullets and movement hit it
            Collider col = coverBlock.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false; // CRITICAL: Must not be a trigger to block movement
                
                // Add kinematic Rigidbody to ensure physics blocking works
                Rigidbody rb = coverBlock.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = coverBlock.AddComponent<Rigidbody>();
                    rb.isKinematic = true; // Static but still blocks physics-based movement
                    rb.useGravity = false; // Don't fall
                }
            }
            
            // Add NavMeshObstacle so enemies navigate around it
            UnityEngine.AI.NavMeshObstacle obstacle = coverBlock.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = coverBlock.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            }
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(width, height, depth); // Match the block size
            obstacle.center = Vector3.zero; // Center on the block
            obstacle.carving = true; // Carve out NavMesh so enemies avoid it
            obstacle.carveOnlyStationary = false; // Always carve even if moving (we set isKinematic)
            
            // Apply cover material (PBR)
            Material coverMat = MaterialManager.GetCoverMaterial();
            if (coverMat != null)
            {
                Renderer renderer = coverBlock.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = coverMat;
                }
            }
            
            coverBlocks.Add(coverBlock);
        }
        
        Debug.Log($"Created {coverBlockCount} cover blocks");
    }
    
    void ClearCoverBlocks()
    {
        foreach (GameObject block in coverBlocks)
        {
            if (block != null)
            {
                Destroy(block);
            }
        }
        coverBlocks.Clear();
    }
    
    bool IsPositionBlocked(Vector3 position, float radius)
    {
        // Check if position overlaps with existing cover blocks
        Collider[] overlaps = Physics.OverlapSphere(position, radius);
        foreach (Collider col in overlaps)
        {
            if (col.name.StartsWith("CoverBlock"))
            {
                return true;
            }
        }
        return false;
    }
    
    Vector3 GetRandomSpawnPosition()
    {
        // Find a random spawn position that doesn't overlap with cover blocks
        Vector3 position = Vector3.zero;
        int attempts = 0;
        float spawnRadius = 2f; // Radius to check around spawn point
        
        do
        {
            // Random position across the map
            float x = Random.Range(-playAreaSize + 5f, playAreaSize - 5f);
            float z = Random.Range(-playAreaSize + 5f, playAreaSize - 5f);
            
            // Avoid center area where player spawns
            if (Mathf.Abs(x) < 8f) x = x >= 0 ? 8f : -8f;
            if (Mathf.Abs(z) < 8f) z = z >= 0 ? 8f : -8f;
            
            // Raycast to find ground height
            float y = GetGroundHeight(x, z);
            position = new Vector3(x, y, z);
            attempts++;
        }
        while (IsPositionBlocked(position, spawnRadius) && attempts < 50);
        
        // If we couldn't find a clear spot after 50 attempts, use the last position anyway
        // (shouldn't happen with reasonable cover count)
        return position;
    }
    
    float GetGroundHeight(float x, float z)
    {
        // Raycast down from above to find ground
        Vector3 rayStart = new Vector3(x, 10f, z); // Start high above
        RaycastHit hit;
        int groundLayer = LayerMask.GetMask("Default");
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, groundLayer))
        {
            // Return position at ground level (CharacterController will handle offset)
            return hit.point.y;
        }
        
        // Fallback: assume ground is at Y=0
        return 0f;
    }
    
    void ChangeModelColor(GameObject model, Color color)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                Material mat = new Material(renderer.material);
                mat.color = color;
                renderer.material = mat;
            }
        }
    }
    
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
