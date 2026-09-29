using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// Late Night Studying - central game/task manager.
/// Tasks can be completed in ANY order. When all 5 are done the end screen shows,
/// the room goes dark, the player is returned to the bed, and everything resets
/// (the "it was a dream" / Groundhog Day loop).
///
/// Put this on an empty GameObject called "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI - HUD Canvas")]
    [Tooltip("The panel (child of HUD_Canvas) that holds the title + Start button.")]
    public GameObject startPanel;
    [Tooltip("The Start button itself. Hidden once pressed, shown again after the dream loop.")]
    public GameObject startButton;
    [Tooltip("The panel that shows 'Complete the five tasks...' and the running task list.")]
    public GameObject taskPanel;
    [Tooltip("TextMeshPro text inside taskPanel that shows the instruction / progress.")]
    public TextMeshProUGUI taskText;
    [Tooltip("Optional: TextMeshPro text used for the short story 'sign' after each interaction.")]
    public TextMeshProUGUI signText;
    [Tooltip("The panel shown when everything is finished ('Project Complete... Good Job!').")]
    public GameObject endPanel;

    [Header("Player / Reset")]
    [Tooltip("Drag your XR Origin (XR Rig) here.")]
    public Transform xrOrigin;
    [Tooltip("Empty GameObject placed on / next to the bed where the player should wake up.")]
    public Transform bedSpawnPoint;
    [Tooltip("Seconds the end screen stays up before the room goes dark.")]
    public float endScreenSeconds = 4f;
    [Tooltip("Seconds of darkness before the player 'wakes up' back in bed.")]
    public float darknessSeconds = 2f;
    [Tooltip("Seconds for the black screen to slowly loom in and out.")]
    public float fadeSeconds = 2.5f;

    [Header("Room Lighting")]
    [Tooltip("The grey ambient color the room starts with (matches your Lighting > Environment > Ambient Color).")]
    public Color ambientGrey = new Color(0.35f, 0.35f, 0.35f);
    [Tooltip("Ambient color once the lamp is on.")]
    public Color ambientBright = Color.white;
    [Tooltip("Any scene lights that should be switched off when the room goes dark.")]
    public Light[] roomLights;

    [Header("Text")]
    [TextArea] public string instructionMessage = "Complete the five tasks around the room before the deadline arrives.";
    [TextArea] public string completeMessage = "Project Complete, the student is finally ready for tomorrow. Good Job!";
    [TextArea] public string wakeUpMessage = "...It was only a dream. The project still isn't done.";

    [Header("Task list labels")]
    public string lampLabel = "Turn on the lamp";
    public string drawerLabel = "Open the drawer";
    public string bookLabel = "Pick up the book";
    public string tvLabel = "Turn on the TV";
    public string closetLabel = "Open the closet";

    // ---- runtime state ----
    public const int TotalTasks = 5;
    public bool GameStarted { get; private set; }
    private readonly HashSet<string> completedTasks = new HashSet<string>();
    private readonly List<TaskObject> registeredObjects = new List<TaskObject>();
    private Coroutine signRoutine;

    // Exact spot captured when the game starts, restored at the end.
    private Vector3 startPos;
    private Quaternion startRot;
    private bool hasStartSpot;

    // Runtime head-locked black-fade overlay.
    private GameObject fadeRoot;
    private UnityEngine.UI.Image fadeImg;
    private TextMeshProUGUI fadeText;

    // ------------------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Make sure the ambient mode is "Color" so RenderSettings.ambientLight is what the room uses.
        RenderSettings.ambientMode = AmbientMode.Flat;
        ResetRoom(showStartScreen: true);
    }

    // Objects (lamp, drawer, tv...) register themselves so we can reset them later.
    public void Register(TaskObject obj)
    {
        if (!registeredObjects.Contains(obj)) registeredObjects.Add(obj);
    }

    // ------------------------------------------------------------------
    // Hook this to the Start button's OnClick() on HUD_Canvas.
    // ------------------------------------------------------------------
    public void StartGame()
    {
        if (GameStarted) return;
        GameStarted = true;
        completedTasks.Clear();

        // Every run starts fresh: anything the player fiddled with before pressing Start
        // (e.g. the TV) goes back to its starting state so it can be done as a task.
        foreach (var obj in registeredObjects) if (obj) obj.ResetObject();

        if (startPanel) startPanel.SetActive(false);
        if (startButton) startButton.SetActive(false);
        if (endPanel) endPanel.SetActive(false);
        if (taskPanel) taskPanel.SetActive(true);

        // Remember exactly where the player started, to return them here at the end.
        if (xrOrigin)
        {
            startPos = xrOrigin.position;
            startRot = xrOrigin.rotation;
            hasStartSpot = true;
        }

        RefreshTaskText();
        Debug.Log("[GameManager] Game started.");
    }

    // ------------------------------------------------------------------
    // Called by each TaskObject when the player interacts with it.
    // taskId: "lamp", "drawer", "book", "tv", "closet"
    // ------------------------------------------------------------------
    public bool CompleteTask(string taskId, string signMessage)
    {
        if (!GameStarted)
        {
            Debug.Log($"[GameManager] Ignored '{taskId}' - press Start first.");
            return false;
        }

        taskId = taskId.ToLower();
        if (completedTasks.Contains(taskId)) return false; // already done

        completedTasks.Add(taskId);
        Debug.Log($"[GameManager] Task complete: {taskId} ({completedTasks.Count}/{TotalTasks})");

        ShowSign(signMessage);
        RefreshTaskText();

        if (completedTasks.Count >= TotalTasks)
            StartCoroutine(EndSequence());

        return true;
    }

    public bool IsTaskDone(string taskId) => completedTasks.Contains(taskId.ToLower());

    // ------------------------------------------------------------------
    public void SetAmbient(Color c)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = c;
        DynamicGI.UpdateEnvironment();
    }

    private void RefreshTaskText()
    {
        if (!taskText) return;
        taskText.text =
            instructionMessage + "\n\n" +
            Line("lamp",   lampLabel) +
            Line("drawer", drawerLabel) +
            Line("book",   bookLabel) +
            Line("tv",     tvLabel) +
            Line("closet", closetLabel) +
            $"\n{completedTasks.Count} / {TotalTasks} complete";
    }

    private string Line(string id, string label)
        => (completedTasks.Contains(id) ? "[x] " : "[ ] ") + label + "\n";

    private void ShowSign(string message)
    {
        if (!signText || string.IsNullOrEmpty(message)) return;
        if (signRoutine != null) StopCoroutine(signRoutine);
        signRoutine = StartCoroutine(SignRoutine(message));
    }

    private IEnumerator SignRoutine(string message)
    {
        signText.gameObject.SetActive(true);
        signText.text = message;
        yield return new WaitForSeconds(4f);
        signText.text = "";
    }

    // ------------------------------------------------------------------
    // ENDING: Good Job -> darkness -> wake up in bed -> everything reset
    // ------------------------------------------------------------------
    private IEnumerator EndSequence()
    {
        yield return new WaitForSeconds(1f);

        if (taskPanel) taskPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);

        EnsureFadeOverlay();
        if (fadeText) fadeText.text = completeMessage;

        // The black screen slowly looms in while the completion text is presented.
        yield return Fade(0f, 1f, fadeSeconds);

        // Fully black: bring the player back to exactly where they started.
        if (xrOrigin && hasStartSpot)
            xrOrigin.SetPositionAndRotation(startPos, startRot);
        else if (xrOrigin && bedSpawnPoint)
            xrOrigin.SetPositionAndRotation(bedSpawnPoint.position, Quaternion.Euler(0f, bedSpawnPoint.eulerAngles.y, 0f));

        // Put the room back to its opening state behind the black.
        ResetRoom(showStartScreen: true);

        if (fadeText) fadeText.text = wakeUpMessage;
        yield return new WaitForSeconds(darknessSeconds);

        // Reveal the reset room from black.
        if (fadeText) fadeText.text = "";
        yield return Fade(1f, 0f, fadeSeconds);

        ShowSign(wakeUpMessage);
        if (fadeRoot) Destroy(fadeRoot);
        fadeRoot = null; fadeImg = null; fadeText = null;
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float t = 0f;
        SetFadeAlpha(from);
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, seconds);
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        if (!fadeImg) return;
        var c = fadeImg.color; c.a = a; fadeImg.color = c;
    }

    // Builds a head-locked black overlay (with a message) in front of the camera.
    private void EnsureFadeOverlay()
    {
        if (fadeRoot != null) return;
        Camera cam = null;
        if (xrOrigin) cam = xrOrigin.GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        fadeRoot = new GameObject("EndFadeOverlay");
        var canvas = fadeRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = fadeRoot.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(2400, 1600);
        fadeRoot.transform.SetParent(cam.transform, false);
        // Far enough and sized to blanket the whole (VR-wide) field of view.
        fadeRoot.transform.localPosition = new Vector3(0f, 0f, 0.8f);
        fadeRoot.transform.localRotation = Quaternion.identity;
        fadeRoot.transform.localScale = Vector3.one * 0.0011f;

        var blackGO = new GameObject("Black");
        blackGO.transform.SetParent(fadeRoot.transform, false);
        var brt = blackGO.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        fadeImg = blackGO.AddComponent<UnityEngine.UI.Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);
        fadeImg.raycastTarget = false;

        var txtGO = new GameObject("Message");
        txtGO.transform.SetParent(fadeRoot.transform, false);
        var trt = txtGO.AddComponent<RectTransform>();
        // A centered text column, well inside the big black canvas.
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1100, 800);
        trt.anchoredPosition = Vector2.zero;
        fadeText = txtGO.AddComponent<TextMeshProUGUI>();
        fadeText.alignment = TextAlignmentOptions.Center;
        fadeText.textWrappingMode = TextWrappingModes.Normal;
        fadeText.enableAutoSizing = true; fadeText.fontSizeMin = 24; fadeText.fontSizeMax = 60;
        fadeText.color = Color.white;
        fadeText.raycastTarget = false;
        fadeText.text = "";
    }

    private void ResetRoom(bool showStartScreen)
    {
        GameStarted = false;
        completedTasks.Clear();

        SetAmbient(ambientGrey);
        foreach (var l in roomLights) if (l) l.enabled = true;

        foreach (var obj in registeredObjects) if (obj) obj.ResetObject();

        if (startPanel) startPanel.SetActive(showStartScreen);
        if (startButton) startButton.SetActive(showStartScreen);
        if (taskPanel) taskPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);
        if (signText) signText.text = "";
    }

    // ------------------------------------------------------------------
    // Editor testing without a headset: Space = start, 1-5 = tasks
    // ------------------------------------------------------------------
    void Update()
    {
#if UNITY_EDITOR
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame) StartGame();
        if (kb.digit1Key.wasPressedThisFrame) FindObjectOfType<LampInteraction>()?.Interact();
        if (kb.digit2Key.wasPressedThisFrame) FindObjectOfType<DrawerInteraction>()?.Interact();
        if (kb.digit3Key.wasPressedThisFrame) FindObjectOfType<BookInteraction>()?.Interact();
        if (kb.digit4Key.wasPressedThisFrame) FindObjectOfType<TVInteraction>()?.Interact();
        if (kb.digit5Key.wasPressedThisFrame) FindObjectOfType<ClosetInteraction>()?.Interact();
#endif
    }
}