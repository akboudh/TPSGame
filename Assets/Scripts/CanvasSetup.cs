using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class CanvasSetup : MonoBehaviour
{
    void Start()
    {
        SetupCanvas();
    }
    
    void SetupCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CanvasSetup: Missing Canvas component!");
            return;
        }
        
        // Ensure Canvas is set to Screen Space - Overlay
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Optional: Enable Pixel Perfect (can be adjusted in Inspector)
        // CanvasScaler will handle resolution scaling automatically
    }
}
