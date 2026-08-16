#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace DefenderChronicles.Editor
{
    public static class DefenderPresentationAssetBuilder
    {
        private const string GeneratedRoot = "Assets/Art/GeneratedPresentation";
        private const string ResourceRoot = "Assets/Resources/Presentation";
        private const string ModelRoot = "Assets/Art/Models/Generated";
        private const string AdventurersRoot = "Assets/ThirdParty/KayKit/Adventurers/Models";
        private const string MedievalRoot = "Assets/ThirdParty/KayKit/MedievalHexagon/Models";
        private const string UiFramePath = "Assets/ThirdParty/Kenney/FantasyUiBorders/HudFrameBorder.png";
        private static Sprite activeUiFrameSprite;

        [MenuItem("Defender Chronicles/Build Artistic Presentation Assets")]
        public static void BuildPresentationAssets()
        {
            Directory.CreateDirectory(GeneratedRoot);
            Directory.CreateDirectory(ResourceRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureTmpResources();
            ConfigureForwardRenderer();
            ConfigureImports();

            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/DefenderHudPanel.png");
            Sprite uiFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiFramePath);
            TMP_FontAsset bodyFont = CreateFontAsset("Assets/Art/Fonts/NotoSans-Regular.ttf", "NotoSans-DC");
            TMP_FontAsset titleFont = CreateFontAsset("Assets/Art/Fonts/NotoSerif-SemiBold.ttf", "NotoSerif-DC");
            (RuntimeAnimatorController actorAnimator, AnimationClip[] actorClips) = CreateActorAnimator();
            UiCopyCatalog copy = CreateCopyCatalog();
            var materials = CreatePaletteMaterials();
            LicensedAtlasMaterialEntry[] licensedMaterials = CreateLicensedAtlasMaterials();
            Dictionary<string, AudioMixerGroup> mixerGroups = CreateMixer();
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>($"{GeneratedRoot}/DefenderPresentation.mixer");
            AudioMixerSnapshot normalSnapshot = mixer.FindSnapshot("Snapshot");
            AudioMixerSnapshot pauseSnapshot = EnsureSnapshot(mixer, "Pause");
            GameObject hudPrefab = CreateHudPrefab(panelSprite, uiFrameSprite, bodyFont, titleFont);

            string catalogPath = $"{ResourceRoot}/DefenderPresentationCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<DefenderPresentationCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DefenderPresentationCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.environmentPrefab = CreateLicensedEnvironmentPrefab();
            catalog.outpostSlotPrefab = Model("OutpostSlot");
            catalog.commandPointPrefab = Model("CommandPoint");
            catalog.actorVisuals = new[]
            {
                Actor(PresentationActorKind.Knight, LicensedCharacter("KnightLicensed"), 0.74f),
                Actor(PresentationActorKind.Guard, LicensedCharacter("KnightLicensed"), 0.66f),
                Actor(PresentationActorKind.Raider, LicensedCharacter("BarbarianLicensed"), 0.64f),
                Actor(PresentationActorKind.Vanguard, LicensedCharacter("RogueLicensed"), 0.68f),
                Actor(PresentationActorKind.Regenerator, LicensedCharacter("MageLicensed"), 0.60f),
                Actor(PresentationActorKind.Support, LicensedCharacter("MageLicensed"), 0.62f),
                Actor(PresentationActorKind.Siege, "Siege", 0.92f),
                Actor(PresentationActorKind.Boss, LicensedCharacter("BarbarianLicensed"), 1.02f)
            };
            catalog.outpostVisuals = new[]
            {
                Outpost(OutpostKind.Archer, 1, LicensedBuilding("building_tower_A_blue"), 0.72f),
                Outpost(OutpostKind.Archer, 2, LicensedBuilding("building_archeryrange_blue"), 0.72f),
                Outpost(OutpostKind.Barracks, 1, LicensedBuilding("building_tower_base_blue"), 0.76f),
                Outpost(OutpostKind.Barracks, 2, LicensedBuilding("building_barracks_blue"), 0.72f),
                Outpost(OutpostKind.Mage, 1, LicensedBuilding("building_tower_B_blue"), 0.66f),
                Outpost(OutpostKind.Mage, 2, LicensedBuilding("building_church_blue"), 0.76f)
            };
            catalog.hudPrefab = hudPrefab;
            catalog.panelSprite = panelSprite;
            catalog.uiFrameSprite = uiFrameSprite;
            catalog.bodyFont = bodyFont;
            catalog.titleFont = titleFont;
            catalog.uiCopy = copy;
            catalog.goldIcon = Icon("Gold");
            catalog.keepIcon = Icon("Keep");
            catalog.waveIcon = Icon("Wave");
            catalog.archerIcon = Icon("Archer");
            catalog.barracksIcon = Icon("Barracks");
            catalog.mageIcon = Icon("Mage");
            catalog.abilityIcon = Icon("Ability");
            catalog.actorAnimator = actorAnimator;
            catalog.actorClips = actorClips;
            catalog.navyMaterial = materials["Navy"];
            catalog.defenderMaterial = materials["Defender"];
            catalog.stoneMaterial = materials["Stone"];
            catalog.forestMaterial = materials["Forest"];
            catalog.enemyMaterial = materials["Enemy"];
            catalog.magicMaterial = materials["Magic"];
            catalog.fireMaterial = materials["Fire"];
            catalog.darkMaterial = materials["Dark"];
            catalog.earthMaterial = materials["Earth"];
            catalog.grassAccentMaterial = materials["Grass"];
            catalog.metalMaterial = materials["Metal"];
            catalog.licensedAtlasMaterials = licensedMaterials;
            catalog.uiMixer = mixerGroups["UI"];
            catalog.sfxMixer = mixerGroups["SFX"];
            catalog.musicMixer = mixerGroups["Music"];
            catalog.normalSnapshot = normalSnapshot;
            catalog.pauseSnapshot = pauseSnapshot;
            catalog.audioCues = Enum.GetValues(typeof(PresentationCue)).Cast<PresentationCue>()
                .Select(cue => new AudioCueEntry
                {
                    cue = cue,
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Presentation/SFX/{cue}.wav"),
                    volume = cue is PresentationCue.BossAttack or PresentationCue.BossPhase ? 0.86f : 0.68f,
                    pitchVariation = cue is PresentationCue.Victory or PresentationCue.Defeat ? 0f : 0.035f,
                    concurrency = cue is PresentationCue.EnemyHit ? 4 : 2
                }).ToArray();
            catalog.fontLicense = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ThirdParty/Noto/OFL.txt");
            catalog.audioLicense = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/ThirdParty/Audio/CC0Sources/CC0-1.0-Legal-Code.txt");
            catalog.thirdPartyArtSources = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ThirdParty/ART_ASSET_SOURCES.md");
            catalog.provenance = new[]
            {
                Provenance("Noto Sans", "Assets/Art/Fonts/NotoSans-Regular.ttf", AssetLicenseKind.OpenFontLicense,
                    "https://github.com/notofonts/noto-fonts", "2026-08-16"),
                Provenance("Noto Serif", "Assets/Art/Fonts/NotoSerif-SemiBold.ttf", AssetLicenseKind.OpenFontLicense,
                    "https://github.com/notofonts/noto-fonts", "2026-08-16"),
                Provenance("HUD panel", "Assets/Art/UI/DefenderHudPanel.png", AssetLicenseKind.OpenAIGenerated,
                    "OpenAI image generation / this project session", "2026-08-16"),
                Provenance("HUD icons", "Assets/Art/UI/DefenderIconAtlas.png", AssetLicenseKind.OpenAIGenerated,
                    "OpenAI image generation / this project session", "2026-08-16"),
                Provenance("Blender source", "ArtSource/Blender", AssetLicenseKind.Original,
                    "Defender Chronicles procedural art pipeline", "2026-08-16"),
                Provenance("CC0 sound effects", "Assets/Audio/Presentation/SFX", AssetLicenseKind.CreativeCommonsZero,
                    "https://kenney.nl/assets/interface-sounds | https://www.kenney.nl/assets/impact-sounds | https://www.kenney.nl/assets/rpg-audio", "2026-08-16"),
                Provenance("CC0 music", "Assets/Audio/Presentation/Music", AssetLicenseKind.CreativeCommonsZero,
                    "https://opengameart.org/ (six source pages recorded in Assets/ThirdParty/Audio/CC0Sources/SOURCES.md)", "2026-08-16"),
                Provenance("KayKit Adventurers", AdventurersRoot, AssetLicenseKind.CreativeCommonsZero,
                    "https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0", "2026-08-16"),
                Provenance("KayKit Medieval Hexagon", MedievalRoot, AssetLicenseKind.CreativeCommonsZero,
                    "https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0", "2026-08-16"),
                Provenance("Kenney Fantasy UI Borders", UiFramePath, AssetLicenseKind.CreativeCommonsZero,
                    "https://kenney.nl/assets/fantasy-ui-borders", "2026-08-16")
            };
            catalog.music = Enum.GetValues(typeof(MusicState)).Cast<MusicState>()
                .Select(state => new MusicEntry
                {
                    state = state,
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Presentation/Music/{state}.wav"),
                    volume = state is MusicState.Boss ? 0.58f : 0.46f,
                    loop = state is not (MusicState.Victory or MusicState.Defeat)
                }).ToArray();
            EditorUtility.SetDirty(catalog);
            AssignCatalogToScene(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            IReadOnlyList<string> errors = catalog.ValidationErrors();
            if (errors.Count > 0)
                throw new InvalidOperationException("Presentation asset build is incomplete:\n- " + string.Join("\n- ", errors));
            Debug.Log($"[DC_ART_ASSETS_READY] Artistic presentation catalog built with {catalog.actorVisuals.Length} actors, " +
                      $"{catalog.outpostVisuals.Length} outposts and {catalog.audioCues.Length} audio cues.");
        }

        public static void LogPresentationBounds()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string name in new[] { "Environment", "OutpostSlot", "CommandPoint", "Knight", "Boss" })
            {
                GameObject prefab = Model(name);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) continue;
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Debug.Log($"[DC_ART_BOUNDS] {name}: center={bounds.center}, size={bounds.size}, rootScale={instance.transform.localScale}");
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        public static void LogLicensedAssetBounds()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var assets = new Dictionary<string, GameObject>
            {
                ["KnightLicensed"] = LicensedCharacter("KnightLicensed"),
                ["BarbarianLicensed"] = LicensedCharacter("BarbarianLicensed"),
                ["MageLicensed"] = LicensedCharacter("MageLicensed"),
                ["RogueLicensed"] = LicensedCharacter("RogueLicensed"),
                ["CastleLicensed"] = LicensedBuilding("building_castle_blue"),
                ["ArcherL1Licensed"] = LicensedBuilding("building_tower_A_blue")
            };
            foreach ((string name, GameObject prefab) in assets)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) continue;
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                string materialInfo = string.Join(", ", renderers.SelectMany(item => item.sharedMaterials)
                    .Where(item => item != null).Select(item => $"{item.name}/{item.mainTexture?.name}").Distinct());
                Debug.Log($"[DC_LICENSED_BOUNDS] {name}: center={bounds.center}, size={bounds.size}, materials={materialInfo}");
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        public static void LogAudioMixerApi()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>($"{GeneratedRoot}/DefenderPresentation.mixer");
            if (mixer == null) return;
            foreach (MethodInfo method in mixer.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(method => method.Name.Contains("Snapshot", StringComparison.OrdinalIgnoreCase)))
                Debug.Log("[DC_MIXER_API] " + method.Name + "(" + string.Join(", ", method.GetParameters()
                    .Select(parameter => parameter.ParameterType.Name + " " + parameter.Name)) + ")");
        }

        private static AssetProvenanceEntry Provenance(string name, string path, AssetLicenseKind license,
            string source, string acquiredOn)
        {
            return new AssetProvenanceEntry
            {
                assetName = name,
                assetPath = path,
                license = license,
                sourceUrl = source,
                acquiredOn = acquiredOn
            };
        }

        private static AudioMixerSnapshot EnsureSnapshot(AudioMixer mixer, string snapshotName)
        {
            AudioMixerSnapshot existing = mixer.FindSnapshot(snapshotName);
            if (existing != null) return existing;
            AudioMixerSnapshot pendingCopy = mixer.FindSnapshot("Snapshot - Copy");
            if (pendingCopy != null)
            {
                pendingCopy.name = snapshotName;
                EditorUtility.SetDirty(mixer);
                AssetDatabase.SaveAssets();
                return pendingCopy;
            }
            MethodInfo clone = mixer.GetType().GetMethod("CloneNewSnapshotFromTarget",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (clone == null) throw new InvalidOperationException("Audio mixer snapshot creation API is unavailable.");
            clone.Invoke(mixer, new object[] { false });
            var snapshot = mixer.FindSnapshot("Snapshot - Copy");
            if (snapshot == null) throw new InvalidOperationException("Pause audio snapshot could not be created.");
            snapshot.name = snapshotName;
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            return snapshot;
        }

        private static void ConfigureImports()
        {
            var panel = AssetImporter.GetAtPath("Assets/Art/UI/DefenderHudPanel.png") as TextureImporter;
            if (panel != null)
            {
                panel.textureType = TextureImporterType.Sprite;
                panel.spriteImportMode = SpriteImportMode.Single;
                panel.spriteBorder = new Vector4(128, 128, 128, 128);
                panel.maxTextureSize = 1024;
                panel.textureCompression = TextureImporterCompression.Compressed;
                panel.mipmapEnabled = false;
                panel.SaveAndReimport();
            }

            if (AssetImporter.GetAtPath(UiFramePath) is TextureImporter frame)
            {
                frame.textureType = TextureImporterType.Sprite;
                frame.spriteImportMode = SpriteImportMode.Single;
                frame.spriteBorder = new Vector4(12, 12, 12, 12);
                frame.maxTextureSize = 64;
                frame.textureCompression = TextureImporterCompression.Uncompressed;
                frame.filterMode = FilterMode.Bilinear;
                frame.mipmapEnabled = false;
                frame.SaveAndReimport();
            }

            foreach (string path in Directory.GetFiles("Assets/ThirdParty/KayKit", "*.png", SearchOption.AllDirectories))
            {
                string assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter atlas) continue;
                atlas.textureType = TextureImporterType.Default;
                atlas.sRGBTexture = true;
                atlas.mipmapEnabled = true;
                atlas.maxTextureSize = assetPath.Contains("MedievalHexagon") ? 1024 : 512;
                atlas.textureCompression = TextureImporterCompression.CompressedHQ;
                atlas.SaveAndReimport();
            }

            foreach (string path in Directory.GetFiles("Assets/Art/UI", "Icon*.png", SearchOption.TopDirectoryOnly))
            {
                string assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter icon) continue;
                icon.textureType = TextureImporterType.Sprite;
                icon.spriteImportMode = SpriteImportMode.Single;
                icon.mipmapEnabled = false;
                icon.maxTextureSize = 256;
                icon.textureCompression = TextureImporterCompression.Compressed;
                icon.SaveAndReimport();
            }

            foreach (string path in Directory.GetFiles("Assets/Audio/Presentation", "*.wav", SearchOption.AllDirectories))
            {
                string assetPath = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(assetPath) is not AudioImporter importer) continue;
                importer.forceToMono = true;
                importer.loadInBackground = assetPath.Contains("/Music/");
                importer.defaultSampleSettings = new AudioImporterSampleSettings
                {
                    loadType = assetPath.Contains("/Music/") ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.Vorbis,
                    quality = assetPath.Contains("/Music/") ? 0.55f : 0.72f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
                };
                importer.SaveAndReimport();
            }
        }

        private static Sprite Icon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Icon{name}.png");
        }

        private static (RuntimeAnimatorController controller, AnimationClip[] clips) CreateActorAnimator()
        {
            string[] names = { "Idle", "Move", "Attack", "Hit", "Defeat", "Ability", "Victory", "Phase", "BossAttack" };
            var clips = new List<AnimationClip>();
            foreach (string clipName in names)
            {
                string path = $"{GeneratedRoot}/DC_{clipName}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    clip = CreateMotionClip(clipName);
                    AssetDatabase.CreateAsset(clip, path);
                }
                clip.name = clipName;
                EditorUtility.SetDirty(clip);
                clips.Add(clip);
            }

            string controllerPath = $"{GeneratedRoot}/DC_Actors.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
                foreach (string trigger in names.Skip(2))
                    controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                AnimatorState idle = machine.AddState("Idle");
                idle.motion = clips[0];
                machine.defaultState = idle;
                AnimatorState move = machine.AddState("Move");
                move.motion = clips[1];
                AnimatorStateTransition toMove = idle.AddTransition(move);
                toMove.hasExitTime = false;
                toMove.duration = 0.08f;
                toMove.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
                AnimatorStateTransition toIdle = move.AddTransition(idle);
                toIdle.hasExitTime = false;
                toIdle.duration = 0.08f;
                toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

                for (int i = 2; i < names.Length; i++)
                {
                    AnimatorState state = machine.AddState(names[i]);
                    state.motion = clips[i];
                    AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
                    enter.hasExitTime = false;
                    enter.duration = 0.025f;
                    enter.AddCondition(AnimatorConditionMode.If, 0f, names[i]);
                    AnimatorStateTransition exit = state.AddTransition(idle);
                    exit.hasExitTime = true;
                    exit.exitTime = 1f;
                    exit.duration = 0.05f;
                }
            }
            return (controller, clips.ToArray());
        }

        private static AnimationClip CreateMotionClip(string clipName)
        {
            float duration = clipName is "Idle" or "Move" ? 0.8f : clipName == "Defeat" ? 0.58f : 0.34f;
            float lift = clipName switch
            {
                "Idle" => 0.025f, "Move" => 0.09f, "Victory" => 0.15f, "Phase" => 0.12f, _ => 0.04f
            };
            float thrust = clipName switch
            {
                "Attack" => 0.2f, "Ability" => 0.13f, "BossAttack" => 0.27f, "Hit" => -0.08f, _ => 0f
            };
            float scale = clipName switch
            {
                "Hit" => 0.9f, "Defeat" => 0.62f, "Ability" => 1.16f, "Victory" => 1.12f,
                "Phase" => 1.2f, "BossAttack" => 1.14f, _ => 1f
            };
            var clip = new AnimationClip { name = clipName, frameRate = 30f };
            clip.wrapMode = clipName is "Idle" or "Move" ? WrapMode.Loop : WrapMode.Once;
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(duration * 0.5f, lift), new Keyframe(duration, 0f)));
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(duration * 0.45f, thrust), new Keyframe(duration, 0f)));
            foreach (string axis in new[] { "x", "y", "z" })
                clip.SetCurve("", typeof(Transform), $"m_LocalScale.{axis}", new AnimationCurve(
                    new Keyframe(0f, 1f), new Keyframe(duration * 0.55f, scale), new Keyframe(duration, 1f)));
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
            {
                loopTime = clipName is "Idle" or "Move",
                stopTime = duration
            });
            return clip;
        }

        private static void EnsureTmpResources()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null)
                return;
            Directory.CreateDirectory("Assets/TextMesh Pro/Resources");
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            settings.name = "TMP Settings";
            AssetDatabase.CreateAsset(settings, "Assets/TextMesh Pro/Resources/TMP Settings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static TMP_FontAsset CreateFontAsset(string fontPath, string assetName)
        {
            string assetPath = $"{GeneratedRoot}/{assetName}.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;
            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) throw new InvalidOperationException($"Font import failed: {fontPath}");
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (Texture2D atlas in asset.atlasTextures.Where(texture => texture != null))
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static UiCopyCatalog CreateCopyCatalog()
        {
            string path = $"{ResourceRoot}/UiCopyTr.asset";
            var copy = AssetDatabase.LoadAssetAtPath<UiCopyCatalog>(path);
            if (copy == null)
            {
                copy = ScriptableObject.CreateInstance<UiCopyCatalog>();
                AssetDatabase.CreateAsset(copy, path);
            }
            var values = new Dictionary<UiTextKey, string>
            {
                [UiTextKey.Gold] = "ALTIN", [UiTextKey.Keep] = "KALE", [UiTextKey.Wave] = "DALGA",
                [UiTextKey.Preparing] = "HAZIRLIK", [UiTextKey.Combat] = "SAVAŞ", [UiTextKey.Intermission] = "MOLA",
                [UiTextKey.Paused] = "DURAKLATILDI", [UiTextKey.Loading] = "YÜKLENİYOR", [UiTextKey.Pause] = "DURAKLAT",
                [UiTextKey.Continue] = "DEVAM", [UiTextKey.Archer] = "OKÇU", [UiTextKey.Barracks] = "KIŞLA",
                [UiTextKey.Mage] = "BÜYÜ", [UiTextKey.ShieldBash] = "KALKAN DARBESİ", [UiTextKey.Cancel] = "İPTAL",
                [UiTextKey.FirstWave] = "İLK DALGAYI BAŞLAT", [UiTextKey.EarlyWave] = "DALGAYI ERKEN ÇAĞIR",
                [UiTextKey.NextWave] = "SONRAKİ DALGA", [UiTextKey.Upgrade] = "YÜKSELT", [UiTextKey.Sell] = "SAT",
                [UiTextKey.Knight] = "ŞÖVALYE", [UiTextKey.AbilityReady] = "YETENEK HAZIR",
                [UiTextKey.KnightReturning] = "ŞÖVALYE DÖNÜYOR", [UiTextKey.Victory] = "ZAFER\nGREENWATCH KORUNDU",
                [UiTextKey.Defeat] = "YENİLGİ\nGREENWATCH DÜŞTÜ", [UiTextKey.VictoryPhase] = "ZAFER",
                [UiTextKey.DefeatPhase] = "YENİLGİ", [UiTextKey.Restart] = "YENİDEN DENE",
                [UiTextKey.SlotInstruction] = "KARAKOL PLATFORMU", [UiTextKey.BuildInstruction] = "Aşağıdan bir karakol seç."
            };
            copy.locale = "tr-TR";
            copy.entries = Enum.GetValues(typeof(UiTextKey)).Cast<UiTextKey>()
                .Select(key => new UiCopyEntry { key = key, value = values[key] }).ToArray();
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static Dictionary<string, Material> CreatePaletteMaterials()
        {
            var palette = new Dictionary<string, Color>
            {
                ["Navy"] = Hex("07182A"), ["Defender"] = Hex("1D6FD1"), ["Stone"] = Hex("8A96A3"),
                ["Forest"] = Hex("193D2C"), ["Enemy"] = Hex("A52B32"), ["Magic"] = Hex("3CBFEF"),
                ["Fire"] = Hex("FF8A24"), ["Dark"] = Hex("0E1114"), ["Earth"] = Hex("9A6742"),
                ["Grass"] = Hex("2B5B43"), ["Metal"] = Hex("596979")
            };
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var result = new Dictionary<string, Material>();
            foreach ((string name, Color color) in palette)
            {
                string path = $"{GeneratedRoot}/DC_{name}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = $"DC_{name}" };
                    AssetDatabase.CreateAsset(material, path);
                }
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", name switch
                    {
                        "Metal" => 0.46f,
                        "Stone" => 0.22f,
                        "Defender" or "Enemy" => 0.24f,
                        _ => 0.09f
                    });
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", name == "Metal" ? 0.62f : name is "Defender" or "Enemy" ? 0.08f : 0f);
                if (name is "Magic" or "Fire")
                {
                    material.EnableKeyword("_EMISSION");
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", color * (name == "Fire" ? 1.25f : 0.68f));
                }
                EditorUtility.SetDirty(material);
                result[name] = material;
            }
            return result;
        }

        private static LicensedAtlasMaterialEntry[] CreateLicensedAtlasMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var definitions = new[]
            {
                ("knight_texture", $"{AdventurersRoot}/knight_texture.png"),
                ("barbarian_texture", $"{AdventurersRoot}/barbarian_texture.png"),
                ("mage_texture", $"{AdventurersRoot}/mage_texture.png"),
                ("rogue_texture", $"{AdventurersRoot}/rogue_texture.png"),
                ("hexagons_medieval", $"{MedievalRoot}/hexagons_medieval.png")
            };
            var result = new List<LicensedAtlasMaterialEntry>();
            foreach ((string sourceName, string texturePath) in definitions)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null) throw new InvalidOperationException($"Licensed atlas texture failed to import: {texturePath}");
                string materialPath = $"{GeneratedRoot}/Licensed_{sourceName}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = $"Licensed_{sourceName}" };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.mainTexture = texture;
                material.color = Color.white;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.08f);
                bool actorAtlas = sourceName != "hexagons_medieval";
                if (actorAtlas)
                {
                    material.EnableKeyword("_EMISSION");
                    if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", texture);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", new Color(0.13f, 0.16f, 0.2f));
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                EditorUtility.SetDirty(material);
                result.Add(new LicensedAtlasMaterialEntry { sourceName = sourceName, material = material });
            }
            return result.ToArray();
        }

        private static GameObject CreateLicensedEnvironmentPrefab()
        {
            string path = $"{GeneratedRoot}/LicensedEnvironment.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
            var root = new GameObject("LicensedEnvironment");
            GameObject baseEnvironment = PrefabUtility.InstantiatePrefab(Model("Environment"), root.transform) as GameObject;
            if (baseEnvironment == null) throw new InvalidOperationException("Base environment model is missing.");
            baseEnvironment.name = "Battlefield";
            foreach (Renderer renderer in baseEnvironment.GetComponentsInChildren<Renderer>(true))
                if (renderer.name.StartsWith("Keep", StringComparison.OrdinalIgnoreCase) ||
                    renderer.name.StartsWith("Pine", StringComparison.OrdinalIgnoreCase))
                    renderer.enabled = false;

            GameObject castle = PrefabUtility.InstantiatePrefab(LicensedBuilding("building_castle_blue"), root.transform) as GameObject;
            if (castle == null) throw new InvalidOperationException("Licensed castle model is missing.");
            castle.name = "Defender Keep Licensed";
            // The runtime rotates the authored environment 180 degrees to undo FBX handedness.
            // Composite children therefore use the imported (mirrored) local coordinates too.
            castle.transform.localPosition = new Vector3(-23.45f, 0f, -9f);
            castle.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            castle.transform.localScale = Vector3.one;

            (string asset, Vector3 position, float scale, float rotation)[] nature =
            {
                ("trees_A_medium", new Vector3(0.8f, 0f, 11.15f), 1.05f, 12f),
                ("trees_B_small", new Vector3(4.0f, 0f, 11.2f), 1.16f, -18f),
                ("trees_A_small", new Vector3(7.8f, 0f, 11.2f), 1.12f, 28f),
                ("trees_A_medium", new Vector3(12.0f, 0f, 11.15f), 1.02f, -8f),
                ("trees_B_small", new Vector3(16.1f, 0f, 11.2f), 1.2f, 16f),
                ("trees_A_small", new Vector3(20.2f, 0f, 11.15f), 1.1f, -28f),
                ("trees_B_small", new Vector3(1.0f, 0f, 0.62f), 1.14f, 22f),
                ("trees_A_small", new Vector3(5.2f, 0f, 0.58f), 1.08f, -14f),
                ("trees_A_medium", new Vector3(10.2f, 0f, 0.58f), 1f, 8f),
                ("trees_B_small", new Vector3(15.7f, 0f, 0.62f), 1.18f, -26f),
                ("trees_A_small", new Vector3(21.1f, 0f, 0.6f), 1.08f, 18f),
                ("rock_single_A", new Vector3(4.3f, 0.03f, 3.0f), 1.9f, 12f),
                ("rock_single_B", new Vector3(9.7f, 0.03f, 2.0f), 1.7f, -18f),
                ("rock_single_A", new Vector3(15.8f, 0.03f, 9.7f), 2.1f, 24f),
                ("rock_single_B", new Vector3(19.2f, 0.03f, 3.0f), 1.8f, -8f)
            };
            for (int i = 0; i < nature.Length; i++)
            {
                var entry = nature[i];
                AddLicensedEnvironmentPiece(root.transform, entry.asset, entry.position, entry.scale, entry.rotation, i + 1);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void AddLicensedEnvironmentPiece(Transform parent, string assetName, Vector3 worldPosition,
            float scale, float worldRotation, int index)
        {
            GameObject source = LicensedBuilding(assetName);
            GameObject piece = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (piece == null) throw new InvalidOperationException($"Licensed environment model is missing: {assetName}.");
            piece.name = $"Licensed Nature {index:00} {assetName}";
            piece.transform.localPosition = new Vector3(-worldPosition.x, worldPosition.y, -worldPosition.z);
            piece.transform.localRotation = Quaternion.Euler(0f, worldRotation - 180f, 0f);
            piece.transform.localScale = Vector3.one * scale;
        }

        private static void ConfigureForwardRenderer()
        {
            const string rendererPath = "Assets/Settings/DefenderForwardRenderer.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "DefenderForwardRenderer";
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            var rendererSerialized = new SerializedObject(renderer);
            SerializedProperty postProcessData = rendererSerialized.FindProperty("postProcessData");
            if (postProcessData != null && postProcessData.objectReferenceValue == null)
            {
                postProcessData.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (string pipelinePath in new[]
                     {
                         "Assets/Settings/UniversalRP.asset",
                         "Assets/Settings/UniversalRPCustomAxisSort.asset"
                     })
            {
                UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
                if (pipeline == null) continue;
                var serialized = new SerializedObject(pipeline);
                SerializedProperty rendererList = serialized.FindProperty("m_RendererDataList");
                if (rendererList == null) continue;
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                SerializedProperty defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");
                if (defaultIndex != null) defaultIndex.intValue = 0;
                SerializedProperty depth = serialized.FindProperty("m_RequireDepthTexture");
                if (depth != null) depth.boolValue = true;
                SerializedProperty msaa = serialized.FindProperty("m_MSAA");
                if (msaa != null) msaa.intValue = 1;
                SerializedProperty softShadows = serialized.FindProperty("m_SoftShadowsSupported");
                if (softShadows != null) softShadows.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }

            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
        }

        private static Dictionary<string, AudioMixerGroup> CreateMixer()
        {
            string path = $"{GeneratedRoot}/DefenderPresentation.mixer";
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            if (mixer == null)
            {
                Type controllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                if (controllerType == null) throw new InvalidOperationException("Unity AudioMixerController type is unavailable.");
                MethodInfo factory = controllerType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(method => method.Name == "CreateMixerControllerAtPath" &&
                                              method.GetParameters().Length == 1 &&
                                              method.GetParameters()[0].ParameterType == typeof(string));
                if (factory == null)
                    throw new InvalidOperationException("Audio mixer factory method is unavailable: " +
                        string.Join(", ", controllerType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Select(method => method.Name).Distinct()));
                factory.Invoke(null, new object[] { path });
                AssetDatabase.SaveAssets();
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
            }
            AudioMixerGroup[] masters = mixer != null ? mixer.FindMatchingGroups("Master") : Array.Empty<AudioMixerGroup>();
            if (masters.Length == 0) throw new InvalidOperationException("Audio mixer master group could not be created.");
            object controllerObject = mixer;
            Type type = controllerObject.GetType();
            MethodInfo createGroup = type.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo addChild = type.GetMethod("AddChildToParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo masterProperty = type.GetProperty("masterGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (createGroup == null || addChild == null || masterProperty == null)
                throw new InvalidOperationException("Audio mixer group creation API is unavailable.");
            object master = masterProperty.GetValue(controllerObject);
            var result = new Dictionary<string, AudioMixerGroup>();
            foreach (string groupName in new[] { "UI", "SFX", "Music" })
            {
                AudioMixerGroup group = mixer.FindMatchingGroups(groupName).FirstOrDefault(item => item.name == groupName);
                if (group == null)
                {
                    object child = createGroup.Invoke(controllerObject, new object[] { groupName, false });
                    addChild.Invoke(controllerObject, new[] { child, master });
                    group = child as AudioMixerGroup;
                }
                result[groupName] = group;
            }
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            return result;
        }

        private static GameObject CreateHudPrefab(Sprite panel, Sprite frame, TMP_FontAsset body, TMP_FontAsset title)
        {
            string path = $"{GeneratedRoot}/DefenderHud.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            activeUiFrameSprite = frame;

            var root = new GameObject("DefenderHud", typeof(RectTransform), typeof(DefenderHudController));
            Stretch(root.GetComponent<RectTransform>());
            GameObject top = Panel(root.transform, "TopBar", panel, new Color(0.025f, 0.055f, 0.095f, 0.96f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -82), Vector2.zero);
            Text(top.transform, "Gold", "ALTIN  200", body, 28, TextAlignmentOptions.MidlineLeft,
                new Vector2(20, 8), new Vector2(310, 62), Vector2.zero, Vector2.zero);
            Text(top.transform, "Lives", "KALE  20", body, 28, TextAlignmentOptions.MidlineLeft,
                new Vector2(335, 8), new Vector2(240, 62), Vector2.zero, Vector2.zero);
            Text(top.transform, "Wave", "DALGA  0 / 7", title, 28, TextAlignmentOptions.Center,
                new Vector2(-205, 8), new Vector2(410, 62), new Vector2(.5f, 0), new Vector2(.5f, 0));
            Text(top.transform, "Phase", "HAZIRLIK", body, 22, TextAlignmentOptions.Center,
                new Vector2(205, 8), new Vector2(280, 62), new Vector2(.5f, 0), new Vector2(.5f, 0));
            Button(top.transform, "Pause", "DURAKLAT", panel, body, new Vector2(-218, 12), new Vector2(198, 54),
                new Vector2(1, 0), new Vector2(1, 0));

            Text(root.transform, "Preview", string.Empty, title, 25, TextAlignmentOptions.Center,
                new Vector2(-430, -150), new Vector2(860, 52), new Vector2(.5f, 1), new Vector2(.5f, 1), Hex("FFB14A"));
            Text(root.transform, "Message", string.Empty, body, 28, TextAlignmentOptions.Center,
                new Vector2(-550, -214), new Vector2(1100, 60), new Vector2(.5f, 1), new Vector2(.5f, 1));

            GameObject bottom = Panel(root.transform, "BottomBar", panel, new Color(0.025f, 0.055f, 0.095f, 0.97f),
                Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 132));
            Button(bottom.transform, "Archer", "[A]  OKÇU\n75", panel, body, new Vector2(18, 36), new Vector2(188, 70), Vector2.zero, Vector2.zero);
            Button(bottom.transform, "Barracks", "[K]  KIŞLA\n100", panel, body, new Vector2(218, 36), new Vector2(188, 70), Vector2.zero, Vector2.zero);
            Button(bottom.transform, "Mage", "[B]  BÜYÜ\n125", panel, body, new Vector2(418, 36), new Vector2(188, 70), Vector2.zero, Vector2.zero);
            Button(bottom.transform, "Ability", "KALKAN DARBESİ", panel, body, new Vector2(620, 36), new Vector2(246, 70), Vector2.zero, Vector2.zero);
            Button(bottom.transform, "Cancel", "İPTAL", panel, body, new Vector2(880, 36), new Vector2(150, 70), Vector2.zero, Vector2.zero);
            Text(bottom.transform, "Hero", "ŞÖVALYE", body, 20, TextAlignmentOptions.MidlineLeft,
                new Vector2(620, 4), new Vector2(390, 30), Vector2.zero, Vector2.zero, Hex("A8DFFF"));
            Button(bottom.transform, "NextWave", "İLK DALGAYI BAŞLAT", panel, title, new Vector2(-340, 36), new Vector2(320, 70),
                new Vector2(1, 0), new Vector2(1, 0), Hex("174D78"));

            GameObject selected = Panel(root.transform, "SelectedPanel", panel, new Color(0.025f, 0.055f, 0.095f, 0.97f),
                new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-338, -126), new Vector2(-18, 184));
            TMP_Text description = Text(selected.transform, "Description", string.Empty, body, 22, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            description.rectTransform.offsetMin = new Vector2(16, 92);
            description.rectTransform.offsetMax = new Vector2(-16, -18);
            Button(selected.transform, "Upgrade", "YÜKSELT", panel, body, new Vector2(14, 18), new Vector2(138, 58), Vector2.zero, Vector2.zero);
            Button(selected.transform, "Sell", "SAT", panel, body, new Vector2(168, 18), new Vector2(138, 58), Vector2.zero, Vector2.zero);

            GameObject result = Panel(root.transform, "ResultPanel", null, new Color(0.01f, 0.02f, 0.035f, 0.92f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text(result.transform, "Title", string.Empty, title, 68, TextAlignmentOptions.Center,
                new Vector2(-520, -40), new Vector2(1040, 190), new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            Button(result.transform, "Restart", "YENİDEN DENE", panel, title, new Vector2(-175, -160), new Vector2(350, 86),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Hex("174D78"));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject Panel(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return go;
        }

        private static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font, float size,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, Vector2 anchorMin, Vector2 anchorMax,
            Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color ?? Color.white;
            text.enableWordWrapping = false;
            var rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            return text;
        }

        private static GameObject Button(Transform parent, string name, string label, Sprite sprite, TMP_FontAsset font,
            Vector2 position, Vector2 dimensions, Vector2 anchorMin, Vector2 anchorMax, Color? tint = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = tint ?? new Color(0.06f, 0.18f, 0.30f, 0.98f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.20f, 0.48f, 0.78f, 1f);
            colors.pressedColor = new Color(1f, 0.48f, 0.13f, 1f);
            colors.disabledColor = new Color(0.08f, 0.10f, 0.14f, 0.72f);
            button.colors = colors;
            if (activeUiFrameSprite != null)
            {
                var frame = new GameObject("LicensedFrame", typeof(RectTransform), typeof(Image));
                frame.transform.SetParent(go.transform, false);
                var frameRect = frame.GetComponent<RectTransform>();
                Stretch(frameRect);
                frameRect.offsetMin = new Vector2(3, 3);
                frameRect.offsetMax = new Vector2(-3, -3);
                var frameImage = frame.GetComponent<Image>();
                frameImage.sprite = activeUiFrameSprite;
                frameImage.type = Image.Type.Sliced;
                frameImage.color = tint.HasValue ? Hex("713913") : Hex("123653");
                frameImage.raycastTarget = false;
            }
            TMP_Text text = Text(go.transform, "Label", label, font, 22, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            text.rectTransform.offsetMin = new Vector2(8, 5);
            text.rectTransform.offsetMax = new Vector2(-8, -5);
            return go;
        }

        private static void AssignCatalogToScene(DefenderPresentationCatalog catalog)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
            GameBootstrap bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null) throw new InvalidOperationException("Game scene has no GameBootstrap.");
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("presentationCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelRoot}/{name}.fbx");
        private static GameObject LicensedCharacter(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{AdventurersRoot}/{name}.fbx");
        private static GameObject LicensedBuilding(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{MedievalRoot}/{name}.fbx");
        private static ActorVisualEntry Actor(PresentationActorKind kind, string name, float scale) =>
            new() { kind = kind, prefab = Model(name), scale = scale };
        private static ActorVisualEntry Actor(PresentationActorKind kind, GameObject prefab, float scale) =>
            new() { kind = kind, prefab = prefab, scale = scale };
        private static OutpostVisualEntry Outpost(OutpostKind kind, int level, string name) =>
            new() { kind = kind, level = level, prefab = Model(name) };
        private static OutpostVisualEntry Outpost(OutpostKind kind, int level, GameObject prefab, float scale) =>
            new() { kind = kind, level = level, prefab = prefab, scale = scale };
        private static Color Hex(string value) => ColorUtility.TryParseHtmlString($"#{value}", out Color color) ? color : Color.magenta;
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
