using System;
using UnityEngine;

namespace AirbnbTurnover
{
    /// <summary>
    /// Trigger volume that accepts one kind of CleanupItem. Either consumes the item
    /// (trash bin, laundry basket) or snaps it into the next free slot (couch, sink, bed).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DropZone : MonoBehaviour
    {
        public ItemKind accepts;
        [Tooltip("Task credited for each accepted item. Use None when another script tracks progress.")]
        public TaskId task = TaskId.None;
        [Tooltip("Hide the item when it enters. Otherwise it snaps into the next slot.")]
        public bool consume = true;
        public Transform[] slots;

        public int AcceptedCount { get; private set; }
        public event Action<DropZone, CleanupItem> ItemAccepted;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other) => TryAccept(other);

        // Items released while already inside the zone still need to count.
        void OnTriggerStay(Collider other) => TryAccept(other);

        void TryAccept(Collider other)
        {
            var item = other.GetComponentInParent<CleanupItem>();
            if (item == null || item.IsDone || item.kind != accepts)
                return;

            if (consume)
            {
                item.Consume();
            }
            else
            {
                if (slots == null || AcceptedCount >= slots.Length)
                    return;
                var slot = slots[AcceptedCount];
                item.LockInPlace(slot.position, slot.rotation);
            }

            AcceptedCount++;
            if (task != TaskId.None && TaskManager.Instance != null)
                TaskManager.Instance.AddProgress(task);
            if (TaskManager.Instance != null)
                TaskManager.Instance.PlayPop(transform.position);
            ItemAccepted?.Invoke(this, item);
        }
    }
}
