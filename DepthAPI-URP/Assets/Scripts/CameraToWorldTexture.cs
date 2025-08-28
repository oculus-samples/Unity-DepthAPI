using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.CameraToWorld
{
    /// <summary>
    /// Copies frames from WebCamTexture into a manually-provided RenderTexture.
    /// This component never allocates or configures the RT — assign it in the Inspector.
    /// </summary>
    public class CameraToWorldTexture : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WebCamTextureManager webCamTextureManager;

        [Header("Output (assign manually)")]
        [SerializeField] private RenderTexture outputRT;   // You create & configure this elsewhere

        public RenderTexture OutputRT => outputRT;
        public bool IsReady => outputRT != null;

        private IEnumerator Start()
        {
            // Wait until webcam exists
            while (webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
                yield return null;

            // Warn if missing RT
            if (outputRT == null)
                Debug.LogWarning("[CameraToWorldTexture] Output RenderTexture is not assigned.");
        }

        private void Update()
        {
            var wct = webCamTextureManager?.WebCamTexture;
            if (wct == null || !wct.isPlaying || outputRT == null) return;

            Graphics.Blit(wct, outputRT);
        }

        /// <summary>
        /// Grab a CPU-readable RGBA32 snapshot of the current outputRT.
        /// </summary>
        public Texture2D SnapshotRGBA32()
        {
            if (outputRT == null) return null;

            var prev = RenderTexture.active;
            RenderTexture.active = outputRT;

            var tex = new Texture2D(outputRT.width, outputRT.height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, outputRT.width, outputRT.height), 0, 0, false);
            tex.Apply(false, false);

            RenderTexture.active = prev;
            return tex;
        }
    }

    // Simple “log once per-object” helper
    static class DebugExtensions
    {
        public static void DebugLogOnce(this Object ctx, string msg)
        {
            if (!ctx) return;
            string key = $"__logged__{msg}";
            if (ctx.name.Contains(key)) return;
            Debug.Log(msg, ctx);
            ctx.name += key; // crude but effective per-instance throttle
        }
    }
}
