using System.Collections;
using UnityEngine;

public class ClosetInteraction : TaskObject
{
    [Header("Door movement")]
    [Tooltip("The transform to rotate (hinge). Leave empty to rotate this object.")]
    public Transform doorPivot;
    [Tooltip("Degrees to swing on the local Y axis. Use -90 if it opens the wrong way.")]
    public float openAngle = 90f;
    public float swingSeconds = 0.8f;

    [Header("Reveal")]
    [Tooltip("The outfit / shirt object inside the closet (optional, shown when opened).")]
    public GameObject outfitInside;

    [Header("Hang clothes")]
    [Tooltip("The folded clothes to hide when the right one is clicked.")]
    public GameObject[] foldedClothes;
    [Tooltip("The clothes shown hanging on the hanger/railing when clicked.")]
    public GameObject hungClothes;

    private Quaternion closedRot;
    private bool hasStoredRot;
    private Transform Pivot => doorPivot ? doorPivot : transform;

    protected override void Start()
    {
        closedRot = Pivot.localRotation;
        hasStoredRot = true;
        base.Start();
    }

    protected override void OnInteract()
    {
        StopAllCoroutines();
        if (Mathf.Abs(openAngle) > 0.01f)
            StartCoroutine(Swing(closedRot * Quaternion.Euler(0f, openAngle, 0f)));
        if (outfitInside) outfitInside.SetActive(true);

        if (foldedClothes != null)
            foreach (var g in foldedClothes) if (g) g.SetActive(false);
        if (hungClothes) hungClothes.SetActive(true);
    }

    private IEnumerator Swing(Quaternion target)
    {
        Quaternion from = Pivot.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, swingSeconds);
            Pivot.localRotation = Quaternion.Slerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        Pivot.localRotation = target;
    }

    public override void ResetObject()
    {
        base.ResetObject();
        StopAllCoroutines();
        if (hasStoredRot) Pivot.localRotation = closedRot;
        if (outfitInside) outfitInside.SetActive(false);

        if (foldedClothes != null)
            foreach (var g in foldedClothes) if (g) g.SetActive(true);
        if (hungClothes) hungClothes.SetActive(false);
    }
}
