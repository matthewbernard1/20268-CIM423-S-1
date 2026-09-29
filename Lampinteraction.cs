using UnityEngine;

/// <summary>
/// Put on short_lamp_1. Changes the room's ambient (Environment) color from grey to white,
/// optionally swaps the lamp shade material and turns on a light inside the lamp.
/// Set taskId = "lamp" in the Inspector.
/// </summary>
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
        // Room ambient grey -> white
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