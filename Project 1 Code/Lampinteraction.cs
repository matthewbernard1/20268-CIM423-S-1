using UnityEngine;


/// Allows Lamp to turn on light in room

public class LampInteraction : TaskObject
{
    [Header("Lamp visuals (all optional)")]
    [Tooltip("Renderer of the lamp shade / bulb, if you want its material to change.")]
    public Renderer lampRenderer;
    public Material darkMaterial;
    public Material brightMaterial;
    [Tooltip("A Point Light placed inside the lamp. Off until interacted.")]
    public Light lampLight;

    protected override void OnInteract()
    {
        // Room ambient color changes from grey to white
        
        GameManager.Instance.SetAmbient(GameManager.Instance.ambientBright);

        if (lampRenderer && brightMaterial) lampRenderer.material = brightMaterial;
        if (lampLight) lampLight.enabled = true;
    }

    public override void ResetObject()
    {
        base.ResetObject();
        if (lampRenderer && darkMaterial) lampRenderer.material = darkMaterial;
        if (lampLight) lampLight.enabled = false;
    }
}
