using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Semi-transparent panel that sits behind a TextMeshPro text to make it easier to read.
/// Put this on an Image that is a sibling placed just ABOVE the text in the hierarchy
/// (so it draws behind it). The panel resizes to fit the text and hides itself
/// whenever the text is hidden or empty.
/// </summary>
[RequireComponent(typeof(Image))]
public class TextBackdrop : MonoBehaviour
{
    [Tooltip("The text this panel sits behind.")]
    public TMP_Text target;
    [Tooltip("Extra space around the text, in canvas units.")]
    public Vector2 padding = new Vector2(40f, 24f);

    private Image image;
    private RectTransform rect;

    void Awake()
    {
        image = GetComponent<Image>();
        rect = (RectTransform)transform;
        image.raycastTarget = false;
    }

    void LateUpdate()
    {
        bool show = target && target.isActiveAndEnabled && !string.IsNullOrWhiteSpace(target.text);
        image.enabled = show;
        if (!show) return;

        target.ForceMeshUpdate();
        Bounds b = target.textBounds;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = (Vector2)b.size + padding;
        rect.position = target.transform.TransformPoint(b.center);
        rect.rotation = target.transform.rotation;
    }
}
