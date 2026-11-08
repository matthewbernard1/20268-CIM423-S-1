using UnityEngine;

namespace AirbnbTurnover
{
    /// <summary>
    /// Keeps a world-space UI panel floating at an offset from the player's head, turning with them.
    /// </summary>
    public class TagAlong : MonoBehaviour
    {
        public Transform head;
        [Tooltip("Offset from the head in its yaw-only frame (x right, y up, z forward).")]
        public Vector3 offset = new Vector3(0f, 0f, 1.2f);
        public float followSpeed = 4f;

        void OnEnable()
        {
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
            Snap();
        }

        void LateUpdate()
        {
            if (head == null)
                return;
            float k = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, TargetPosition(), k);
            FaceHead();
        }

        public void Snap()
        {
            if (head == null)
                return;
            transform.position = TargetPosition();
            FaceHead();
        }

        Vector3 TargetPosition()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = Vector3.ProjectOnPlane(head.up, Vector3.up);
            Quaternion yaw = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            return head.position + yaw * offset;
        }

        void FaceHead()
        {
            // UI is readable from its -Z side, so point +Z away from the viewer.
            Vector3 away = transform.position - head.position;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
