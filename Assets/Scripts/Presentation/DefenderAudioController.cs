using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace DefenderChronicles.Presentation
{
    public sealed class DefenderAudioController : MonoBehaviour
    {
        private sealed class ActiveVoice
        {
            public PresentationCue cue;
            public string group;
            public PresentationAudioPriority priority;
            public float baseVolume;
            public float startedAt;
        }

        private readonly List<AudioSource> sfxSources = new();
        private readonly Dictionary<AudioSource, ActiveVoice> activeVoices = new();
        private DefenderPresentationCatalog catalog;
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private DefenderAmbientAudioController ambientController;
        private Coroutine musicRoutine;
        private Coroutine duckRoutine;
        private MusicState currentState;
        private PresentationAudioReviewLevel reviewLevel = PresentationAudioReviewLevel.Normal;
        private float reviewGain = 1f;
        private float musicDuckMultiplier = 1f;
        private float musicANominalVolume;
        private float musicBNominalVolume;

        public MusicState CurrentState => currentState;
        public PresentationAudioReviewLevel ReviewLevel => reviewLevel;
        public DefenderAmbientAudioController Ambient => ambientController;
        public int ActiveVoiceCount => activeVoices.Count(pair => pair.Key != null && pair.Key.isPlaying);

        public void Initialize(DefenderPresentationCatalog value)
        {
            catalog = value;
            for (int i = 0; i < 10; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = catalog.sfxMixer;
                sfxSources.Add(source);
            }
            musicA = CreateMusicSource("Music A");
            musicB = CreateMusicSource("Music B");
            ambientController = gameObject.AddComponent<DefenderAmbientAudioController>();
            ambientController.Initialize(catalog.sfxMixer);
            activeMusic = musicA;
            string requestedLevel = Environment.GetEnvironmentVariable("DC_AUDIO_REVIEW_LEVEL");
            if (Enum.TryParse(requestedLevel, true, out PresentationAudioReviewLevel parsedLevel))
                SetReviewLevel(parsedLevel);
            SetMusic(MusicState.Preparation, true);
        }

        public void Play(PresentationCue cue)
        {
            Play(new PresentationSignal(cue, Vector3.zero));
        }

        public void Play(PresentationSignal signal)
        {
            PresentationAudioCueSpec spec = PresentationAudioPolicy.Describe(signal);
            if (spec.delaySeconds > 0f)
                StartCoroutine(PlayDelayed(signal, spec));
            else
                PlayNow(signal.cue, spec);
        }

        public void SetReviewLevel(PresentationAudioReviewLevel level)
        {
            reviewLevel = level;
            reviewGain = PresentationAudioPolicy.ReviewGain(level);
            ambientController?.SetReviewGain(reviewGain);
            foreach ((AudioSource source, ActiveVoice voice) in activeVoices)
            {
                if (source != null) source.volume = voice.baseVolume * reviewGain;
            }
            ApplyMusicVolume(musicA);
            ApplyMusicVolume(musicB);
        }

        private IEnumerator PlayDelayed(PresentationSignal signal, PresentationAudioCueSpec spec)
        {
            yield return new WaitForSecondsRealtime(spec.delaySeconds);
            PlayNow(signal.cue, spec);
        }

        private void PlayNow(PresentationCue cue, PresentationAudioCueSpec spec)
        {
            AudioCueEntry entry = catalog?.Audio(cue);
            if (entry?.clip == null) return;
            RemoveFinishedVoices();

            int clipLimit = Mathf.Max(1, Mathf.Min(entry.concurrency, spec.maxConcurrent));
            int clipPlaying = activeVoices.Count(pair => pair.Key.isPlaying && pair.Key.clip == entry.clip);
            if (clipPlaying >= clipLimit) return;

            AudioSource target = null;
            List<KeyValuePair<AudioSource, ActiveVoice>> groupVoices = activeVoices
                .Where(pair => pair.Key.isPlaying && pair.Value.group == spec.concurrencyGroup)
                .OrderBy(pair => pair.Value.priority)
                .ThenBy(pair => pair.Value.startedAt)
                .ToList();
            if (groupVoices.Count >= spec.maxConcurrent)
            {
                KeyValuePair<AudioSource, ActiveVoice> victim = groupVoices[0];
                if (spec.priority <= victim.Value.priority) return;
                target = victim.Key;
            }

            target ??= sfxSources.FirstOrDefault(source => !source.isPlaying);
            if (target == null)
            {
                KeyValuePair<AudioSource, ActiveVoice> victim = activeVoices
                    .OrderBy(pair => pair.Value.priority)
                    .ThenBy(pair => pair.Value.startedAt)
                    .First();
                if (!PresentationAudioPolicy.CanReplace(victim.Value.priority, spec.priority)) return;
                target = victim.Key;
            }

            target.Stop();
            target.clip = entry.clip;
            target.volume = entry.volume * reviewGain;
            target.pitch = 1f + UnityEngine.Random.Range(-entry.pitchVariation, entry.pitchVariation);
            target.priority = PresentationAudioPolicy.UnityPriority(spec.priority);
            target.outputAudioMixerGroup = cue is PresentationCue.UiClick or PresentationCue.UiError
                ? catalog.uiMixer
                : catalog.sfxMixer;
            activeVoices[target] = new ActiveVoice
            {
                cue = cue,
                group = spec.concurrencyGroup,
                priority = spec.priority,
                baseVolume = entry.volume,
                startedAt = Time.unscaledTime
            };
            target.Play();

            if (spec.musicDuck > 0f)
            {
                if (duckRoutine != null) StopCoroutine(duckRoutine);
                duckRoutine = StartCoroutine(DuckMusic(spec.musicDuck, spec.duckSeconds));
            }
        }

        public void SetMusic(MusicState state, bool immediate = false)
        {
            if (catalog == null || (!immediate && currentState == state)) return;
            MusicEntry entry = catalog.Music(state);
            if (entry?.clip == null) return;
            currentState = state;
            ambientController?.SetMusicState(state);
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            float duration = immediate ? 0f : PresentationAudioPolicy.MusicTransitionSeconds(state);
            musicRoutine = StartCoroutine(Crossfade(entry, duration));
        }

        public void SetPaused(bool paused)
        {
            AudioMixerSnapshot snapshot = paused ? catalog?.pauseSnapshot : catalog?.normalSnapshot;
            snapshot?.TransitionTo(0.18f);
            foreach (AudioSource source in sfxSources) source.mute = paused;
            if (musicA != null) musicA.mute = paused;
            if (musicB != null) musicB.mute = paused;
            ambientController?.SetPaused(paused);
        }

        private AudioSource CreateMusicSource(string sourceName)
        {
            var host = new GameObject(sourceName);
            host.transform.SetParent(transform, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = catalog.musicMixer;
            return source;
        }

        private IEnumerator Crossfade(MusicEntry entry, float duration)
        {
            AudioSource outgoing = activeMusic;
            AudioSource incoming = activeMusic == musicA ? musicB : musicA;
            incoming.clip = entry.clip;
            incoming.loop = entry.loop;
            SetNominalMusicVolume(incoming, duration <= 0f ? entry.volume : 0f);
            incoming.Play();
            if (duration <= 0f)
            {
                outgoing.Stop();
                SetNominalMusicVolume(outgoing, 0f);
                activeMusic = incoming;
                musicRoutine = null;
                yield break;
            }

            float startVolume = NominalMusicVolume(outgoing);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetNominalMusicVolume(outgoing, Mathf.Lerp(startVolume, 0f, t));
                SetNominalMusicVolume(incoming, Mathf.Lerp(0f, entry.volume, t));
                yield return null;
            }
            outgoing.Stop();
            SetNominalMusicVolume(outgoing, 0f);
            activeMusic = incoming;
            musicRoutine = null;
        }

        private IEnumerator DuckMusic(float amount, float seconds)
        {
            musicDuckMultiplier = 1f - Mathf.Clamp01(amount);
            ApplyMusicVolume(musicA);
            ApplyMusicVolume(musicB);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, seconds));
            musicDuckMultiplier = 1f;
            ApplyMusicVolume(musicA);
            ApplyMusicVolume(musicB);
            duckRoutine = null;
        }

        private void RemoveFinishedVoices()
        {
            foreach (AudioSource source in activeVoices.Keys.Where(source => source == null || !source.isPlaying).ToArray())
                activeVoices.Remove(source);
        }

        private float NominalMusicVolume(AudioSource source)
        {
            return source == musicA ? musicANominalVolume : musicBNominalVolume;
        }

        private void SetNominalMusicVolume(AudioSource source, float volume)
        {
            if (source == null) return;
            if (source == musicA) musicANominalVolume = volume;
            else if (source == musicB) musicBNominalVolume = volume;
            ApplyMusicVolume(source);
        }

        private void ApplyMusicVolume(AudioSource source)
        {
            if (source == null) return;
            source.volume = NominalMusicVolume(source) * reviewGain * musicDuckMultiplier;
        }
    }
}
