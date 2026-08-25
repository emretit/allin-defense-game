using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DefenderChronicles.Presentation
{
    /// <summary>
    /// Owns the production-only camera, lighting and restrained mobile post-processing look.
    /// It never changes gameplay state, colliders or timing.
    /// </summary>
    public sealed class DefenderSceneLook : MonoBehaviour
    {
        private VolumeProfile runtimeProfile;
        private Bloom bloom;
        private ColorAdjustments colorAdjustments;
        private Vignette vignette;
        private Coroutine pulseRoutine;

        public static void Pulse(PresentationCue cue)
        {
            Camera camera = Camera.main;
            DefenderSceneLook look = camera != null ? camera.GetComponent<DefenderSceneLook>() : null;
            if (look == null) return;
            if (look.pulseRoutine != null) look.StopCoroutine(look.pulseRoutine);
            look.pulseRoutine = look.StartCoroutine(look.PulseLook(cue));
        }

        public static void Apply(Camera camera)
        {
            if (camera == null) return;
            DefenderSceneLook look = camera.GetComponent<DefenderSceneLook>();
            if (look == null) look = camera.gameObject.AddComponent<DefenderSceneLook>();
            look.Configure(camera);
        }

        private void Configure(Camera camera)
        {
            var quality = GetQualityTier();

            camera.allowHDR = true;
            // SMAA gives the fixed isometric scene clean edges without the iOS overlay/subpass
            // sample-count mismatch caused by combining MSAA with uGUI post-processing.
            camera.allowMSAA = false;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.Medium;
            cameraData.dithering = true;
            cameraData.stopNaN = true;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.21f, 0.30f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.085f, 0.15f, 0.205f);
            RenderSettings.ambientGroundColor = new Color(0.024f, 0.052f, 0.062f);
            RenderSettings.reflectionIntensity = 0.38f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.026f, 0.068f, 0.105f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 50f;

            ConfigureSun(quality);
            EnsureFillLight("Moon Fill", new Vector3(11f, 9f, 5.5f), new Color(0.22f, 0.43f, 0.82f), quality, 0.96f, 24f);
            EnsureFillLight("Gate Ember Fill", new Vector3(-1.2f, 3.4f, 2f), new Color(1f, 0.30f, 0.075f), quality, 1.82f, 7.2f);
            ConfigureVolume();
        }

        private enum VisualQualityTier
        {
            Low,
            Medium,
            High,
            Unknown
        }

        private static VisualQualityTier GetQualityTier()
        {
            string qualityName = QualitySettings.names != null && QualitySettings.names.Length > 0
                ? QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]
                : string.Empty;

            qualityName = (qualityName ?? string.Empty).ToLowerInvariant();
            if (qualityName.Contains("low")) return VisualQualityTier.Low;
            if (qualityName.Contains("medium")) return VisualQualityTier.Medium;
            if (qualityName.Contains("high")) return VisualQualityTier.High;
            return VisualQualityTier.Unknown;
        }

        private static void ConfigureSun(VisualQualityTier qualityTier)
        {
            Light sun = null;
            foreach (Light candidate in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (candidate.type != LightType.Directional) continue;
                sun = candidate;
                break;
            }

            if (sun == null)
            {
                var sunObject = new GameObject("Greenwatch Moon Key");
                sun = sunObject.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.name = "Greenwatch Moon Key";
            sun.color = new Color(0.66f, 0.78f, 0.96f);
            sun.intensity = 1.58f;
            sun.shadows = (qualityTier == VisualQualityTier.High || qualityTier == VisualQualityTier.Unknown) ? LightShadows.Soft : LightShadows.None;
            sun.shadowStrength = 0.74f;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.32f;
            sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            RenderSettings.sun = sun;
        }

        private static void EnsureFillLight(
            string lightName,
            Vector3 position,
            Color color,
            VisualQualityTier qualityTier,
            float intensity,
            float range)
        {
            GameObject lightObject = GameObject.Find(lightName);
            if (lightObject == null) lightObject = new GameObject(lightName);
            lightObject.transform.position = position;
            Light light = lightObject.GetComponent<Light>();
            if (light == null) light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = (qualityTier == VisualQualityTier.Low) ? intensity * 0.65f : intensity;
            light.range = (qualityTier == VisualQualityTier.Low) ? range * 0.6f : range;
            light.shadows = LightShadows.None;
        }

        private void ConfigureVolume()
        {
            Volume volume = GetComponent<Volume>();
            if (volume == null) volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;

            if (runtimeProfile != null) Destroy(runtimeProfile);
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "Defender Runtime Look";
            volume.profile = runtimeProfile;

            bloom = runtimeProfile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(1.08f);
            bloom.intensity.Override(0.28f);
            bloom.scatter.Override(0.58f);
            bloom.clamp.Override(4.5f);

            Tonemapping tonemapping = runtimeProfile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            colorAdjustments = runtimeProfile.Add<ColorAdjustments>();
            colorAdjustments.postExposure.Override(0.08f);
            colorAdjustments.contrast.Override(18f);
            colorAdjustments.saturation.Override(8f);
            colorAdjustments.colorFilter.Override(new Color(0.96f, 0.98f, 0.99f));

            WhiteBalance whiteBalance = runtimeProfile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(-4f);
            whiteBalance.tint.Override(1f);

            vignette = runtimeProfile.Add<Vignette>();
            vignette.color.Override(new Color(0.01f, 0.025f, 0.055f));
            vignette.intensity.Override(0.17f);
            vignette.smoothness.Override(0.48f);
        }

        private IEnumerator PulseLook(PresentationCue cue)
        {
            if (bloom == null || vignette == null || colorAdjustments == null) yield break;
            float duration = cue == PresentationCue.BossPhase ? 0.82f : 0.52f;
            float peakBloom = cue switch
            {
                PresentationCue.BossPhase => 0.7f,
                PresentationCue.Victory => 0.56f,
                PresentationCue.Defeat => 0.48f,
                _ => 0.38f
            };
            Color peakFilter = cue switch
            {
                PresentationCue.BossPhase or PresentationCue.Defeat => new Color(1f, 0.72f, 0.70f),
                PresentationCue.Victory => new Color(1f, 0.9f, 0.66f),
                _ => new Color(0.78f, 0.9f, 1f)
            };
            Color baseFilter = new(0.95f, 0.98f, 1f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                bloom.intensity.Override(Mathf.Lerp(0.28f, peakBloom, envelope));
                vignette.intensity.Override(Mathf.Lerp(0.13f, 0.24f, envelope));
                colorAdjustments.colorFilter.Override(Color.Lerp(baseFilter, peakFilter, envelope * 0.62f));
                yield return null;
            }
            bloom.intensity.Override(0.28f);
            vignette.intensity.Override(0.17f);
            colorAdjustments.colorFilter.Override(baseFilter);
            pulseRoutine = null;
        }

        private void OnDestroy()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            if (runtimeProfile != null) Destroy(runtimeProfile);
        }
    }
}
