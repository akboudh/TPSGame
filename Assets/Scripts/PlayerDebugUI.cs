using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDebugUI : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public TextMeshProUGUI debugText;
    
    [Header("Update Settings")]
    public float updateInterval = 0.1f; // Update every 0.1 seconds
    
    private float timer = 0f;
    
    void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // Find text component if not assigned
        if (debugText == null)
        {
            debugText = GetComponent<TextMeshProUGUI>();
            if (debugText == null)
            {
                debugText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        // Setup UI positioning (top-left corner)
        SetupUI();
    }
    
    void SetupUI()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Anchor to top-left corner
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            
            // Position with offset from top-left
            rectTransform.anchoredPosition = new Vector2(10, -10);
            
            // Set size
            rectTransform.sizeDelta = new Vector2(300, 100);
        }
        
        if (debugText != null)
        {
            debugText.fontSize = 20;
            debugText.color = Color.white;
            debugText.alignment = TextAlignmentOptions.TopLeft;
        }
    }
    
    void Update()
    {
        if (playerTransform == null || debugText == null) return;
        
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            UpdateDebugText();
            timer = 0f;
        }
    }
    
    void UpdateDebugText()
    {
        Vector3 position = playerTransform.position;
        float yaw = playerTransform.eulerAngles.y;
        
        // Format position and rotation
        string positionStr = $"Position (x,z): ({position.x:F2}, {position.z:F2})";
        string yawStr = $"Yaw: {yaw:F1}°";
        
        debugText.text = $"{positionStr}\n{yawStr}";
    }
}
