using UnityEngine;
using System.Collections;

public class ImpactEffect : MonoBehaviour
{
    [Header("Impact Effect Settings")]
    [SerializeField] private float effectDuration = 0.2f;
    [SerializeField] private float sphereSize = 0.1f;
    [SerializeField] private Color impactColor = new Color(1f, 0.5f, 0f, 1f); // Orange-ish
    
    public static void CreateImpact(Vector3 position, Vector3 normal)
    {
        GameObject impactObj = new GameObject("ImpactEffect");
        impactObj.transform.position = position;
        
        // Create small sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(impactObj.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.1f;
        
        // Set color
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = new Color(1f, 0.5f, 0f, 1f);
        }
        
        // Remove collider (we don't need it)
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        // Destroy after duration (use local constant)
        float duration = 0.2f;
        ImpactEffect effect = impactObj.AddComponent<ImpactEffect>();
        effect.StartCoroutine(effect.DestroyAfterDelay(duration));
    }
    
    IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
