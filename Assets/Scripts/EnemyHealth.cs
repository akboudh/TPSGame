using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Health Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int damagePerHit = 25;
    
    private int currentHealth;
    private Renderer enemyRenderer;
    private Color originalColor;
    private bool isFlashing = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Get the renderer component to change color (try root, then children)
        enemyRenderer = GetComponent<Renderer>();
        
        if (enemyRenderer == null)
        {
            // Try finding renderer in children
            enemyRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (enemyRenderer != null && enemyRenderer.material != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        // No warning - this is OK for Swat model, hit flash just won't work
    }
    
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Already dead
        
        currentHealth -= damage;
        
        // Log hit information
        int remainingHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"Hit {gameObject.name} for {damage} (remaining: {remainingHealth})");
        
        // Trigger hit flash
        if (!isFlashing)
        {
            StartCoroutine(HitFlash());
        }
        
        // Check if enemy died
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    System.Collections.IEnumerator HitFlash()
    {
        isFlashing = true;
        
        if (enemyRenderer != null && enemyRenderer.material != null)
        {
            // Change color to red
            enemyRenderer.material.color = Color.red;
            
            // Wait 0.1 seconds
            yield return new WaitForSeconds(0.1f);
            
            // Change color back to original
            enemyRenderer.material.color = originalColor;
        }
        
        isFlashing = false;
    }
    
    void Die()
    {
        Debug.Log($"{gameObject.name} died");
        
        // Notify GameManager that an enemy died
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager != null)
        {
            gameManager.OnEnemyDied();
        }
        
        // Destroy the enemy GameObject
        Destroy(gameObject);
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
