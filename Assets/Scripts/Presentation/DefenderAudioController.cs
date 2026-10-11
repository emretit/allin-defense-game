using System;
using System.Collections;
using System.Collections.Generic;
using DefenderChronicles.UI;
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
        private readonly Dictionary<AudioSource, ActiveVoice> activeVoices = new(10);
        private readonly Dictionary<AudioSource, ActiveVoice> voiceRecords = new(10);
        private readonly AudioSource[] finishedSources = new AudioSource[10];
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
        public int ActiveVoiceCount
        {
            get
            {
                int count = 0;
                foreach (var pair in activeVoices)
                    if (pair.Key != null && pair.Key.isPlaying) count++;
                return count;
            }
        }

        private void OnEnable()
        {
            FrontendPreferences.AudioVolumesChanged += ApplyAudioVolumes;
            ApplyAudioVolumes();
        }

        private void OnDisable()
        {
            FrontendPreferences.AudioVolumesChanged -= ApplyAudioVolumes;
        }

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
                voiceRecords.Add(source, new ActiveVoice());
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
            if (ambientController != null) ambientController.SetReviewGain(reviewGain);
            ApplyAudioVolumes();
        }

        private void ApplyAudioVolumes()
        {
            foreach ((AudioSource source, ActiveVoice voice) in activeVoices)
            {
                if (source != null) source.volume = voice.baseVolume * reviewGain * FrontendPreferences.SfxVolume;
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
            AudioCueEntry entry = catalog != null ? catalog.Audio(cue) : null;
            if (entry?.clip == null) return;
            RemoveFinishedVoices();

            AudioSource target = SelectVoice(entry, spec);
            if (target == null) return;

            target.Stop();
            target.clip = entry.clip;
            target.volume = entry.volume * reviewGain * FrontendPreferences.SfxVolume;
            target.pitch = 1f + UnityEngine.Random.Range(-entry.pitchVariation, entry.pitchVariation);
            target.priority = PresentationAudioPolicy.UnityPriority(spec.priority);
            target.outputAudioMixerGroup = cue is PresentationCue.UiClick or PresentationCue.UiError
                ? catalog.uiMixer
                : catalog.sfxMixer;
            ActiveVoice voice = voiceRecords[target];
            voice.cue = cue;
            voice.group = spec.concurrencyGroup;
            voice.priority = spec.priority;
            voice.baseVolume = entry.volume;
            voice.startedAt = Time.unscaledTime;
            activeVoices[target] = voice;
            target.Play();

            if (spec.musicDuck > 0f)
            {
                if (duckRoutine != null) StopCoroutine(duckRoutine);
                duckRoutine = StartCoroutine(DuckMusic(spec.musicDuck, spec.duckSeconds));
            }
        }

        private AudioSource SelectVoice(AudioCueEntry entry, PresentationAudioCueSpec spec)
        {
            int clipPlaying = 0;
            int groupPlaying = 0;
            AudioSource groupVictim = null;
            AudioSource globalVictim = null;
            ActiveVoice groupVoice = null;
            ActiveVoice globalVoice = null;
            // Strict comparisons retain the original stable LINQ ordering on equal keys.
            foreach (var pair in activeVoices)
            {
                AudioSource source = pair.Key;
                ActiveVoice voice = pair.Value;
                bool playing = source.isPlaying;
                if (playing && source.clip == entry.clip) clipPlaying++;
                if (globalVoice == null || ComesBefore(voice, globalVoice))
                {
                    globalVictim = source;
                    globalVoice = voice;
                }
                if (!playing || voice.group != spec.concurrencyGroup) continue;
                groupPlaying++;
                if (groupVoice == null || ComesBefore(voice, groupVoice))
                {
                    groupVictim = source;
                    groupVoice = voice;
                }
            }
            int clipLimit = Mathf.Max(1, Mathf.Min(entry.concurrency, spec.maxConcurrent));
            if (clipPlaying >= clipLimit) return null;
            if (groupPlaying >= spec.maxConcurrent)
                return spec.priority > groupVoice.priority ? groupVictim : null;
            foreach (AudioSource source in sfxSources)
                if (!source.isPlaying) return source;
            return globalVoice != null && PresentationAudioPolicy.CanReplace(globalVoice.priority, spec.priority)
                ? globalVictim : null;
        }

        private static bool ComesBefore(ActiveVoice voice, ActiveVoice other) =>
            voice.priority < other.priority ||
            (voice.priority == other.priority && voice.startedAt < other.startedAt);

        public void SetMusic(MusicState state, bool immediate = false)
        {
            if (catalog == null || (!immediate && currentState == state)) return;
            MusicEntry entry = catalog.Music(state);
            if (entry?.clip == null) return;
            currentState = state;
            if (ambientController != null) ambientController.SetMusicState(state);
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            float duration = immediate ? 0f : PresentationAudioPolicy.MusicTransitionSeconds(state);
            musicRoutine = StartCoroutine(Crossfade(entry, duration));
        }

        public void SetPaused(bool paused)
        {
            AudioMixerSnapshot snapshot = catalog != null ? (paused ? catalog.pauseSnapshot : catalog.normalSnapshot) : null;
            if (snapshot != null) snapshot.TransitionTo(0.18f);
            foreach (AudioSource source in sfxSources) source.mute = paused;
            if (musicA != null) musicA.mute = paused;
            if (musicB != null) musicB.mute = paused;
            if (ambientController != null) ambientController.SetPaused(paused);
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
            int count = 0;
            foreach (var pair in activeVoices)
                if (pair.Key == null || !pair.Key.isPlaying) finishedSources[count++] = pair.Key;
            for (int i = 0; i < count; i++)
            {
                activeVoices.Remove(finishedSources[i]);
                finishedSources[i] = null;
            }
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
            source.volume = NominalMusicVolume(source) * reviewGain * musicDuckMultiplier * FrontendPreferences.MusicVolume;
        }
    }
}
