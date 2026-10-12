using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI
{
    // Original artwork, sampled directly; no offscreen camera or RenderTexture.
    public sealed class FrontendWarTableRenderer : MonoBehaviour
    {
        private RawImage target;
        private Texture2D source;
        public RenderTexture Initialize(Texture2D texture, RawImage output, Shader configuredShader = null)
        {
            source = texture;
            target = output;
            return null;
        }
        public void SetVisible(bool value)
        {
            if (target == null) return;
            if (value) target.texture = source;
            if (target.TryGetComponent<FrontendCoverImage>(out var cover)) cover.Drift = value;
        }
    }
}
