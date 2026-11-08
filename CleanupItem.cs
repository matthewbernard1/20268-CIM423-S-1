using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AirbnbTurnover
{
    public enum ItemKind { Trash, CouchPillow, Dish, Towel, BedPillow, Sponge, Gloves }

    /// <summary>
    /// Marks a grabbable mess object. Drop zones and dirty spots look for this component.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CleanupItem : MonoBehaviour, IXRSelectFilter
    {
        // Inside of the house the player can walk around in (matches the teleport floor, floor at y ~0.11).
        static readonly Bounds PlayArea = new Bounds(new Vector3(0f, 2.55f, 0f), new Vector3(11.2f, 6.1f, 20.5f));
        // Anything resting higher than this is out of the player's reach.
        const float MaxReachHeight = 2.4f;
        [Tooltip("Seconds an item may stay out of reach before it is sent back to where it started.")]
        public float outOfReachDelay = 1f;

        public ItemKind kind;

        public bool IsDone { get; private set; }

        Rigidbody body;
        XRGrabInteractable grab;
        Vector3 startPosition;
        Quaternion startRotation;
        float outOfReachTime;
        bool putOnRequested;
        Vector3 lastCenter;
        Vector3 lastPosition;
        bool hasLast;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            startPosition = transform.position;
            startRotation = transform.rotation;

            // Extra solver passes and gentle depenetration keep small, thin items from sinking into or popping through furniture.
            body.solverIterations = 12;
            body.solverVelocityIterations = 4;
            body.maxDepenetrationVelocity = 1.5f;

            if (grab != null && kind != ItemKind.Gloves)
                grab.selectFilters.Add(this);
            if (grab != null && kind == ItemKind.Gloves)
            {
                grab.selectEntered.AddListener(_ => putOnRequested = true);
                grab.activated.AddListener(_ => putOnRequested = true);
            }
        }

        void Update()
        {
            if (IsDone)
                return;

            // Releasing inside the select callback confuses the interactor, so the gloves go on a frame later.
            if (putOnRequested)
            {
                PutOnGloves();
                return;
            }

            // If something glitches through a wall or the floor, or lands somewhere unreachable, put it back where it started.
            if (!IsHeld && IsOutOfReach())
            {
                outOfReachTime += Time.deltaTime;
                if (outOfReachTime >= outOfReachDelay)
                    ResetToStart();
            }
            else
            {
                outOfReachTime = 0f;
            }
        }

        void FixedUpdate()
        {
            if (IsDone || body.isKinematic)
            {
                hasLast = false;
                return;
            }

            // Continuous collision misses some cases (spinning thin items, being shoved by the hand), so check whether the
            // item crossed a wall, floor or piece of furniture since the last step and, if it did, put it back on its side.
            Vector3 center = body.worldCenterOfMass;
            if (hasLast && (center - lastCenter).sqrMagnitude > 1e-6f &&
                Physics.Linecast(lastCenter, center, out var hit, ~0, QueryTriggerInteraction.Ignore) &&
                BlocksItems(hit.collider))
            {
                body.position = lastPosition;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity *= 0.5f;
                return;
            }
            lastCenter = center;
            lastPosition = body.position;
            hasLast = true;
        }

        bool BlocksItems(Collider c)
        {
            // Static scenery and locked-in items block; other loose items and the player's own body don't.
            if (c is CharacterController)
                return false;
            var other = c.attachedRigidbody;
            return other == null || (other != body && other.isKinematic);
        }

        // ---- IXRSelectFilter: nothing but the gloves can be picked up until the gloves are on. ----

        public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (TaskManager.Instance == null || TaskManager.Instance.GlovesOn)
                return true;
            // Only nag when the player is actually trying to grab, not just pointing at things.
            if (interactor.isSelectActive)
                TaskManager.Instance.RemindGloves();
            return false;
        }

        bool IsOutOfReach()
        {
            Vector3 p = body.worldCenterOfMass;
            if (!PlayArea.Contains(p))
                return true;
            // High up on top of a cabinet or wedged in the ceiling, and not moving.
            // Items placed up high on purpose (decorations) are left alone.
            return p.y > MaxReachHeight && startPosition.y < MaxReachHeight && body.linearVelocity.sqrMagnitude < 0.01f;
        }

        public bool IsHeld => grab != null && grab.isSelected;

        /// <summary>Forces any hand/controller holding this item to let go.</summary>
        public void ForceRelease()
        {
            if (grab != null && grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        /// <summary>Removes the item from the world (trash in the bin, towel in the basket).</summary>
        public void Consume()
        {
            IsDone = true;
            ForceRelease();
            gameObject.SetActive(false);
        }

        /// <summary>Locks the item into a pose (pillow on couch, plate in sink).</summary>
        public void LockInPlace(Vector3 position, Quaternion rotation)
        {
            IsDone = true;
            ForceRelease();
            if (grab != null)
                grab.enabled = false;
            // Kinematic bodies only support speculative CCD.
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>The player "puts on" the gloves: the whole pair disappears and the task is credited.</summary>
        void PutOnGloves()
        {
            foreach (var item in FindObjectsByType<CleanupItem>())
                if (item.kind == ItemKind.Gloves && !item.IsDone)
                    item.Consume();
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.PlayPop(transform.position);
                TaskManager.Instance.AddProgress(TaskId.Gloves);
            }
        }

        void ResetToStart()
        {
            ForceRelease();
            outOfReachTime = 0f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            hasLast = false;
            body.position = startPosition;
            body.rotation = startRotation;
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
