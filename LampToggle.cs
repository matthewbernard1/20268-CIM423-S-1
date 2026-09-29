using UnityEngine;

public class LampToggle : MonoBehaviour
{
    // Drag your Point Light game object into this box in the Inspector
    public GameObject roomLight; 

    // This runs whenever you click the object this script is attached to
    private void OnMouseDown()
    {
        if (roomLight != null)
        {
            // If the light is active, deactivate it. If it's inactive, activate it.
            roomLight.SetActive(!roomLight.activeSelf);
        }
    }
}
