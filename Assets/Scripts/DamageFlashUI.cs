using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageFlashUI : MonoBehaviour
{
    [Header("Damage Flash Settings")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
    
    private Canvas canvas;
    private GameObject flashObject;
    private Image flashImage;
    private bool isFlashing = false;
    
    void Start()
    {
        // Find Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
        }
        
        if (canvas == null)
        {
            // Create Canvas if needed
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        CreateFlashUI();
    }
    
    void CreateFlashUI()
    {
        // Create full-screen red flash image
        flashObject = new GameObject("DamageFlash");
        flashObject.transform.SetParent(canvas.transform, false);
        
        flashImage = flashObject.AddComponent<Image>();
        RectTransform rect = flashObject.GetComponent<RectTransform>();
        
        // Full screen coverage
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        // Set color
        flashImage.color = flashColor;
        
        // Initially hidden
        flashObject.SetActive(false);
    }
    
    public void ShowFlash()
    {
        if (flashObject != null && !isFlashing)
        {
            StartCoroutine(FlashCoroutine());
        }
    }
    
    IEnumerator FlashCoroutine()
    {
        isFlashing = true;
        flashObject.SetActive(true);
        
        // Flash on
        flashImage.color = flashColor;
        
        yield return new WaitForSeconds(flashDuration);
        
        // Flash off
        flashObject.SetActive(false);
        isFlashing = false;
    }
}