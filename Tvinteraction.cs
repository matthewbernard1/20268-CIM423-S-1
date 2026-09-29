using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Put on the TV. The screen shows its plain material until the TV is clicked;
/// clicking plays the video (turns the TV on) and completes the "tv" task while a game is running.
/// Set taskId = "tv" in the Inspector. Hook the XR interactable's Select Entered to TurnOnTV().
///
/// Video setup: a Video Player on the screen with Play On Awake OFF, Render Mode = Render Texture,
/// Target Texture = TVScreen_RT, and screenOnMaterial showing that render texture.
/// </summary>
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
            // Decode the first frames now so the screen lights up the instant it is clicked.
            tvVideo.Prepare();
        }
    }

    // Hook this to the XR interactable's Select Entered. Turns the TV on any time.
    public void TurnOnTV()
    {
        Debug.Log("[TV] Clicked - TurnOnTV()", this);

        // Complete the task FIRST so a video/codec hiccup can never block it.
        Interact();   // base: completes the "tv" task if the game is running (ignored otherwise)

        // Then turn the screen on, guarded so any video error can't break the click.
        try { SetOn(true); }
        catch (System.Exception e) { Debug.LogError("[TV] Failed to start video: " + e.Message, this); }
    }

    protected override void OnInteract()
    {
        // Visuals are already handled by TurnOnTV(); keep this idempotent.
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
                tvVideo.Prepare();   // ready for the next click
            }
            if (tvAudio) tvAudio.Stop();
        }
    }

    // Blank the render texture so the "off" TV never shows a leftover video frame.
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
