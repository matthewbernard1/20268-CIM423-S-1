using UnityEngine;
using UnityEngine.Video;

/// The screen shows its plain material until the TV is clicked

/// Clicking plays the video (turns the TV on) and completes the "tv" task while a game is running.

/// Set taskId = "tv" in the Inspector. Hook the XR interactable's Select Entered to TurnOnTV().

public class TVInteraction : TaskObject
{
    [Header("TV visuals (all optional)")]
    public Renderer screenRenderer;
    public Material screenOffMaterial;
    public Material screenOnMaterial;
    public GameObject screenOnObject;
    public Light screenGlow;
    public AudioSource tvAudio;

    [Tooltip("Video Player on the screen. Turn OFF its Play On Awake; it starts when the TV is clicked.")]
    public VideoPlayer tvVideo;

    private bool isOn;

    protected override void Start()
    {
        base.Start();

        if (tvVideo)
        {
            tvVideo.playOnAwake = false;
            tvVideo.isLooping = true;
            tvVideo.errorReceived += (vp, message) => Debug.LogError("[TV] Video error: " + message, this);
            tvVideo.Prepare();
        }
    }

    // Turns the TV on any time it is clicked.
    
    public void TurnOnTV()
    {
        Debug.Log("[TV] Clicked - TurnOnTV()", this);

        Interact(); 

        // Code is guarded so any video error can't break the click.
        
        try { SetOn(true); }
        catch (System.Exception e) { Debug.LogError("[TV] Failed to start video: " + e.Message, this); }
    }

    protected override void OnInteract()
    {
        SetOn(true);
    }

    private void SetOn(bool on)
    {
        if (isOn == on) return;
        isOn = on;

        if (screenRenderer)
        {
            if (on && screenOnMaterial) screenRenderer.material = screenOnMaterial;
            else if (!on && screenOffMaterial) screenRenderer.material = screenOffMaterial;
        }
        if (screenOnObject) screenOnObject.SetActive(on);
        if (screenGlow) screenGlow.enabled = on;

        if (on)
        {
            if (tvVideo && !tvVideo.isPlaying) tvVideo.Play();
            if (tvAudio && !tvAudio.isPlaying) tvAudio.Play();
        }
        else
        {
            if (tvVideo)
            {
                tvVideo.Stop();
                ClearScreen();
                tvVideo.Prepare();
            }
            if (tvAudio) tvAudio.Stop();
        }
    }

    private void ClearScreen()
    {
        if (!tvVideo || !tvVideo.targetTexture) return;
        var previous = RenderTexture.active;
        RenderTexture.active = tvVideo.targetTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
    }

    public override void ResetObject()
    {
        base.ResetObject();
        SetOn(false);
    }
}
