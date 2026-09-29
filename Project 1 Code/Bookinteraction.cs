using UnityEngine;
using TMPro;

/// Put on the book (inside the drawer). When grabbed/clicked it shows text.
/// Works with XR Grab Interactable OR XR Simple Interactable - hook Interact() to Select Entered.
/// Set taskId = "book" in the Inspector.

public class BookInteraction : TaskObject
{
    [Header("Book text (optional - the HUD sign text shows too)")]
    [Tooltip("A World Space Canvas (child of the book or floating above the drawer) with the book's text. Hidden until picked up.")]
    public GameObject bookTextCanvas;
    [Tooltip("The TextMeshPro text inside that canvas.")]
    public TextMeshProUGUI bookText;
    [TextArea] public string bookMessage = "The book was found.\nThe missing notes are inside.";

    [Header("Pick up")]
    [Tooltip("Hide the book (as if picked up) when clicked.")]
    public bool hideOnPickup = true;

    protected override void OnInteract()
    {
        if (bookText) bookText.text = bookMessage;
        if (bookTextCanvas) bookTextCanvas.SetActive(true);

        if (hideOnPickup) SetVisible(false);
    }

    public override void ResetObject()
    {
        base.ResetObject();
        if (bookTextCanvas) bookTextCanvas.SetActive(false);
        SetVisible(true);
    }

    // Hide/show the book without disabling this GameObject (so it can still be reset).
    void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = visible;
    }
}

