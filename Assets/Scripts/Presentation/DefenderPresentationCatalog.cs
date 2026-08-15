using System;
using System.Collections.Generic;
using System.Linq;
using DefenderChronicles.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace DefenderChronicles.Presentation
{
    public enum PresentationActorKind
    {
        Knight,
        Guard,
        Raider,
        Vanguard,
        Regenerator,
        Support,
        Siege,
        Boss
    }

    public enum PresentationCue
    {
        UiClick,
        UiError,
        OutpostBuilt,
        OutpostUpgraded,
        OutpostSold,
        UnitSpawned,
        UnitDefeated,
        EnemyHit,
        EnemyDefeated,
        HeroMoved,
        HeroAttacked,
        HeroAbility,
        OutpostAttacked,
        MageImpact,
        HealPulse,
        OutpostDisrupted,
        SlowApplied,
        BossPhase,
        BossAttack,
        WaveStarted,
        Victory,
        Defeat
    }

    public enum MusicState
    {
        Preparation,
        Combat,
        Intense,
        Boss,
        Victory,
        Defeat
    }

    public enum AssetLicenseKind
    {
        Original,
        OpenFontLicense,
        CreativeCommonsZero,
        OpenAIGenerated
    }

    [Serializable]
    public sealed class AssetProvenanceEntry
    {
        public string assetName;
        public string assetPath;
        public AssetLicenseKind license;
        public string sourceUrl;
        public string acquiredOn;
    }

    [Serializable]
    public sealed class ActorVisualEntry
    {
        public PresentationActorKind kind;
        public GameObject prefab;
        [Min(0.1f)] public float scale = 1f;
    }

    [Serializable]
    public sealed class OutpostVisualEntry
    {
        public OutpostKind kind;
        [Range(1, 2)] public int level = 1;
        public GameObject prefab;
    }

    [Serializable]
    public sealed class AudioCueEntry
    {
        public PresentationCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.75f;
        [Range(0f, 0.15f)] public float pitchVariation = 0.035f;
        [Range(1, 8)] public int concurrency = 3;
    }

    [Serializable]
    public sealed class MusicEntry
    {
        public MusicState state;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.55f;
        public bool loop = true;
    }

    [CreateAssetMenu(menuName = "Defender Chronicles/Presentation Catalog", fileName = "DefenderPresentationCatalog")]
    public sealed class DefenderPresentationCatalog : ScriptableObject
    {
        [Header("Environment")]
        public GameObject environmentPrefab;
        public GameObject outpostSlotPrefab;
        public GameObject commandPointPrefab;

        [Header("Actors and Outposts")]
        public ActorVisualEntry[] actorVisuals = Array.Empty<ActorVisualEntry>();
        public OutpostVisualEntry[] outpostVisuals = Array.Empty<OutpostVisualEntry>();

        [Header("HUD")]
        public GameObject hudPrefab;
        public Sprite panelSprite;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset titleFont;
        public UiCopyCatalog uiCopy;
        public Sprite goldIcon;
        public Sprite keepIcon;
        public Sprite waveIcon;
        public Sprite archerIcon;
        public Sprite barracksIcon;
        public Sprite mageIcon;
        public Sprite abilityIcon;

        [Header("Animation")]
        public RuntimeAnimatorController actorAnimator;
        public AnimationClip[] actorClips = Array.Empty<AnimationClip>();

        [Header("Shared Palette")]
        public Material navyMaterial;
        public Material defenderMaterial;
        public Material stoneMaterial;
        public Material forestMaterial;
        public Material enemyMaterial;
        public Material magicMaterial;
        public Material fireMaterial;
        public Material darkMaterial;

        [Header("Audio")]
        public AudioMixerGroup uiMixer;
        public AudioMixerGroup sfxMixer;
        public AudioMixerGroup musicMixer;
        public AudioMixerSnapshot normalSnapshot;
        public AudioMixerSnapshot pauseSnapshot;
        public AudioCueEntry[] audioCues = Array.Empty<AudioCueEntry>();
        public MusicEntry[] music = Array.Empty<MusicEntry>();

        [Header("Provenance")]
        public TextAsset fontLicense;
        public TextAsset audioLicense;
        public AssetProvenanceEntry[] provenance = Array.Empty<AssetProvenanceEntry>();

        public GameObject ActorPrefab(PresentationActorKind kind)
        {
            return actorVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind)?.prefab;
        }

        public float ActorScale(PresentationActorKind kind)
        {
            return actorVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind)?.scale ?? 1f;
        }

        public GameObject OutpostPrefab(OutpostKind kind, int level)
        {
            int clamped = Mathf.Clamp(level, 1, 2);
            return outpostVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind && entry.level == clamped)?.prefab;
        }

        public AudioCueEntry Audio(PresentationCue cue)
        {
            return audioCues.FirstOrDefault(entry => entry != null && entry.cue == cue);
        }

        public MusicEntry Music(MusicState state)
        {
            return music.FirstOrDefault(entry => entry != null && entry.state == state);
        }

        public Material ResolveMaterial(string sourceName)
        {
            string value = sourceName?.ToLowerInvariant() ?? string.Empty;
            if (value.Contains("navy")) return navyMaterial;
            if (value.Contains("defender") || value.Contains("blue")) return defenderMaterial;
            if (value.Contains("stone") || value.Contains("metal")) return stoneMaterial;
            if (value.Contains("forest") || value.Contains("green")) return forestMaterial;
            if (value.Contains("enemy") || value.Contains("crimson") || value.Contains("red")) return enemyMaterial;
            if (value.Contains("magic") || value.Contains("cyan")) return magicMaterial;
            if (value.Contains("fire") || value.Contains("orange") || value.Contains("gold")) return fireMaterial;
            return darkMaterial;
        }

        public IReadOnlyList<string> ValidationErrors()
        {
            var errors = new List<string>();
            if (environmentPrefab == null) errors.Add("Environment prefab is missing.");
            if (outpostSlotPrefab == null) errors.Add("Outpost slot prefab is missing.");
            if (commandPointPrefab == null) errors.Add("Command point prefab is missing.");
            if (hudPrefab == null) errors.Add("HUD prefab is missing.");
            if (panelSprite == null) errors.Add("HUD panel sprite is missing.");
            if (bodyFont == null || titleFont == null) errors.Add("TMP font assets are incomplete.");
            if (uiCopy == null || uiCopy.ValidationErrors().Count > 0) errors.Add("Turkish UI copy is incomplete.");
            if (new[] { goldIcon, keepIcon, waveIcon, archerIcon, barracksIcon, mageIcon, abilityIcon }
                .Any(item => item == null)) errors.Add("HUD icon sprites are incomplete.");
            if (actorAnimator == null) errors.Add("Shared actor AnimatorController is missing.");
            foreach (string clipName in new[] { "Idle", "Move", "Attack", "Hit", "Defeat", "Ability", "Victory", "Phase", "BossAttack" })
                if (actorClips.All(clip => clip == null || clip.name != clipName))
                    errors.Add($"Actor animation clip is missing: {clipName}.");

            foreach (PresentationActorKind kind in Enum.GetValues(typeof(PresentationActorKind)))
                if (ActorPrefab(kind) == null) errors.Add($"Actor prefab is missing: {kind}.");
            foreach (OutpostKind kind in Enum.GetValues(typeof(OutpostKind)))
                for (int level = 1; level <= 2; level++)
                    if (OutpostPrefab(kind, level) == null) errors.Add($"Outpost prefab is missing: {kind} level {level}.");

            foreach (PresentationCue cue in Enum.GetValues(typeof(PresentationCue)))
                if (Audio(cue)?.clip == null) errors.Add($"Audio cue is missing: {cue}.");
            foreach (MusicState state in Enum.GetValues(typeof(MusicState)))
                if (Music(state)?.clip == null) errors.Add($"Music state is missing: {state}.");
            if (uiMixer == null || sfxMixer == null || musicMixer == null || normalSnapshot == null || pauseSnapshot == null)
                errors.Add("Audio mixer groups are incomplete.");
            if (uiMixer == sfxMixer || uiMixer == musicMixer || sfxMixer == musicMixer)
                errors.Add("UI, SFX and music must use separate mixer groups.");

            if (fontLicense == null) errors.Add("Noto font license file is missing.");
            if (audioLicense == null) errors.Add("CC0 audio license file is missing.");
            foreach (string required in new[] { "Noto Sans", "Noto Serif", "HUD panel", "HUD icons", "Blender source", "CC0 sound effects", "CC0 music" })
            {
                AssetProvenanceEntry entry = provenance.FirstOrDefault(item => item != null && item.assetName == required);
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetPath) || string.IsNullOrWhiteSpace(entry.acquiredOn))
                    errors.Add($"Asset provenance is missing: {required}.");
                else if ((entry.license is AssetLicenseKind.OpenFontLicense or AssetLicenseKind.CreativeCommonsZero) &&
                         string.IsNullOrWhiteSpace(entry.sourceUrl))
                    errors.Add($"Licensed asset source URL is missing: {required}.");
            }

            if (new[] { navyMaterial, defenderMaterial, stoneMaterial, forestMaterial, enemyMaterial,
                    magicMaterial, fireMaterial, darkMaterial }.Any(item => item == null))
                errors.Add("Shared palette materials are incomplete.");
            return errors;
        }
    }
}
