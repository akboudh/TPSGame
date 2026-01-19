using UnityEngine;

public class DebugUISetup : MonoBehaviour
{
    void Start()
    {
        SetupArrow();
    }
    
    void SetupArrow()
    {
        GameObject arrowObj = GameObject.Find("DirectionArrow");
        if (arrowObj == null) return;
        
        Transform arrowTransform = arrowObj.transform;
        
        // Scale to make it arrow-like: long and narrow pointing forward
        arrowTransform.localScale = new Vector3(0.2f, 0.2f, 1.5f);
        arrowTransform.localPosition = new Vector3(0, 0.5f, 1f);
        
        // Rotate to point forward (since cube's forward is along Z)
        arrowTransform.localRotation = Quaternion.identity;
        
        // Change color to make it more visible - create red material
        Renderer renderer = arrowObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            renderer.material = mat;
        }
    }
}
