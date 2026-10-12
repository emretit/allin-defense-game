using System;
using System.Collections;
using AllinDefense.UI;
using UnityEngine;
using UnityEngine.Audio;

namespace AllinDefense.Presentation
{
    public readonly struct DefenderAmbientQualityConfig
    {
        public readonly bool enabled;
        public readonly float windVolume;
        public readonly float detailVolume;
        public readonly float minimumDetailInterval;
        public readonly float maximumDetailInterval;

        public DefenderAmbientQualityConfig(bool enabled, float windVolume, float detailVolume,
            float minimumDetailInterval, float maximumDetailInterval)
        {
            this.enabled = enabled;
            this.windVolume = windVolume;
            this.detailVolume = detailVolume;
            this.minimumDetailInterval = minimumDetailInterval;
            this.maximumDetailInterval = maximumDetailInterval;
        }
    }

    public sealed class DefenderAmbientAudioController : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private readonly System.Random random = new(41370);
        private DefenderAmbientQualityConfig config;
        private AudioSource windSource;
        private AudioSource detailSource;
        private AudioClip windClip;
        private AudioClip leafClip;
        private AudioClip crystalClip;
        private Coroutine detailRoutine;
        private float reviewGain = 1f;
        private float stateGain = 1f;
        private bool paused;
        private int detailIndex;

        public bool IsEnabled => config.enabled;
        public int PlayedDetailCount { get; private set; }

        private void OnEnable()
        {
            FrontendPreferences.AudioVolumesChanged += ApplyVolumes;
            ApplyVolumes();
        }

        private void OnDisable()
        {
            FrontendPreferences.AudioVolumesChanged -= ApplyVolumes;
        }

        public void Initialize(AudioMixerGroup mixer)
        {
            string qualityName = QualitySettings.names.Length > 0
                ? QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]
                : "Medium";
            config = QualityConfigForName(qualityName);
            if (Environment.GetEnvironmentVariable("DC_AMBIENT_DISABLE") == "1")
                config = QualityConfigForName("Low");
            if (!Application.isEditor)
                Debug.Log($"[DC_AMBIENT_CONFIG] quality={qualityName} enabled={config.enabled} " +
                          $"wind={config.windVolume:F3} detail={config.detailVolume:F3} " +
                          $"interval={config.minimumDetailInterval:F0}-{config.maximumDetailInterval:F0}");
            if (!config.enabled) return;

            windSource = CreateSource("Ambient Wind", mixer, true);
            detailSource = CreateSource("Ambient Detail", mixer, false);
            windClip = CreateWindClip();
            leafClip = CreateLeafClip();
            crystalClip = CreateCrystalClip();
            windSource.clip = windClip;
            ApplyVolumes();
            windSource.Play();
            detailRoutine = StartCoroutine(PlayDetails());
        }

        public void SetReviewGain(float gain)
        {
            reviewGain = Mathf.Clamp01(gain);
            ApplyVolumes();
        }

        public void SetMusicState(MusicState state)
        {
            stateGain = state switch
            {
                MusicState.Preparation => 1f,
                MusicState.Combat => 0.68f,
                MusicState.Intense => 0.52f,
                MusicState.Boss => 0.38f,
                MusicState.Victory => 0.30f,
                MusicState.Defeat => 0.22f,
                _ => 0.65f
            };
            ApplyVolumes();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            ApplyVolumes();
        }

        public static DefenderAmbientQualityConfig QualityConfigForName(string qualityName)
        {
            if (qualityName?.IndexOf("low", StringComparison.OrdinalIgnoreCase) >= 0)
                return new DefenderAmbientQualityConfig(false, 0f, 0f, 0f, 0f);
            if (qualityName?.IndexOf("high", StringComparison.OrdinalIgnoreCase) >= 0)
                return new DefenderAmbientQualityConfig(true, 0.075f, 0.11f, 5f, 10f);
            return new DefenderAmbientQualityConfig(true, 0.055f, 0.08f, 7f, 12f);
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup mixer, bool loop)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = 224;
            source.outputAudioMixerGroup = mixer;
            return source;
        }

        private IEnumerator PlayDetails()
        {
            while (true)
            {
                float interval = Mathf.Lerp(config.minimumDetailInterval, config.maximumDetailInterval,
                    (float)random.NextDouble());
                yield return new WaitForSecondsRealtime(interval);
                if (paused || reviewGain <= 0f || detailSource == null) continue;
                detailSource.clip = detailIndex++ % 3 == 2 ? crystalClip : leafClip;
                detailSource.pitch = Mathf.Lerp(0.94f, 1.06f, (float)random.NextDouble());
                detailSource.Play();
                PlayedDetailCount++;
                if (!Application.isEditor)
                    Debug.Log($"[DC_AMBIENT_DETAIL] count={PlayedDetailCount} clip={detailSource.clip.name} " +
                              $"review_gain={reviewGain:F2} state_gain={stateGain:F2}");
            }
        }

        private void ApplyVolumes()
        {
            float audible = paused ? 0f : reviewGain * stateGain * FrontendPreferences.SfxVolume;
            if (windSource != null) windSource.volume = config.windVolume * audible;
            if (detailSource != null) detailSource.volume = config.detailVolume * audible;
        }

        private static AudioClip CreateWindClip()
        {
            int sampleCount = SampleRate * 4;
            float[] samples = new float[sampleCount];
            uint state = 0x43a7u;
            float filtered = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / 8388607.5f) - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.012f);
                float time = index / (float)SampleRate;
                float breath = 0.62f + 0.22f * Mathf.Sin(time * Mathf.PI * 0.5f) +
                               0.10f * Mathf.Sin(time * Mathf.PI * 1.5f);
                samples[index] = filtered * breath * 0.32f;
            }
            return CreateClip("DC_Original_WindBed", samples);
        }

        private static AudioClip CreateLeafClip()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.34f);
            float[] samples = new float[sampleCount];
            uint state = 0x91c5u;
            float filtered = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                state = state * 1103515245u + 12345u;
                float noise = ((state >> 9) / 4194303.5f) - 1f;
                filtered = Mathf.Lerp(filtered, noise, 0.18f);
                float t = index / (float)sampleCount;
                float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Exp(-1.8f * t);
                samples[index] = filtered * envelope * 0.48f;
            }
            return CreateClip("DC_Original_LeafRustle", samples);
        }

        private static AudioClip CreateCrystalClip()
        {
            int sampleCount = Mathf.RoundToInt(SampleRate * 0.72f);
            float[] samples = new float[sampleCount];
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float envelope = Mathf.Exp(-5.4f * time);
                samples[index] = envelope * (Mathf.Sin(2f * Mathf.PI * 740f * time) * 0.34f +
                                             Mathf.Sin(2f * Mathf.PI * 1110f * time) * 0.17f);
            }
            return CreateClip("DC_Original_CrystalChime", samples);
        }

        private static AudioClip CreateClip(string clipName, float[] samples)
        {
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (detailRoutine != null) StopCoroutine(detailRoutine);
            if (windClip != null) Destroy(windClip);
            if (leafClip != null) Destroy(leafClip);
            if (crystalClip != null) Destroy(crystalClip);
        }
    }
}
