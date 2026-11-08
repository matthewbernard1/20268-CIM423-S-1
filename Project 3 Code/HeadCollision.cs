using UnityEngine;

namespace AirbnbTurnover
{
    /// <summary>
    /// Stops the player's head from passing through walls and the floor. Head movement from tracking
    /// (or the XR Interaction Simulator) bypasses the rig's CharacterController, so when the head ends up
    /// inside level geometry the whole rig is pushed back out instead.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class HeadCollision : MonoBehaviour
    {
        public Transform rig;
        public Transform head;
        public float radius = 0.18f;
        [Tooltip("Lowest the head may go above the floor.")]
        public float minHeadHeight = 0.45f;

        SphereCollider probe;
        Vector3 lastHead;
        bool hasLast;
        readonly Collider[] overlaps = new Collider[16];

        void Awake()
        {
            probe = GetComponent<SphereCollider>();
            probe.isTrigger = true;
            probe.radius = radius;
        }

        void LateUpdate()
        {
            if (rig == null || head == null)
                return;

            Vector3 current = head.position;
            Vector3 correction = Vector3.zero;

            // Fast moves can skip over a thin wall entirely, so check the path first.
            if (hasLast)
            {
                Vector3 step = current - lastHead;
                step.y = 0f;
                if (step.sqrMagnitude > 1e-6f &&
                    Physics.Raycast(lastHead, step.normalized, out var hit, step.magnitude + radius, ~0, QueryTriggerInteraction.Ignore) &&
                    IsLevel(hit.collider))
                {
                    correction -= step;
                }
            }

            // Push out of anything the head is overlapping.
            Vector3 probePos = current + correction;
            int count = Physics.OverlapSphereNonAlloc(probePos, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = overlaps[i];
                if (!IsLevel(other))
                    continue;
                if (Physics.ComputePenetration(probe, probePos, Quaternion.identity,
                        other, other.transform.position, other.transform.rotation, out var dir, out var dist))
                {
                    var push = dir * dist;
                    push.y = 0f;
                    correction += push;
                    probePos += push;
                }
            }

            // Keep the head above the floor, and out of tables, beds and counters when leaning down.
            if (Physics.Raycast(probePos + Vector3.up * 0.5f, Vector3.down, out var below, 3f, ~0, QueryTriggerInteraction.Ignore) &&
                IsLevel(below.collider))
            {
                bool isFloor = below.point.y <= rig.position.y + 0.2f;
                float clearance = isFloor ? minHeadHeight : radius;
                if (probePos.y < below.point.y + clearance)
                    correction.y += below.point.y + clearance - probePos.y;
            }

            if (correction.sqrMagnitude > 1e-8f)
            {
                rig.position += correction;
                Physics.SyncTransforms();
            }

            lastHead = head.position;
            hasLast = true;
            transform.position = lastHead;
        }

        bool IsLevel(Collider c)
        {
            // Only static scenery blocks the head: not grabbable items, not the player's own colliders.
            return c != null && c != probe && c.attachedRigidbody == null && !c.transform.IsChildOf(rig);
        }
    }
}
