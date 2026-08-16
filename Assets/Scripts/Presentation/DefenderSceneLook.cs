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

        public static void Apply(Camera camera)
        {
            if (camera == null) return;
            DefenderSceneLook look = camera.GetComponent<DefenderSceneLook>();
            if (look == null) look = camera.gameObject.AddComponent<DefenderSceneLook>();
            look.Configure(camera);
        }

        private void Configure(Camera camera)
        {
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
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.27f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.075f, 0.15f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.055f, 0.065f);
            RenderSettings.reflectionIntensity = 0.38f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.035f, 0.085f, 0.14f);
            RenderSettings.fogStartDistance = 21f;
            RenderSettings.fogEndDistance = 45f;

            ConfigureSun();
            EnsureFillLight("Moon Fill", new Vector3(11f, 9f, 5.5f), new Color(0.25f, 0.52f, 1f), 1.15f, 25f);
            EnsureFillLight("Gate Ember Fill", new Vector3(-1.2f, 3.4f, 2f), new Color(1f, 0.28f, 0.055f), 2.15f, 8f);
            ConfigureVolume();
        }

        private static void ConfigureSun()
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
            sun.color = new Color(0.70f, 0.82f, 1f);
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.68f;
            sun.shadowBias = 0.045f;
            sun.shadowNormalBias = 0.32f;
            sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
            RenderSettings.sun = sun;
        }

        private static void EnsureFillLight(string lightName, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = GameObject.Find(lightName);
            if (lightObject == null) lightObject = new GameObject(lightName);
            lightObject.transform.position = position;
            Light light = lightObject.GetComponent<Light>();
            if (light == null) light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
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

            Bloom bloom = runtimeProfile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(1.08f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.58f);
            bloom.clamp.Override(4.5f);

            Tonemapping tonemapping = runtimeProfile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = runtimeProfile.Add<ColorAdjustments>();
            color.postExposure.Override(0.08f);
            color.contrast.Override(13f);
            color.saturation.Override(7f);
            color.colorFilter.Override(new Color(0.95f, 0.98f, 1f));

            WhiteBalance whiteBalance = runtimeProfile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(-7f);
            whiteBalance.tint.Override(2f);

            Vignette vignette = runtimeProfile.Add<Vignette>();
            vignette.color.Override(new Color(0.01f, 0.025f, 0.055f));
            vignette.intensity.Override(0.13f);
            vignette.smoothness.Override(0.48f);
        }

        private void OnDestroy()
        {
            if (runtimeProfile != null) Destroy(runtimeProfile);
        }
    }
}
