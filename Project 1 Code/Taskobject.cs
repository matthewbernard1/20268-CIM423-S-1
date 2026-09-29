using UnityEngine;

// TaskObject that makes sure each important intertable is registered when it's box colider is clicked on

public abstract class TaskObject : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("Must match one of: lamp, drawer, book, tv, closet")]
    public string taskId;
    [TextArea] public string signMessage;

    protected bool isDone;

    public bool IsDone => isDone;

    protected virtual void Start()
    {
        if (GameManager.Instance) GameManager.Instance.Register(this);
        ResetObject();
    }

    public void Interact()
    {
        if (isDone) return;
        if (GameManager.Instance == null) { Debug.LogWarning("No GameManager in scene."); return; }

        if (!GameManager.Instance.CompleteTask(taskId, signMessage)) return;

        isDone = true;
        OnInteract();
    }

    protected abstract void OnInteract();

    public virtual void ResetObject() { isDone = false; }
}
