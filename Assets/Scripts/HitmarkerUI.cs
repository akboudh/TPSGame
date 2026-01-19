using UnityEngine;
using UnityEngine.UI;

public class HitmarkerUI : MonoBehaviour
{
    [Header("Hitmarker Settings")]
    [SerializeField] private float displayDuration = 0.08f;
    [SerializeField] private Color hitmarkerColor = Color.white;
    [SerializeField] private float lineLength = 12f;
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private float gapSize = 4f; // Gap between lines
    
    private Canvas canvas;
    private GameObject hitmarkerObject;
    private RectTransform hitmarkerRect;
    private bool isShowing = false;
    private float showTimer = 0f;
    
    void Start()
    {
        // Find Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("HitmarkerUI: No Canvas found in scene!");
            return;
        }
        
        CreateHitmarker();
    }
    
    void CreateHitmarker()
    {
        // Create hitmarker GameObject as child of Canvas
        hitmarkerObject = new GameObject("Hitmarker");
        hitmarkerObject.transform.SetParent(canvas.transform, false);
        
        hitmarkerRect = hitmarkerObject.AddComponent<RectTransform>();
        
        // Center on screen
        hitmarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        hitmarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        hitmarkerRect.pivot = new Vector2(0.5f, 0.5f);
        hitmarkerRect.anchoredPosition = Vector2.zero;
        hitmarkerRect.sizeDelta = new Vector2(lineLength * 2, lineLength * 2);
        
        // Create 4 lines (X shape)
        CreateLine("Line_TL", new Vector2(-gapSize, gapSize), new Vector2(-lineLength, lineLength)); // Top-left
        CreateLine("Line_TR", new Vector2(gapSize, gapSize), new Vector2(lineLength, lineLength));   // Top-right
        CreateLine("Line_BL", new Vector2(-gapSize, -gapSize), new Vector2(-lineLength, -lineLength)); // Bottom-left
        CreateLine("Line_BR", new Vector2(gapSize, -gapSize), new Vector2(lineLength, -lineLength));   // Bottom-right
        
        // Initially hide
        hitmarkerObject.SetActive(false);
    }
    
    void CreateLine(string name, Vector2 startPos, Vector2 endPos)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(hitmarkerRect, false);
        
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        Image lineImage = lineObj.AddComponent<Image>();
        
        // Position and size
        Vector2 center = (startPos + endPos) * 0.5f;
        Vector2 direction = (endPos - startPos).normalized;
        float length = Vector2.Distance(startPos, endPos);
        
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = center;
        lineRect.sizeDelta = new Vector2(length, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        
        lineImage.color = hitmarkerColor;
        
        // Create white sprite
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        lineImage.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }
    
    void Update()
    {
        if (isShowing)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0f)
            {
                HideHitmarker();
            }
        }
    }
    
    public void ShowHitmarker()
    {
        if (hitmarkerObject != null)
        {
            hitmarkerObject.SetActive(true);
            isShowing = true;
            showTimer = displayDuration;
        }
    }
    
    void HideHitmarker()
    {
        if (hitmarkerObject != null)
        {
            hitmarkerObject.SetActive(false);
            isShowing = false;
        }
    }
}
