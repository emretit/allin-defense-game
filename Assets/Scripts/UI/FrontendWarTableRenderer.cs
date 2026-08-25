using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    public sealed class FrontendWarTableRenderer : MonoBehaviour
    {
        private const int PresentationLayer = 31;
        private Camera presentationCamera;
        private RenderTexture renderTexture;
        private Material backdropMaterial;
        private Mesh backdropMesh;
        private RawImage target;
        private Texture2D sourceTexture;
        private bool visible;

        public RenderTexture Initialize(Texture2D texture, RawImage output, Shader configuredShader = null)
        {
            target = output;
            sourceTexture = texture;
            Shader shader = configuredShader ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("UI/Default");
            if (shader == null)
            {
                target.texture = sourceTexture;
                Debug.LogWarning("[DC_WAR_TABLE_FALLBACK] A render shader was unavailable; using the source artwork directly.");
                return null;
            }

            ResolveQualitySize(out int width, out int height);
            renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = $"Greenwatch War Table {width}x{height}",
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();

            GameObject cameraObject = new("War Table Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            presentationCamera = cameraObject.GetComponent<Camera>();
            presentationCamera.orthographic = true;
            presentationCamera.orthographicSize = 5.625f;
            presentationCamera.aspect = 16f / 9f;
            presentationCamera.clearFlags = CameraClearFlags.SolidColor;
            presentationCamera.backgroundColor = new Color(0.006f, 0.014f, 0.028f, 1f);
            presentationCamera.cullingMask = 1 << PresentationLayer;
            presentationCamera.targetTexture = renderTexture;
            presentationCamera.transform.localPosition = new Vector3(0f, 0f, -10f);

            GameObject backdrop = new("War Table Artwork", typeof(MeshFilter), typeof(MeshRenderer));
            backdrop.layer = PresentationLayer;
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.localScale = new Vector3(20f, 11.25f, 1f);
            backdropMesh = CreateQuadMesh();
            backdrop.GetComponent<MeshFilter>().sharedMesh = backdropMesh;
            backdropMaterial = new Material(shader) { name = "War Table Runtime Material", mainTexture = texture };
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = backdropMaterial;

            SetVisible(false);
            return renderTexture;
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (presentationCamera != null) presentationCamera.enabled = value;
            if (target != null && value) target.texture = renderTexture != null ? renderTexture : sourceTexture;
        }

        private void Update()
        {
            if (!visible || presentationCamera == null || FrontendPreferences.ReducedMotion) return;
            float x = Mathf.Sin(Time.unscaledTime * 0.14f) * 0.018f;
            float y = Mathf.Cos(Time.unscaledTime * 0.11f) * 0.010f;
            presentationCamera.transform.localPosition = new Vector3(x, y, -10f);
        }

        private static void ResolveQualitySize(out int width, out int height)
        {
            string quality = QualitySettings.names.Length > 0
                ? QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]
                : "Medium";
            if (quality.ToLowerInvariant().Contains("low"))
            {
                width = 640;
                height = 360;
            }
            else if (quality.ToLowerInvariant().Contains("high") || quality.ToLowerInvariant().Contains("ultra"))
            {
                width = 1280;
                height = 720;
            }
            else
            {
                width = 960;
                height = 540;
            }
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "Frontend War Table Quad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (presentationCamera != null) presentationCamera.targetTexture = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (backdropMaterial != null) Destroy(backdropMaterial);
            if (backdropMesh != null) Destroy(backdropMesh);
        }
    }
}
