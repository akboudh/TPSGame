using UnityEngine;
using UnityEngine.Rendering;

public class MaterialManager : MonoBehaviour
{
    private static Material playerMaterial;
    private static Material enemyMaterial;
    private static Material groundMaterial;
    private static Material coverMaterial;
    
    void Start()
    {
        // Create materials if not already created
        CreateMaterials();
        
        // Apply materials to existing objects
        ApplyMaterialsToScene();
    }
    
    public static void CreateMaterials()
    {
        // Get URP/Lit shader
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            // Fallback to standard Lit shader
            litShader = Shader.Find("Lit");
            if (litShader == null)
            {
                Debug.LogError("MaterialManager: Could not find URP/Lit or Lit shader!");
                return;
            }
        }
        
        // Create Player Material (olive drab)
        if (playerMaterial == null)
        {
            playerMaterial = new Material(litShader);
            playerMaterial.name = "Player_Material";
            ColorUtility.TryParseHtmlString("#556B2F", out Color playerColor);
            playerMaterial.SetColor("_BaseColor", playerColor);
            if (playerMaterial.HasProperty("_Color"))
                playerMaterial.SetColor("_Color", playerColor);
            if (playerMaterial.HasProperty("_Metallic"))
                playerMaterial.SetFloat("_Metallic", 0.05f);
            if (playerMaterial.HasProperty("_Smoothness"))
                playerMaterial.SetFloat("_Smoothness", 0.35f);
            Debug.Log("Created Player_Material");
        }
        
        // Create Enemy Material (dark muted red)
        if (enemyMaterial == null)
        {
            enemyMaterial = new Material(litShader);
            enemyMaterial.name = "Enemy_Material";
            ColorUtility.TryParseHtmlString("#7A2E2E", out Color enemyColor);
            enemyMaterial.SetColor("_BaseColor", enemyColor);
            if (enemyMaterial.HasProperty("_Color"))
                enemyMaterial.SetColor("_Color", enemyColor);
            if (enemyMaterial.HasProperty("_Metallic"))
                enemyMaterial.SetFloat("_Metallic", 0.05f);
            if (enemyMaterial.HasProperty("_Smoothness"))
                enemyMaterial.SetFloat("_Smoothness", 0.35f);
            Debug.Log("Created Enemy_Material");
        }
        
        // Create Ground Material (muddy brown)
        if (groundMaterial == null)
        {
            groundMaterial = new Material(litShader);
            groundMaterial.name = "Ground_Material";
            ColorUtility.TryParseHtmlString("#6B4423", out Color groundColor); // Muddy brown
            groundMaterial.SetColor("_BaseColor", groundColor);
            if (groundMaterial.HasProperty("_Color"))
                groundMaterial.SetColor("_Color", groundColor);
            if (groundMaterial.HasProperty("_Metallic"))
                groundMaterial.SetFloat("_Metallic", 0f);
            if (groundMaterial.HasProperty("_Smoothness"))
                groundMaterial.SetFloat("_Smoothness", 0.25f);
            Debug.Log("Created Ground_Material (muddy brown)");
        }
        
        // Create Cover Material (concrete grey)
        if (coverMaterial == null)
        {
            coverMaterial = new Material(litShader);
            coverMaterial.name = "Cover_Material";
            ColorUtility.TryParseHtmlString("#8A8A8A", out Color coverColor); // Concrete grey
            coverMaterial.SetColor("_BaseColor", coverColor);
            if (coverMaterial.HasProperty("_Color"))
                coverMaterial.SetColor("_Color", coverColor);
            if (coverMaterial.HasProperty("_Metallic"))
                coverMaterial.SetFloat("_Metallic", 0f);
            if (coverMaterial.HasProperty("_Smoothness"))
                coverMaterial.SetFloat("_Smoothness", 0.2f);
            Debug.Log("Created Cover_Material (concrete grey)");
        }
    }
    
    public static void ApplyMaterialsToScene()
    {
        // Apply to Player
        GameObject player = GameObject.Find("Player");
        if (player != null && playerMaterial != null)
        {
            ApplyMaterialToObject(player, playerMaterial);
            Debug.Log("Applied Player_Material to Player");
        }
        
        // Apply to Ground
        GameObject ground = GameObject.Find("Ground");
        if (ground != null && groundMaterial != null)
        {
            ApplyMaterialToObject(ground, groundMaterial);
            Debug.Log("Applied Ground_Material to Ground");
        }
        
        // Apply to existing enemies
        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null && enemyMaterial != null)
            {
                ApplyMaterialToObject(enemy.gameObject, enemyMaterial);
            }
        }
        
        // Cover blocks are applied in GameManager.CreateRandomCoverBlocks()
    }
    
    public static void ApplyMaterialToObject(GameObject obj, Material material)
    {
        if (obj == null || material == null) return;
        
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = material;
            }
        }
    }
    
    public static Material GetPlayerMaterial() => playerMaterial;
    public static Material GetEnemyMaterial() => enemyMaterial;
    public static Material GetGroundMaterial() => groundMaterial;
    public static Material GetCoverMaterial() => coverMaterial;
}
