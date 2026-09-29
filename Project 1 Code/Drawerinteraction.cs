using System.Collections;
using UnityEngine;

/// <summary>
/// Put on small_drawer. Slides the drawer out and reveals the book inside.
/// Set taskId = "drawer" in the Inspector.
///
/// TIP: if the drawer is one mesh with the dresser, you need the drawer to be its OWN
/// GameObject (child of the dresser) so it can move on its own.
/// </summary>
public class DrawerInteraction : TaskObject
{
    [Header("Drawer movement")]
    [Tooltip("The part that slides. Leave empty to move this object itself.")]
    public Transform drawerPart;
    [Tooltip("Local direction & distance to slide, e.g. (0,0,0.3) = 30cm forward on local Z. Flip the sign if it slides backwards.")]
    public Vector3 slideOffset = new Vector3(0f, 0f, 0.3f);
    public float slideSeconds = 0.6f;

    [Header("Book")]
    [Tooltip("The book GameObject sitting inside the drawer. It is hidden until the drawer opens.")]
    public GameObject bookInside;

    private Vector3 closedLocalPos;
    private bool hasStoredPos;

    private Transform Part => drawerPart ? drawerPart : transform;

    protected override void Start()
    {
        closedLocalPos = Part.localPosition;
        hasStoredPos = true;
        base.Start();
    }

    protected override void OnInteract()
    {
        StopAllCoroutines();
        StartCoroutine(Slide(closedLocalPos + slideOffset));
        if (bookInside) bookInside.SetActive(true);

        // Once open, drop the drawer's own collider so its ray no longer blocks
        // the book sitting inside it (the drawer is already "done").
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }

    private IEnumerator Slide(Vector3 target)
    {
        Vector3 from = Part.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, slideSeconds);
            Part.localPosition = Vector3.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        Part.localPosition = target;
    }

    public override void ResetObject()
    {
        base.ResetObject();
        StopAllCoroutines();
        if (hasStoredPos) Part.localPosition = closedLocalPos;
        if (bookInside) bookInside.SetActive(false);
        var col = GetComponent<Collider>();
        if (col) col.enabled = true;
    }
}