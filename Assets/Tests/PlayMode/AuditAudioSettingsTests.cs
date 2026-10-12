using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AllinDefense.Tests
{
    public sealed class AuditAudioSettingsTests
    {
        private const string MusicKey = "AllinDefense.Frontend.Music";
        private const string SfxKey = "AllinDefense.Frontend.Sfx";
        private GameObject host;
        private DefenderAudioController controller;
        private DefenderPresentationCatalog catalog;
        private AudioClip musicClip;
        private AudioClip cueClip;
        private bool hadMusic;
        private bool hadSfx;
        private float savedMusic;
        private float savedSfx;
        private int quality;
        private AppFlowController frontend;
        private GameSessionController frontendSession;
        private string frontendSavePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadMusic = PlayerPrefs.HasKey(MusicKey);
            hadSfx = PlayerPrefs.HasKey(SfxKey);
            savedMusic = FrontendPreferences.MusicVolume;
            savedSfx = FrontendPreferences.SfxVolume;
            FrontendPreferences.MusicVolume = 1f;
            FrontendPreferences.SfxVolume = 1f;
            quality = QualitySettings.GetQualityLevel();
            int medium = System.Array.FindIndex(QualitySettings.names, name => name == "Medium");
            if (medium >= 0) QualitySettings.SetQualityLevel(medium, false);
            musicClip = AudioClip.Create("Audit Music", 44100 * 4, 1, 44100, false);
            cueClip = AudioClip.Create("Audit Cue", 44100 * 4, 1, 44100, false);
            catalog = ScriptableObject.CreateInstance<DefenderPresentationCatalog>();
            catalog.music = new[]
            {
                new MusicEntry { state = MusicState.Preparation, clip = musicClip, volume = 0.6f },
                new MusicEntry { state = MusicState.Combat, clip = musicClip, volume = 0.4f }
            };
            catalog.audioCues = new[]
            {
                new AudioCueEntry { cue = PresentationCue.UiClick, clip = cueClip, volume = 0.7f },
                new AudioCueEntry { cue = PresentationCue.EnemyHit, clip = cueClip, volume = 0.7f },
                new AudioCueEntry { cue = PresentationCue.HeroAbility, clip = cueClip, volume = 0.7f }
            };
            Initialize();
            yield return null; // Let newly attached AudioSources finish their first audio update.
        }

        [TearDown]
        public void TearDown()
        {
            if (frontend != null) Object.DestroyImmediate(frontend.gameObject);
            if (frontendSession != null) Object.DestroyImmediate(frontendSession.gameObject);
            if (frontendSavePath != null && File.Exists(frontendSavePath)) File.Delete(frontendSavePath);
            if (host != null) Object.DestroyImmediate(host);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(musicClip);
            Object.DestroyImmediate(cueClip);
            FrontendPreferences.MusicVolume = savedMusic;
            FrontendPreferences.SfxVolume = savedSfx;
            if (!hadMusic) PlayerPrefs.DeleteKey(MusicKey);
            if (!hadSfx) PlayerPrefs.DeleteKey(SfxKey);
            PlayerPrefs.Save();
            QualitySettings.SetQualityLevel(quality, false);
        }

        private void Initialize()
        {
            if (host != null) return;
            host = new GameObject("Audit Battle Audio");
            controller = host.AddComponent<DefenderAudioController>();
            controller.Initialize(catalog);
            controller.SetReviewLevel(PresentationAudioReviewLevel.Normal);
        }

        private AudioSource[] MusicSources => host.GetComponentsInChildren<AudioSource>()
            .Where(source => source.gameObject.name.StartsWith("Music ")).ToArray();

        private AudioSource CueSource => host.GetComponents<AudioSource>().Single(source => source.clip == cueClip);

        [UnityTest]
        public IEnumerator MissingPreferencesPreserveNominalBattleVolumes()
        {
            Object.DestroyImmediate(host);
            host = null;
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            Initialize();
            yield return null; // Recreated AudioSources need their first audio update before playing a cue.

            Assert.That(FrontendPreferences.MusicVolume, Is.EqualTo(1f));
            Assert.That(FrontendPreferences.SfxVolume, Is.EqualTo(1f));
            Assert.That(MusicSources.Sum(source => source.volume), Is.EqualTo(0.6f).Within(0.00001f));
            controller.Play(PresentationCue.EnemyHit);
            Assert.That(CueSource.volume, Is.EqualTo(0.7f).Within(0.00001f));
            AudioSource[] ambient = host.GetComponentsInChildren<AudioSource>();
            Assert.That(ambient.Single(source => source.name == "Ambient Wind").volume,
                Is.EqualTo(0.055f).Within(0.00001f));
            Assert.That(ambient.Single(source => source.name == "Ambient Detail").volume,
                Is.EqualTo(0.08f).Within(0.00001f));
        }

        [Test]
        public void FrontendDefaultAndEffectsSliderPreserveNominalUiVolume()
        {
            PlayerPrefs.DeleteKey(MusicKey);
            PlayerPrefs.DeleteKey(SfxKey);
            frontendSavePath = Path.Combine(Path.GetTempPath(), $"dc-audio-{System.Guid.NewGuid():N}.json");
            frontendSession = new GameObject("Audit Frontend Session").AddComponent<GameSessionController>();
            frontend = new GameObject("Audit Frontend Audio").AddComponent<AppFlowController>();
            frontend.Initialize(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"),
                frontendSession, false, frontendSavePath);
            AudioSource uiSource = frontend.GetComponent<AudioSource>();
            Assert.That(uiSource.volume, Is.EqualTo(0.80f).Within(0.00001f));

            typeof(AppFlowController).GetMethod("ShowScreen", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(frontend, new object[] { AppScreen.MainMenu });
            typeof(AppFlowController).GetMethod("OpenSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(frontend, null);
            Slider effects = frontend.GetComponentsInChildren<Slider>(true)
                .Single(slider => slider.transform.parent.name == "EFEKTLER Row");
            Assert.That(effects.value, Is.EqualTo(1f));
            foreach (float preference in new[] { 0.5f, 0f, 1f })
            {
                effects.value = preference;
                Assert.That(FrontendPreferences.SfxVolume, Is.EqualTo(preference));
                Assert.That(uiSource.volume, Is.EqualTo(0.80f * preference).Within(0.00001f));
                uiSource.volume = 0.123f;
                typeof(AppFlowController).GetMethod("ApplyStoredAudioPreferences", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(frontend, null);
                Assert.That(uiSource.volume, Is.EqualTo(0.80f * preference).Within(0.00001f));
            }
        }

        [TestCase(0f)]
        [TestCase(0.35f)]
        [TestCase(0.4f)]
        [TestCase(1f)]
        public void StoredMusicPreferenceAppliesWhenBattleStarts(float preference)
        {
            // Simulate persisted preferences loaded on the next launch, without a setter event.
            Object.DestroyImmediate(host);
            host = null;
            PlayerPrefs.SetFloat(MusicKey, preference);
            PlayerPrefs.Save();
            Initialize();
            Assert.That(MusicSources.Sum(source => source.volume), Is.EqualTo(0.6f * preference).Within(0.00001f));
        }

        [TestCase(PresentationCue.UiClick, 0f)]
        [TestCase(PresentationCue.EnemyHit, 0f)]
        [TestCase(PresentationCue.UiClick, 1f)]
        [TestCase(PresentationCue.EnemyHit, 1f)]
        public void CueUsesEffectsPreferenceAndPreservesReviewGain(PresentationCue cue, float preference)
        {
            FrontendPreferences.SfxVolume = preference;
            Initialize();
            controller.SetReviewLevel(PresentationAudioReviewLevel.Low);
            controller.Play(cue);
            Assert.That(CueSource.volume, Is.EqualTo(0.7f * 0.25f * preference).Within(0.00001f));
        }

        [Test]
        public void PreferenceChangesUpdatePlayingMusicAndCueImmediately()
        {
            Initialize();
            controller.Play(PresentationCue.UiClick);
            FrontendPreferences.MusicVolume = 0f;
            FrontendPreferences.SfxVolume = 0f;
            Assert.That(MusicSources.All(source => source.volume == 0f), Is.True);
            Assert.That(CueSource.volume, Is.Zero);
            FrontendPreferences.MusicVolume = 1f;
            FrontendPreferences.SfxVolume = 1f;
            Assert.That(MusicSources.Sum(source => source.volume), Is.EqualTo(0.6f).Within(0.00001f));
            Assert.That(CueSource.volume, Is.EqualTo(0.7f).Within(0.00001f));
        }

        [UnityTest]
        public IEnumerator PreferenceChangesPreserveBothCrossfadeChannelsAndDucking()
        {
            Initialize();
            controller.SetReviewLevel(PresentationAudioReviewLevel.Low);
            controller.SetMusic(MusicState.Combat);
            yield return null;
            controller.Play(PresentationCue.HeroAbility);
            float[] volumes = MusicSources.Select(source => source.volume).ToArray();
            Assert.That(volumes.All(volume => volume > 0f), Is.True);
            FrontendPreferences.MusicVolume = 0.5f;
            for (int i = 0; i < volumes.Length; i++)
                Assert.That(MusicSources[i].volume, Is.EqualTo(volumes[i] * 0.5f).Within(0.00001f));
            FrontendPreferences.MusicVolume = 0f;
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(MusicSources.All(source => source.volume == 0f), Is.True);
            FrontendPreferences.MusicVolume = 1f;
            Assert.That(MusicSources.Sum(source => source.volume), Is.EqualTo(0.4f * 0.25f).Within(0.00001f));
        }

        [Test]
        public void AmbientUsesEffectsPreferenceWithStateAndReviewGain()
        {
            Initialize();
            Assert.That(controller.Ambient.IsEnabled, Is.True);
            controller.SetMusic(MusicState.Combat, true);
            controller.SetReviewLevel(PresentationAudioReviewLevel.Low);
            AudioSource[] sources = host.GetComponentsInChildren<AudioSource>()
                .Where(source => source.gameObject.name.StartsWith("Ambient ")).ToArray();
            float[] volumes = sources.Select(source => source.volume).ToArray();
            Assert.That(sources, Has.Length.EqualTo(2));
            FrontendPreferences.MusicVolume = 0f;
            for (int i = 0; i < sources.Length; i++) Assert.That(sources[i].volume, Is.EqualTo(volumes[i]));
            FrontendPreferences.SfxVolume = 0f;
            Assert.That(sources.All(source => source.volume == 0f), Is.True);
            FrontendPreferences.SfxVolume = 1f;
            for (int i = 0; i < sources.Length; i++) Assert.That(sources[i].volume, Is.EqualTo(volumes[i]));
            controller.SetPaused(true);
            FrontendPreferences.SfxVolume = 0.5f;
            Assert.That(sources.All(source => source.volume == 0f), Is.True);
            controller.SetPaused(false);
            for (int i = 0; i < sources.Length; i++)
                Assert.That(sources[i].volume, Is.EqualTo(volumes[i] * 0.5f).Within(0.00001f));
        }

        [Test]
        public void ReenabledControllerAppliesPreferencesChangedWhileDisabled()
        {
            Initialize();
            controller.enabled = false;
            FrontendPreferences.MusicVolume = 0f;
            Assert.That(MusicSources.Sum(source => source.volume), Is.EqualTo(0.6f).Within(0.00001f));
            controller.enabled = true;
            Assert.That(MusicSources.All(source => source.volume == 0f), Is.True);
        }

        [Test]
        public void DestroyedControllersUnsubscribeFromPreferenceChanges()
        {
            Initialize();
            Object.DestroyImmediate(host);
            host = null;
            EventInfo changed = typeof(FrontendPreferences).GetEvent("AudioVolumesChanged");
            Assert.That(changed, Is.Not.Null);
            var subscribers = (System.Delegate)typeof(FrontendPreferences)
                .GetField("AudioVolumesChanged", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.That(subscribers == null || subscribers.GetInvocationList().All(subscriber =>
                !(subscriber.Target is DefenderAudioController) && !(subscriber.Target is DefenderAmbientAudioController)), Is.True);
            FrontendPreferences.MusicVolume = 0f;
            FrontendPreferences.SfxVolume = 0f;
        }
    }
}
