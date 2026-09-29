using UnityEngine;

/// <summary>
/// Base class for every interactable object in the room.
/// Each object does its visible thing (open, light up, etc.) and tells the GameManager.
///
/// WIRING (same for every object):
///   1. Put an XR Simple Interactable (or XR Grab Interactable for the book) on the object.
///   2. Make sure the object has a Collider.
///   3. In the interactable's "Select Entered" event, press +, drag THIS object in,
///      and choose  <ScriptName> -> Interact().
/// </summary>
public abstract class TaskObject : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("Must match one of: lamp, drawer, book, tv, closet")]
    public string taskId;
    [TextArea] public string signMessage;

    protected bool isDone;

    /// <summary>True once this object's task has been completed (until ResetObject).</summary>
    public bool IsDone => isDone;

    protected virtual void Start()
    {
        if (GameManager.Instance) GameManager.Instance.Register(this);
        ResetObject();
    }

    /// <summary>Hook this to Select Entered on the XR interactable.</summary>
    public void Interact()
    {
        if (isDone) return;
        if (GameManager.Instance == null) { Debug.LogWarning("No GameManager in scene."); return; }

        // Ask the manager first - it refuses if the game hasn't started yet.
        if (!GameManager.Instance.CompleteTask(taskId, signMessage)) return;

        isDone = true;
        OnInteract();
    }

    /// <summary>The visible effect (open drawer, light lamp...).</summary>
    protected abstract void OnInteract();

    /// <summary>Put the object back to its starting state (called at scene start and after the dream loop).</summary>
    public virtual void ResetObject() { isDone = false; }
}