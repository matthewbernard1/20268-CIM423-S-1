using UnityEngine;
using Unity.XR.CoreUtils;

/// Keeps the player's head inside the room and out of furniture by using code and box coliders

/// If the camera is outside the room box, the whole rig is shifted

/// Works for joystick movement, teleporting, and the XR Interaction Simulator


[RequireComponent(typeof(XROrigin))]
public class RoomBounds : MonoBehaviour
{
    [Header("Room")]
    [Tooltip("World-space center of the room box.")]
    public Vector3 center = new Vector3(3.9f, 1.5f, -2.1f);
    [Tooltip("World-space size of the room box (inner wall to inner wall).")]
    public Vector3 size = new Vector3(5.7f, 4f, 6.7f);
    [Tooltip("How far the head must stay away from the walls, in meters.")]
    public float headMargin = 0.35f;

    [Header("Furniture")]
    [Tooltip("Push the head out of any solid collider it overlaps (dresser, TV, lamp...).")]
    public bool pushOutOfObjects = true;
    [Tooltip("Radius of the invisible sphere around the camera used for pushing.")]
    public float headRadius = 0.15f;
    [Tooltip("Which layers count as solid.")]
    public LayerMask solidLayers = ~0;

    private XROrigin origin;
    private SphereCollider headCollider;
    private Vector3 lastSafeHead;
    private Vector3 lastOriginPos;
    private readonly Collider[] hits = new Collider[16];

    void Awake()
    {
        origin = GetComponent<XROrigin>();
    }

    void Start()
    {
        var headGO = new GameObject("Head Collider");
        headGO.layer = gameObject.layer;
        headGO.transform.SetParent(origin.Camera.transform, false);
        headCollider = headGO.AddComponent<SphereCollider>();
        headCollider.isTrigger = true;
        headCollider.radius = headRadius;

        lastSafeHead = origin.Camera.transform.position;
        lastOriginPos = transform.position;
    }

    void LateUpdate()
    {
        if (origin.Camera == null) return;
        var cam = origin.Camera.transform;

        if ((transform.position - lastOriginPos).sqrMagnitude > 1f)
            lastSafeHead = cam.position;

        ClampToRoom(cam);
        if (pushOutOfObjects && headCollider != null) PushOutOfObjects(cam);

        lastOriginPos = transform.position;
    }

    void ClampToRoom(Transform cam)
    {
        Vector3 head = cam.position;
        Vector3 min = center - size * 0.5f + Vector3.one * headMargin;
        Vector3 max = center + size * 0.5f - Vector3.one * headMargin;

        Vector3 clamped = new Vector3(
            Mathf.Clamp(head.x, min.x, max.x),
            Mathf.Clamp(head.y, min.y, max.y),
            Mathf.Clamp(head.z, min.z, max.z));

        Vector3 delta = clamped - head;
        if (delta.sqrMagnitude > 1e-8f)
            transform.position += delta;
    }

    void PushOutOfObjects(Transform cam)
    {
        Vector3 head = cam.position;
        int n = Physics.OverlapSphereNonAlloc(head, headRadius, hits, solidLayers, QueryTriggerInteraction.Ignore);

        Vector3 push = Vector3.zero;
        bool overlapping = false, unresolved = false;

        for (int i = 0; i < n; i++)
        {
            var c = hits[i];
            if (c.transform.IsChildOf(transform)) continue;  
            if (IsStructural(c.bounds)) continue;              
            overlapping = true;

            if (Physics.ComputePenetration(headCollider, head, Quaternion.identity,
                    c, c.transform.position, c.transform.rotation, out Vector3 dir, out float dist))
                push += dir * dist;
            else
                unresolved = true;
        }

        if (push.sqrMagnitude > 1e-8f)
            transform.position += push;
        else if (unresolved)
            transform.position += lastSafeHead - head;  

        if (!overlapping) lastSafeHead = head;
    }


    bool IsStructural(Bounds b)
    {
        return b.size.x > size.x * 0.95f || b.size.z > size.z * 0.95f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(center, size - Vector3.one * headMargin * 2f);
    }
}
