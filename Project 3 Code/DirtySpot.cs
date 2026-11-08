using UnityEngine;

namespace AirbnbTurnover
{
    /// <summary>
    /// A stain that fades away while a Sponge item rubs against it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DirtySpot : MonoBehaviour
    {
        public TaskId task = TaskId.Stain;
        [Tooltip("Seconds of scrubbing contact needed to remove the stain.")]
        public float scrubTime = 1.5f;
        public Renderer stainRenderer;
        public Transform highlight;

        float scrubbed;
        bool clean;
        Material stainMaterial;
        Color baseColor;
        Vector3 highlightScale;

        void Start()
        {
            stainMaterial = stainRenderer.material;
            baseColor = stainMaterial.GetColor("_BaseColor");
            if (highlight != null)
                highlightScale = highlight.localScale;
        }

        void Update()
        {
            if (clean || highlight == null)
                return;
            // Gentle pulse so the player can spot the target area.
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            highlight.localScale = highlightScale * pulse;
        }

        void OnTriggerStay(Collider other)
        {
            if (clean)
                return;
            var item = other.GetComponentInParent<CleanupItem>();
            if (item == null || item.kind != ItemKind.Sponge)
                return;

            scrubbed += Time.fixedDeltaTime;
            float remaining = 1f - Mathf.Clamp01(scrubbed / scrubTime);
            stainMaterial.SetColor("_BaseColor", new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * remaining));

            if (scrubbed >= scrubTime)
            {
                clean = true;
                stainRenderer.enabled = false;
                if (highlight != null)
                    highlight.gameObject.SetActive(false);
                if (TaskManager.Instance != null)
                    TaskManager.Instance.AddProgress(task);
            }
        }
    }
}
