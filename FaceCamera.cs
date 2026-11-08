using UnityEngine;

namespace AirbnbTurnover
{
    /// <summary>Billboards a floating world label toward the player (yaw only).</summary>
    public class FaceCamera : MonoBehaviour
    {
        Transform head;

        void LateUpdate()
        {
            if (head == null)
            {
                if (Camera.main == null)
                    return;
                head = Camera.main.transform;
            }
            Vector3 away = transform.position - head.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
