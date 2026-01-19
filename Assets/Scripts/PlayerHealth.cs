using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    
    private int currentHealth;
    private GameManager gameManager;
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Cache GameManager reference - try multiple times in case Start() order is wrong
        CacheGameManager();
        
        // Notify GameManager of initial health (with delay to ensure UI is created)
        StartCoroutine(NotifyGameManagerDelayed());
    }
    
    void CacheGameManager()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
        }
    }
    
    System.Collections.IEnumerator NotifyGameManagerDelayed()
    {
        // Wait a frame to ensure GameManager.Start() has run and UI is created
        yield return null;
        
        CacheGameManager();
        
        if (gameManager != null)
        {
            gameManager.OnPlayerHealthChanged(currentHealth, maxHealth);
        }
    }
    
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // Already dead
        
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Player took {amount} damage. Health: {currentHealth}/{maxHealth}");
        
        // Ensure GameManager is cached
        CacheGameManager();
        
        // Notify GameManager of health change
        if (gameManager != null)
        {
            gameManager.OnPlayerHealthChanged(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerHealth: GameManager not found! Health bar won't update.");
        }
        
        // Check if player died
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Player died - Game Over!");
        
        // Trigger Game Over via GameManager
        if (gameManager != null)
        {
            gameManager.OnPlayerDied();
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (gameManager != null)
        {
            gameManager.OnPlayerHealthChanged(currentHealth, maxHealth);
        }
    }
}
