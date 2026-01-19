using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CrosshairSetup : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [SerializeField] private float size = 8f; // Size in pixels
    [SerializeField] private Color color = Color.white;
    
    private Image crosshairImage;
    private RectTransform rectTransform;
    
    void Start()
    {
        SetupCrosshair();
    }
    
    void SetupCrosshair()
    {
        // Get components
        crosshairImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        if (crosshairImage == null || rectTransform == null)
        {
            Debug.LogError("CrosshairSetup: Missing Image or RectTransform component!");
            return;
        }
        
        // Set anchors to center of screen (0.5, 0.5, 0.5, 0.5)
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Position at screen center (anchored position should be 0,0 when anchored to center)
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Set size (width and height)
        rectTransform.sizeDelta = new Vector2(size, size);
        
        // Set color to white
        crosshairImage.color = color;
        
        // Create a simple white sprite (or use a simple default sprite)
        // Unity's default white sprite should work
        if (crosshairImage.sprite == null)
        {
            // Use Unity's built-in white sprite texture
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            crosshairImage.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
