using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace DefenderChronicles.Presentation
{
    public sealed class DefenderAudioController : MonoBehaviour
    {
        private readonly List<AudioSource> sfxSources = new();
        private DefenderPresentationCatalog catalog;
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource activeMusic;
        private Coroutine musicRoutine;
        private MusicState currentState;

        public MusicState CurrentState => currentState;

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
            activeMusic = musicA;
            SetMusic(MusicState.Preparation, true);
        }

        public void Play(PresentationCue cue)
        {
            AudioCueEntry entry = catalog?.Audio(cue);
            if (entry?.clip == null) return;
            int playing = sfxSources.Count(source => source.isPlaying && source.clip == entry.clip);
            if (playing >= entry.concurrency) return;
            AudioSource target = sfxSources.FirstOrDefault(source => !source.isPlaying) ?? sfxSources[0];
            target.Stop();
            target.clip = entry.clip;
            target.volume = entry.volume;
            target.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            target.outputAudioMixerGroup = cue is PresentationCue.UiClick or PresentationCue.UiError
                ? catalog.uiMixer
                : catalog.sfxMixer;
            target.Play();
        }

        public void SetMusic(MusicState state, bool immediate = false)
        {
            if (catalog == null || (!immediate && currentState == state)) return;
            MusicEntry entry = catalog.Music(state);
            if (entry?.clip == null) return;
            currentState = state;
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(Crossfade(entry, immediate ? 0f : 1.1f));
        }

        public void SetPaused(bool paused)
        {
            AudioMixerSnapshot snapshot = paused ? catalog?.pauseSnapshot : catalog?.normalSnapshot;
            snapshot?.TransitionTo(0.18f);
            foreach (AudioSource source in sfxSources) source.mute = paused;
            if (musicA != null) musicA.mute = paused;
            if (musicB != null) musicB.mute = paused;
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
            incoming.volume = duration <= 0f ? entry.volume : 0f;
            incoming.Play();
            if (duration <= 0f)
            {
                outgoing.Stop();
                activeMusic = incoming;
                musicRoutine = null;
                yield break;
            }

            float startVolume = outgoing.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                outgoing.volume = Mathf.Lerp(startVolume, 0f, t);
                incoming.volume = Mathf.Lerp(0f, entry.volume, t);
                yield return null;
            }
            outgoing.Stop();
            activeMusic = incoming;
            musicRoutine = null;
        }
    }
}
