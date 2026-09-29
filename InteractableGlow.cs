using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Makes an interactable object glow when the player points at it (before it is selected).
///
/// SETUP: drop this on the same GameObject as the XR Simple/Grab Interactable
/// (lamp, drawer, book, TV, closet door). That's it - it finds the interactable and
/// hooks its Hover Entered / Hover Exited events itself, and glows every renderer
/// under the object, so multi-mesh objects light up as one.
///
/// It works by cloning the object's materials while glowing and turning on emission
/// (URP Lit / Standard), then putting the original materials back on hover exit -
/// so no extra "highlight" materials are needed and nothing is changed on disk.
/// </summary>
[DisallowMultipleComponent]
public class InteractableGlow : MonoBehaviour
{
    [Header("Hover glow")]
    [ColorUsage(false, true)] public Color glowColor = new Color(1f, 0.85f, 0.3f);
    [Range(0f, 5f), Tooltip("How bright the glow is while pointed at.")]
    public float glowStrength = 1.5f;
    [Tooltip("Gently pulse the glow instead of a flat colour.")]
    public bool pulse = true;
    public float pulseSpeed = 3f;

    [Header("Idle hint (optional)")]
    [Tooltip("Faint pulsing glow even when NOT pointed at, so players can spot what is interactable.")]
    public bool idleHint = false;
    [Range(0f, 2f)] public float idleStrength = 0.3f;

    [Header("Behaviour")]
    [Tooltip("Stop glowing once this object's task has been completed.")]
    public bool stopWhenDone = true;
    [Tooltip("Renderers to glow. Leave EMPTY to use every renderer under this object.")]
    public Renderer[] renderers;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    const string EmissionKeyword = "_EMISSION";

    class Entry
    {
        public Renderer renderer;
        public Material[] originals;   // what was on the renderer before we touched it
        public Material[] instances;   // our clones with emission turned on
    }

    readonly List<Entry> entries = new List<Entry>();
    XRBaseInteractable interactable;
    TaskObject task;
    bool hovered;
    bool glowing;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>() ?? GetComponentInParent<XRBaseInteractable>();
        task = GetComponent<TaskObject>();

        if (renderers == null || renderers.Length == 0)
        {
            var found = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // Skip meshes that belong to a nested interactable (e.g. the book inside the drawer).
                if (r.GetComponentInParent<InteractableGlow>() != this) continue;
                found.Add(r);
            }
            renderers = found.ToArray();
        }

        foreach (var r in renderers)
            if (r) entries.Add(new Entry { renderer = r });

        if (interactable == null)
            Debug.LogWarning($"[{name}] InteractableGlow: no XR interactable found on this object or its parents.", this);
    }

    void OnEnable()
    {
        if (interactable == null) return;
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    void OnDisable()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
        hovered = false;
        StopGlow();
    }

    void OnDestroy() => DestroyInstances();

    void OnHoverEntered(HoverEnterEventArgs _) => hovered = true;
    void OnHoverExited(HoverExitEventArgs _) => hovered = false;

    void Update()
    {
        bool done = stopWhenDone && task != null && task.IsDone;
        bool wantHover = hovered && !done;
        bool wantIdle = idleHint && !done;

        if (!wantHover && !wantIdle) { StopGlow(); return; }

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        float strength = wantHover
            ? (pulse ? Mathf.Lerp(glowStrength * 0.5f, glowStrength, wave) : glowStrength)
            : idleStrength * wave;

        StartGlow();
        SetEmission(glowColor * strength);
    }

    void StartGlow()
    {
        if (glowing) return;
        glowing = true;

        foreach (var e in entries)
        {
            if (!e.renderer) continue;
            // Re-read the originals every time so material swaps done by the
            // interaction scripts (lamp bright material, TV screen) are respected.
            e.originals = e.renderer.sharedMaterials;
            e.instances = new Material[e.originals.Length];
            for (int i = 0; i < e.originals.Length; i++)
            {
                if (e.originals[i] == null) continue;
                var m = new Material(e.originals[i]) { name = e.originals[i].name + " (glow)" };
                if (m.HasProperty(EmissionColorId)) m.EnableKeyword(EmissionKeyword);
                e.instances[i] = m;
            }
            e.renderer.sharedMaterials = e.instances;
        }
    }

    void SetEmission(Color c)
    {
        foreach (var e in entries)
            if (e.instances != null)
                foreach (var m in e.instances)
                    if (m && m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, c);
    }

    void StopGlow()
    {
        if (!glowing) return;
        glowing = false;

        foreach (var e in entries)
        {
            if (!e.renderer || e.instances == null) continue;
            // Only restore if the renderer still wears our clones. If an interaction
            // script swapped the material while we were glowing, leave its change alone.
            if (StillOurs(e)) e.renderer.sharedMaterials = e.originals;
        }
        DestroyInstances();
    }

    bool StillOurs(Entry e)
    {
        var current = e.renderer.sharedMaterials;
        if (current.Length != e.instances.Length) return false;
        for (int i = 0; i < current.Length; i++)
            if (current[i] != e.instances[i]) return false;
        return true;
    }

    void DestroyInstances()
    {
        foreach (var e in entries)
        {
            if (e.instances == null) continue;
            foreach (var m in e.instances) if (m) Destroy(m);
            e.instances = null;
        }
    }
}
