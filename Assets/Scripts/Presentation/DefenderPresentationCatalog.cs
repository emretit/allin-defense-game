using System;
using System.Collections.Generic;
using System.Linq;
using AllinDefense.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace AllinDefense.Presentation
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
        public RuntimeAnimatorController animatorOverride;
        [Min(0.1f)] public float scale = 1f;
    }

    [Serializable]
    public sealed class OutpostVisualEntry
    {
        public OutpostKind kind;
        [Range(1, 2)] public int level = 1;
        public GameObject prefab;
        [Min(0.1f)] public float scale = 1f;
    }

    [Serializable]
    public sealed class OutpostSpecializationVisualEntry
    {
        public OutpostSpecialization specialization;
        public GameObject prefab;
        [Min(0.1f)] public float scale = 1f;
    }

    [Serializable]
    public sealed class LicensedAtlasMaterialEntry
    {
        public string sourceName;
        public Material material;
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

    [CreateAssetMenu(menuName = "ALLIN: Defense/Presentation Catalog", fileName = "DefenderPresentationCatalog")]
    public sealed class DefenderPresentationCatalog : ScriptableObject
    {
        [Header("Environment")]
        public GameObject environmentPrefab;
        public GameObject outpostSlotPrefab;
        public GameObject commandPointPrefab;

        [Header("Actors and Outposts")]
        public ActorVisualEntry[] actorVisuals = Array.Empty<ActorVisualEntry>();
        public OutpostVisualEntry[] outpostVisuals = Array.Empty<OutpostVisualEntry>();
        public OutpostSpecializationVisualEntry[] outpostSpecializationVisuals = Array.Empty<OutpostSpecializationVisualEntry>();

        [Header("HUD")]
        public GameObject hudPrefab;
        public FrontendPresentationTheme frontendTheme;
        public Sprite panelSprite;
        public Sprite uiFrameSprite;
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

        [Header("Battle Radial Icons")]
        public Sprite radialUpgradeIcon;
        public Sprite radialSellIcon;
        public Sprite radialRapidFireIcon;
        public Sprite radialSharpshooterIcon;
        public Sprite radialShieldGuardIcon;
        public Sprite radialAssaultGuardIcon;
        public Sprite radialBlastIcon;
        public Sprite radialFrostIcon;

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
        public Material earthMaterial;
        public Material grassAccentMaterial;
        public Material metalMaterial;

        [Header("Licensed Atlas Materials")]
        public LicensedAtlasMaterialEntry[] licensedAtlasMaterials = Array.Empty<LicensedAtlasMaterialEntry>();
        public LicensedAtlasMaterialEntry towerPaletteMaterial;

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
        public TextAsset thirdPartyArtSources;
        public AssetProvenanceEntry[] provenance = Array.Empty<AssetProvenanceEntry>();

        public GameObject ActorPrefab(PresentationActorKind kind)
        {
            return actorVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind)?.prefab;
        }

        public float ActorScale(PresentationActorKind kind)
        {
            return actorVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind)?.scale ?? 1f;
        }

        public RuntimeAnimatorController ActorAnimator(PresentationActorKind kind)
        {
            ActorVisualEntry entry = actorVisuals.FirstOrDefault(item => item != null && item.kind == kind);
            return entry != null && entry.animatorOverride != null ? entry.animatorOverride : actorAnimator;
        }

        public bool UsesSharedActorAnimator(PresentationActorKind kind)
        {
            ActorVisualEntry entry = actorVisuals.FirstOrDefault(item => item != null && item.kind == kind);
            return entry == null || entry.animatorOverride == null || entry.animatorOverride == actorAnimator;
        }

        public GameObject OutpostPrefab(OutpostKind kind, int level)
        {
            int clamped = Mathf.Clamp(level, 1, 2);
            return outpostVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind && entry.level == clamped)?.prefab;
        }

        public float OutpostScale(OutpostKind kind, int level)
        {
            int clamped = Mathf.Clamp(level, 1, 2);
            return outpostVisuals.FirstOrDefault(entry => entry != null && entry.kind == kind && entry.level == clamped)?.scale ?? 1f;
        }

        public GameObject OutpostPrefab(OutpostKind kind, int level, OutpostSpecialization specialization) =>
            Specialization(specialization)?.prefab ?? OutpostPrefab(kind, level);

        public float OutpostScale(OutpostKind kind, int level, OutpostSpecialization specialization) =>
            Specialization(specialization)?.scale ?? OutpostScale(kind, level);

        private OutpostSpecializationVisualEntry Specialization(OutpostSpecialization specialization) =>
            specialization == OutpostSpecialization.None ? null : outpostSpecializationVisuals.FirstOrDefault(entry =>
                entry != null && entry.prefab != null && entry.specialization == specialization);

        public Material ResolveLicensedAtlasMaterial(Material source)
        {
            if (source == null) return null;
            string materialName = source.name?.ToLowerInvariant() ?? string.Empty;
            string textureName = source.mainTexture != null ? source.mainTexture.name.ToLowerInvariant() : string.Empty;
            return licensedAtlasMaterials.Append(towerPaletteMaterial).FirstOrDefault(entry => entry != null && entry.material != null &&
                !string.IsNullOrWhiteSpace(entry.sourceName) &&
                (materialName.Contains(entry.sourceName.ToLowerInvariant()) ||
                 textureName.Contains(entry.sourceName.ToLowerInvariant())))?.material;
        }

        public AudioCueEntry Audio(PresentationCue cue)
        {
            foreach (AudioCueEntry entry in audioCues)
                if (entry != null && entry.cue == cue) return entry;
            return null;
        }

        public MusicEntry Music(MusicState state)
        {
            return music.FirstOrDefault(entry => entry != null && entry.state == state);
        }

        public Material ResolveMaterial(string sourceName)
        {
            string value = sourceName?.ToLowerInvariant() ?? string.Empty;
            if (value.Contains("earth") || value.Contains("road")) return earthMaterial;
            if (value.Contains("grass")) return grassAccentMaterial;
            if (value.Contains("navy")) return navyMaterial;
            if (value.Contains("defender") || value.Contains("blue")) return defenderMaterial;
            if (value.Contains("metal")) return metalMaterial;
            if (value.Contains("stone")) return stoneMaterial;
            if (value.Contains("forest") || value.Contains("green")) return forestMaterial;
            if (value.Contains("enemy") || value.Contains("crimson") || value.Contains("red")) return enemyMaterial;
            if (value.Contains("magic") || value.Contains("cyan")) return magicMaterial;
            if (value.Contains("fire") || value.Contains("orange") || value.Contains("gold")) return fireMaterial;
            return darkMaterial;
        }

        public IReadOnlyList<string> VisualDevelopmentValidationWarnings()
        {
            var warnings = new List<string>();
            ValidateActorVisualDevelopmentGates(warnings);
            ValidateOutpostVisualDevelopmentGates(warnings);
            ValidateActorAnimationDevelopmentGates(warnings);
            return warnings;
        }

        private void ValidateActorVisualDevelopmentGates(List<string> warnings)
        {
            var expectedKinds = Enum.GetValues(typeof(PresentationActorKind)).Cast<PresentationActorKind>().ToArray();
            var entries = actorVisuals.Where(entry => entry != null).ToArray();
            if (entries.Length != expectedKinds.Length)
                warnings.Add($"Visual gate VIS-208: expected {expectedKinds.Length} actor visual entries, found {entries.Length}.");

            foreach (PresentationActorKind kind in expectedKinds)
            {
                ActorVisualEntry[] matches = entries.Where(entry => entry.kind == kind).ToArray();
                if (matches.Length != 1)
                {
                    warnings.Add($"Visual gate VIS-208: actor role {kind} has {matches.Length} catalog entries.");
                    continue;
                }

                ActorVisualEntry entry = matches[0];
                if (entry.prefab == null)
                {
                    warnings.Add($"Visual gate VIS-208: actor role {kind} has no prefab.");
                    continue;
                }

                if (entry.scale < 0.45f || entry.scale > 1.25f)
                    warnings.Add($"Visual gate VIS-208: actor role {kind} scale {entry.scale:0.00} is outside the proxy review band.");

                Renderer[] renderers = entry.prefab.GetComponentsInChildren<Renderer>(true);
                int materialCount = renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Select(material => material.name)
                    .Distinct()
                    .Count();
                int materialTarget = kind == PresentationActorKind.Boss ? 3 : 2;
                if (materialCount > materialTarget)
                    warnings.Add($"Visual gate VIS-206: actor role {kind} uses {materialCount} source materials; final target is <= {materialTarget}.");

                string[] requiredSockets = RequiredRuntimeSockets(kind);
                var socketNames = new HashSet<string>(entry.prefab.GetComponentsInChildren<Transform>(true)
                    .Select(transform => transform.name));
                foreach (string socket in requiredSockets)
                    if (!socketNames.Contains(socket))
                        warnings.Add($"Visual gate VIS-203: actor role {kind} is missing runtime socket {socket}.");
            }

            foreach (var group in entries.Where(entry => entry.prefab != null).GroupBy(entry => entry.prefab)
                         .Where(group => group.Select(entry => entry.kind).Distinct().Count() > 1))
            {
                string roles = string.Join(", ", group.Select(entry => entry.kind).Distinct());
                warnings.Add($"Visual gate VIS-201: actor prefab is shared by multiple roles: {roles}.");
            }
        }

        private void ValidateOutpostVisualDevelopmentGates(List<string> warnings)
        {
            var expectedKinds = Enum.GetValues(typeof(OutpostKind)).Cast<OutpostKind>().ToArray();
            var entries = outpostVisuals.Where(entry => entry != null).ToArray();
            int expectedCount = expectedKinds.Length * 2;
            if (entries.Length != expectedCount)
                warnings.Add($"Visual gate VIS-304: expected {expectedCount} outpost visual entries, found {entries.Length}.");

            foreach (OutpostKind kind in expectedKinds)
            {
                for (int level = 1; level <= 2; level++)
                {
                    OutpostVisualEntry[] matches = entries.Where(entry => entry.kind == kind && entry.level == level).ToArray();
                    if (matches.Length != 1)
                    {
                        warnings.Add($"Visual gate VIS-304: outpost {kind} level {level} has {matches.Length} catalog entries.");
                        continue;
                    }

                    OutpostVisualEntry entry = matches[0];
                    if (entry.prefab == null)
                    {
                        warnings.Add($"Visual gate VIS-304: outpost {kind} level {level} has no prefab.");
                        continue;
                    }

                    if (entry.scale < 0.45f || entry.scale > 1.25f)
                        warnings.Add($"Visual gate VIS-301: outpost {kind} level {level} scale {entry.scale:0.00} is outside the proxy review band.");

                    Renderer[] renderers = entry.prefab.GetComponentsInChildren<Renderer>(true);
                    int materialCount = renderers.SelectMany(renderer => renderer.sharedMaterials)
                        .Where(material => material != null)
                        .Select(material => material.name)
                        .Distinct()
                        .Count();
                    if (materialCount > 3)
                        warnings.Add($"Visual gate VIS-302: outpost {kind} level {level} uses {materialCount} source materials; final target is <= 3.");

                    var socketNames = new HashSet<string>(entry.prefab.GetComponentsInChildren<Transform>(true)
                        .Select(transform => transform.name));
                    foreach (string runtimeSocket in StylizedPresentationFactory.RuntimeSocketNamesForOutpost(kind))
                        socketNames.Add(runtimeSocket);
                    foreach (string socket in RequiredOutpostSockets(kind))
                        if (!socketNames.Contains(socket))
                            warnings.Add($"Visual gate VIS-303: outpost {kind} level {level} is missing runtime socket {socket}.");
                }
            }

            foreach (var group in entries.Where(entry => entry.prefab != null).GroupBy(entry => entry.prefab)
                         .Where(group => group.Select(entry => $"{entry.kind} L{entry.level}").Distinct().Count() > 1))
            {
                string roles = string.Join(", ", group.Select(entry => $"{entry.kind} L{entry.level}").Distinct());
                warnings.Add($"Visual gate VIS-304: outpost prefab is shared by multiple entries: {roles}.");
            }
        }

        private void ValidateActorAnimationDevelopmentGates(List<string> warnings)
        {
            foreach (string clipName in PresentationActorAnimationContract.ClipNames)
                if (actorClips.All(clip => clip == null || clip.name != clipName))
                    warnings.Add($"Visual gate VIS-205: shared actor animation clip is missing: {clipName}.");

            if (actorAnimator == null)
                warnings.Add("Visual gate VIS-205: shared actor AnimatorController is missing.");
        }

        private static string[] RequiredRuntimeSockets(PresentationActorKind kind)
        {
            string[] common = kind == PresentationActorKind.Siege
                ? new[] { "Socket_Root", "Socket_Ground", "Socket_Chest" }
                : new[] { "Socket_Root", "Socket_Ground", "Socket_Chest", "Socket_Head" };
            string[] role = kind switch
            {
                PresentationActorKind.Knight => new[] { "Socket_Hand_R", "Socket_Hand_L", "Socket_Weapon_Tip" },
                PresentationActorKind.Guard => new[] { "Socket_Hand_R", "Socket_Hand_L", "Socket_Weapon_Tip" },
                PresentationActorKind.Raider => new[] { "Socket_Hand_R", "Socket_Weapon_Tip" },
                PresentationActorKind.Vanguard => new[] { "Socket_Hand_R", "Socket_Hand_L", "Socket_Weapon_Tip" },
                PresentationActorKind.Regenerator => new[] { "Socket_Back", "Socket_Cast", "Socket_Core" },
                PresentationActorKind.Support => new[] { "Socket_Hand_R", "Socket_Cast", "Socket_Weapon_Tip" },
                PresentationActorKind.Siege => new[] { "Socket_Muzzle", "Socket_Core" },
                PresentationActorKind.Boss => new[]
                {
                    "Socket_Hand_R", "Socket_Hand_L", "Socket_Weapon_Tip", "Socket_Cast", "Socket_Core", "Socket_Back"
                },
                _ => Array.Empty<string>()
            };
            return common.Concat(role).Distinct().ToArray();
        }

        private static string[] RequiredOutpostSockets(OutpostKind kind)
        {
            string[] common =
            {
                "Socket_Root", "Socket_Ground", "Socket_Target", "Socket_Projectile",
                "Socket_VFX", "Socket_Banner", "Socket_Upgrade"
            };
            string[] role = kind switch
            {
                OutpostKind.Archer => new[] { "Socket_Muzzle" },
                OutpostKind.Barracks => new[] { "Socket_Spawn" },
                OutpostKind.Mage => new[] { "Socket_Crystal", "Socket_Cast" },
                _ => Array.Empty<string>()
            };
            return common.Concat(role).Distinct().ToArray();
        }

        public IReadOnlyList<string> ValidationErrors()
        {
            var errors = new List<string>();
            if (environmentPrefab == null) errors.Add("Environment prefab is missing.");
            if (outpostSlotPrefab == null) errors.Add("Outpost slot prefab is missing.");
            if (commandPointPrefab == null) errors.Add("Command point prefab is missing.");
            if (hudPrefab == null) errors.Add("HUD prefab is missing.");
            if (panelSprite == null) errors.Add("HUD panel sprite is missing.");
            if (uiFrameSprite == null) errors.Add("Licensed HUD frame sprite is missing.");
            if (bodyFont == null || titleFont == null) errors.Add("TMP font assets are incomplete.");
            if (frontendTheme == null) errors.Add("Frontend presentation theme is missing.");
            else if (frontendTheme.brandLogo == null || frontendTheme.brandMark == null)
                errors.Add("Frontend V2 brand sprites are incomplete.");
            if (uiCopy == null || uiCopy.ValidationErrors().Count > 0) errors.Add("Turkish UI copy is incomplete.");
            if (new[] { goldIcon, keepIcon, waveIcon, archerIcon, barracksIcon, mageIcon, abilityIcon }
                .Any(item => item == null)) errors.Add("HUD icon sprites are incomplete.");
            if (actorAnimator == null) errors.Add("Shared actor AnimatorController is missing.");
            foreach (string clipName in PresentationActorAnimationContract.ClipNames)
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
            if (thirdPartyArtSources == null) errors.Add("Third-party art source manifest is missing.");
            foreach (string required in new[] { "Noto Sans", "Noto Serif", "HUD panel", "HUD icons", "Blender source", "CC0 sound effects", "CC0 music", "KayKit Adventurers", "KayKit Medieval Hexagon", "Kenney Fantasy UI Borders" })
            {
                AssetProvenanceEntry entry = provenance.FirstOrDefault(item => item != null && item.assetName == required);
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetPath) || string.IsNullOrWhiteSpace(entry.acquiredOn))
                    errors.Add($"Asset provenance is missing: {required}.");
                else if ((entry.license is AssetLicenseKind.OpenFontLicense or AssetLicenseKind.CreativeCommonsZero) &&
                         string.IsNullOrWhiteSpace(entry.sourceUrl))
                    errors.Add($"Licensed asset source URL is missing: {required}.");
            }

            if (new[] { navyMaterial, defenderMaterial, stoneMaterial, forestMaterial, enemyMaterial,
                    magicMaterial, fireMaterial, darkMaterial, earthMaterial, grassAccentMaterial, metalMaterial }
                .Any(item => item == null))
                errors.Add("Shared palette materials are incomplete.");
            foreach (string atlas in new[] { "knight_texture", "barbarian_texture", "mage_texture", "rogue_texture", "hexagons_medieval" })
                if (licensedAtlasMaterials.All(entry => entry == null || entry.material == null || entry.sourceName != atlas))
                    errors.Add($"Licensed atlas material is missing: {atlas}.");
            return errors;
        }
    }
}
