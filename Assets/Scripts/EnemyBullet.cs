using UnityEngine;
using System.Collections;

public class EnemyBullet : MonoBehaviour
{
    private int damage = 10;
    private float lifetime = 2.5f;
    
    public void Initialize(int bulletDamage)
    {
        damage = bulletDamage;
        
        // Set up collider as trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            // Add SphereCollider if missing
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
        }
        
        // Start lifetime coroutine
        StartCoroutine(DestroyAfterLifetime());
    }
    
    IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if hit player by name
        if (other.name == "Player")
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                // Try parent
                playerHealth = other.GetComponentInParent<PlayerHealth>();
            }
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"Enemy bullet hit Player for {damage} damage");
            }
            Destroy(gameObject);
            return;
        }
        
        // Don't collide with enemies or enemy bullets
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        
        if ((enemyLayer != -1 && other.gameObject.layer == enemyLayer) ||
            other.name.StartsWith("Enemy") ||
            (enemyBulletLayer != -1 && other.gameObject.layer == enemyBulletLayer) ||
            other.name.StartsWith("EnemyBullet"))
        {
            return; // Don't destroy bullet, just ignore collision
        }
        
        // Hit ground/world geometry - destroy bullet
        if (!other.name.StartsWith("Enemy") && !other.name.StartsWith("EnemyBullet"))
        {
            Destroy(gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        
        // Don't collide with enemies or enemy bullets
        if ((enemyLayer != -1 && other.layer == enemyLayer) || other.name.StartsWith("Enemy") ||
            (enemyBulletLayer != -1 && other.layer == enemyBulletLayer) || other.name.StartsWith("EnemyBullet"))
        {
            return;
        }
        
        Destroy(gameObject);
    }
}
