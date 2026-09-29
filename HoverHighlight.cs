using UnityEngine;

public class HoverHighlight : MonoBehaviour
{
    [Header("Material Settings")]
    public Material highlightMaterial;
    
    private Renderer objRenderer;
    private Material originalMaterial;

    private void Awake()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalMaterial = objRenderer.material;
        }
    }

    public void OnHoverEnter()
    {
        if (objRenderer != null && highlightMaterial != null)
        {
            objRenderer.material = highlightMaterial;
        }
    }

    public void OnHoverExit()
    {
        if (objRenderer != null && originalMaterial != null)
        {
            objRenderer.material = originalMaterial;
        }
    }
}