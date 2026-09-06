#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.Editor
{
    public sealed class DefenderPresentationBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new BuildFailedException("DefenderPresentationCatalog is missing. Run the artistic presentation asset builder.");
            var errors = catalog.ValidationErrors();
            if (errors.Count > 0)
                throw new BuildFailedException("Artistic presentation catalog is incomplete:\n- " + string.Join("\n- ", errors));

            const string catalogPath = "Assets/Resources/Presentation/DefenderPresentationCatalog.asset";
            const string gameScenePath = "Assets/Scenes/Game.unity";
            string catalogGuid = AssetDatabase.AssetPathToGUID(catalogPath);
            if (string.IsNullOrWhiteSpace(catalogGuid) || !File.ReadAllText(gameScenePath).Contains($"guid: {catalogGuid}"))
                throw new BuildFailedException("GameBootstrap must reference the production DefenderPresentationCatalog.");
        }

        public static void ValidateBaselineEvidenceIndexCheckpoint()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/evidence/visual-development/baseline-index-2026-08-16.md",
                "docs/SIMULATOR_VISUAL_REVIEW_2026-08-16.md",
                "docs/HUD_RATIO_CAPTURE_REVIEW_2026-08-17.md",
                "docs/evidence/runtime/defender-simulator-vis612-16x9-iphone-se-rebuilt-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-vis612-20x9-iphone-17-pro-rebuilt-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-vis612-4x3-ipad-pro-13-m5-helper-rotated-landscape-readable.png",
                "docs/evidence/visual-development/catalog-baseline-2026-08-16.json",
                "docs/evidence/visual-development/asset-inventory-2026-08-16.md"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Baseline evidence file is missing: {relativePath}");
            }

            string indexPath = Path.Combine(root, "docs/evidence/visual-development/baseline-index-2026-08-16.md");
            string index = File.ReadAllText(indexPath);
            string[] requiredIndexTokens =
            {
                "HUD ratio capture review",
                "16:9 preparation runtime screenshot",
                "20:9 preparation runtime screenshot",
                "4:3 tablet preparation runtime screenshot",
                "VIS-002",
                "VIS-003",
                "VIS-006",
                "VIS-007"
            };

            foreach (string token in requiredIndexTokens)
            {
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Baseline index does not mention required token: {token}");
            }

            if (index.Contains("VIS-001 | 16:9 and 4:3 preparation screenshots", StringComparison.Ordinal))
                throw new InvalidOperationException("Baseline index still marks VIS-001 ratio screenshots as missing.");

            Debug.Log("[DC_BASELINE_EVIDENCE_INDEX_CHECKPOINT] passed " +
                      "vis001=16x9/20x9/4x3 index=updated missing=VIS002/VIS003/VIS006/VIS007");
        }

        public static void ValidateArtBibleStandardIndexCheckpoint()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS100_ART_BIBLE_STANDARD_INDEX_2026-08-17.md",
                "docs/VISUAL_ART_BIBLE_2026-08-16.md",
                "docs/VISUAL_DEVELOPMENT_RESEARCH_2026-08-16.md",
                "docs/ART_QUALITY_CONTRACT.md",
                "docs/ACTOR_SOCKET_STANDARD_2026-08-16.md",
                "docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md",
                "docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md",
                "docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md",
                "docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md",
                "docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md",
                "docs/ACTION_ANIMATION_TIMING_CONTRACT_2026-08-17.md",
                "docs/BUILD_VFX_STANDARD_2026-08-16.md",
                "docs/UPGRADE_VFX_STANDARD_2026-08-16.md",
                "docs/SELL_VFX_STANDARD_2026-08-16.md",
                "docs/UI_FULL_SCREEN_RESEARCH_2026-08-16.md",
                "docs/HUD_ACCESSIBILITY_AUDIT_2026-08-16.md",
                "docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json",
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"VIS-100 standard evidence file is missing: {relativePath}");
            }

            string artBible = File.ReadAllText(Path.Combine(root, "docs/VISUAL_ART_BIBLE_2026-08-16.md"));
            string index = File.ReadAllText(Path.Combine(root, "docs/VIS100_ART_BIBLE_STANDARD_INDEX_2026-08-17.md"));

            string[] artBibleSections =
            {
                "## 1. Core Visual Sentence",
                "## 2. Moodboard References",
                "## 3. Palette",
                "## 4. Value Hierarchy",
                "## 5. Scale And Export Rules",
                "## 6. Geometry Budgets",
                "## 7. Materials And Textures",
                "## 8. Silhouette Rules",
                "## 9. Animation Contract",
                "## 10. VFX Families",
                "## 11. HUD Component States",
                "## 12. Accessibility Checklist"
            };

            foreach (string section in artBibleSections)
            {
                if (!artBible.Contains(section, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Art bible is missing section: {section}");
            }

            int moodboardRows = artBible.Split('\n').Count(line =>
                line.StartsWith("| ", StringComparison.Ordinal) &&
                line.Contains(" | ", StringComparison.Ordinal) &&
                char.IsDigit(line.TrimStart('|', ' ')[0]));
            if (moodboardRows < 12)
                throw new InvalidOperationException($"Art bible moodboard reference count is too low: {moodboardRows}");

            for (int id = 101; id <= 111; id++)
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-100 index does not mention {token}.");
            }

            string[] requiredIndexTokens =
            {
                "actor <=2",
                "building <=3",
                "VIS-105 bütçeleri şu an authoring/proxy bütçeleridir",
                "normal/pressed/selected/disabled/cooldown/unaffordable",
                "Kontrast, touch target, color-independence"
            };

            foreach (string token in requiredIndexTokens)
            {
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-100 index does not mention required token: {token}");
            }

            Debug.Log("[DC_ART_BIBLE_STANDARD_INDEX_CHECKPOINT] passed " +
                      "vis101_111=indexed references=12 palette/value/scale/material/silhouette/animation/vfx/ui/accessibility");
        }

        public static void ValidateActorCatalogVisualDevelopmentCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            var errors = catalog.ValidationErrors();
            if (errors.Count > 0)
                throw new InvalidOperationException("Production catalog errors:\n- " + string.Join("\n- ", errors));

            var warnings = catalog.VisualDevelopmentValidationWarnings();
            bool actorSocketWarning = warnings.Any(item => item.Contains("VIS-203") && item.Contains("Socket_"));
            bool knightMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Knight"));
            bool guardMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Guard"));
            bool raiderMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Raider"));
            bool vanguardMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Vanguard"));
            bool regeneratorMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Regenerator"));
            bool supportMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Support"));
            bool siegeMaterialWarning = warnings.Any(item => item.Contains("VIS-206") && item.Contains("Siege"));
            bool actorPrefabShared = warnings.Any(item => item.Contains("VIS-201"));
            bool outpostSocketWarning = warnings.Any(item => item.Contains("VIS-303") && item.Contains("Socket_"));

            if (actorSocketWarning || knightMaterialWarning || guardMaterialWarning || raiderMaterialWarning ||
                vanguardMaterialWarning || regeneratorMaterialWarning || supportMaterialWarning ||
                siegeMaterialWarning ||
                actorPrefabShared || outpostSocketWarning)
            {
                throw new InvalidOperationException("Actor catalog visual checkpoint failed:\n- " +
                                                    string.Join("\n- ", warnings));
            }

            Debug.Log("[DC_ACTOR_CATALOG_VISUAL_CHECKPOINT] passed " +
                      $"warnings={warnings.Count} actorSocketWarnings=0 knightMaterialWarnings=0 guardMaterialWarnings=0 " +
                      $"raiderMaterialWarnings=0 vanguardMaterialWarnings=0 regeneratorMaterialWarnings=0 " +
                      $"supportMaterialWarnings=0 siegeMaterialWarnings=0 sharedActorPrefabs=0 outpostSocketWarnings=0");
        }

        public static void ValidateActorPipelineIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS200_ACTOR_PIPELINE_INDEX_2026-08-17.md",
                "docs/ACTOR_MARKER_SHADOW_STANDARD_2026-08-16.md",
                "docs/ACTOR_ANIMATION_CONTRACT_2026-08-16.md",
                "docs/ACTOR_MATERIAL_IMPORT_STANDARD_2026-08-16.md",
                "docs/ACTOR_ICON_RENDER_STANDARD_2026-08-16.md",
                "docs/CATALOG_VISUAL_VALIDATION_2026-08-16.md",
                "docs/evidence/visual-development/actor-proxy-metrics-2026-08-17.json",
                "docs/evidence/visual-development/actor-icon-64px-review-2026-08-17.png",
                "Builds/unity-actor-animation-visual-checkpoint.log",
                "Builds/unity-actor-animator-override-checkpoint.log",
                "Builds/unity-actor-animator-priority-checkpoint.log",
                "Builds/unity-actor-catalog-visual-checkpoint.log"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Actor pipeline evidence file is missing: {relativePath}");
            }

            foreach (PresentationActorKind kind in Enum.GetValues(typeof(PresentationActorKind)))
            {
                string relativePath = $"docs/evidence/visual-development/actor-icons-2026-08-17/{kind}.png";
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Actor role icon is missing: {relativePath}");
            }

            string indexPath = Path.Combine(root, "docs/VIS200_ACTOR_PIPELINE_INDEX_2026-08-17.md");
            string index = File.ReadAllText(indexPath);
            for (int id = 204; id <= 208; id++)
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-200 actor pipeline index does not mention {token}.");
            }

            if (!index.Contains("VIS-202 açık", StringComparison.Ordinal))
                throw new InvalidOperationException("VIS-200 actor pipeline index must explicitly leave VIS-202 open.");

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            int actorCount = catalog.actorVisuals.Count(entry => entry != null);
            int expectedActorCount = Enum.GetValues(typeof(PresentationActorKind)).Length;
            if (actorCount != expectedActorCount)
                throw new InvalidOperationException($"Actor catalog role count mismatch: {actorCount}/{expectedActorCount}.");

            var warnings = catalog.VisualDevelopmentValidationWarnings();
            bool actorRoleCoverageWarning = warnings.Any(item => item.Contains("VIS-208", StringComparison.Ordinal));
            bool actorSocketWarning = warnings.Any(item => item.Contains("VIS-203", StringComparison.Ordinal) &&
                                                           item.Contains("Socket_", StringComparison.Ordinal));
            bool knightMaterialWarning = warnings.Any(item => item.Contains("VIS-206", StringComparison.Ordinal) &&
                                                              item.Contains("Knight", StringComparison.Ordinal));
            bool raiderMaterialWarning = warnings.Any(item => item.Contains("VIS-206", StringComparison.Ordinal) &&
                                                              item.Contains("Raider", StringComparison.Ordinal));
            if (actorRoleCoverageWarning || actorSocketWarning || knightMaterialWarning || raiderMaterialWarning)
                throw new InvalidOperationException("Actor pipeline catalog warnings remain:\n- " +
                                                    string.Join("\n- ", warnings));

            GameObject knight = StylizedPresentationFactory.CreateRoot("Knight Pipeline Check", Vector3.zero,
                new Vector3(1f, 2f, 1f), catalog.ActorPrefab(PresentationActorKind.Knight), catalog,
                PresentationCollider.Capsule, catalog.ActorScale(PresentationActorKind.Knight),
                catalog.ActorAnimator(PresentationActorKind.Knight), PresentationActorKind.Knight);
            try
            {
                Transform contactShadow = FindDescendant(knight.transform, "Actor Contact Shadow");
                if (contactShadow == null || !contactShadow.TryGetComponent(out MeshRenderer shadowRenderer))
                    throw new InvalidOperationException("Knight actor contact shadow layer is missing.");
                if (shadowRenderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ||
                    shadowRenderer.receiveShadows)
                    throw new InvalidOperationException("Actor contact shadow must not cast or receive shadows.");
                if (!HasDescendant(knight.transform, "Defender Ground Aura"))
                    throw new InvalidOperationException("Knight actor defender aura layer is missing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(knight);
            }

            Debug.Log("[DC_ACTOR_PIPELINE_INDEX_CHECKPOINT] passed " +
                      "vis204_208=indexed icons=8 contactShadow=Off/receiveFalse catalogActors=8 vis202=open");
        }

        public static void ValidateBuildingPipelineIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS300_BUILDING_PIPELINE_INDEX_2026-08-17.md",
                "docs/BUILDING_PIPELINE_STANDARD_2026-08-16.md",
                "docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md",
                "docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md",
                "docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md",
                "docs/BUILDING_QA_READABILITY_2026-08-16.md",
                "docs/BUILDING_BUDGET_REPORT_2026-08-16.md",
                "docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json",
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json",
                "docs/evidence/visual-development/building-proxy-contact-sheet-2026-08-16.png",
                "docs/evidence/visual-development/building-readability-sheet-2026-08-16.png",
                "docs/evidence/visual-development/building-qa-sheet-2026-08-16.png",
                "docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png",
                "Builds/unity-visual-building-binding.log",
                "Builds/ios-slot-regression-smoke.log"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Building pipeline evidence file is missing: {relativePath}");
            }

            string[] generatedBuildings =
            {
                "Archer_L1", "Archer_L2", "Barracks_L1", "Barracks_L2", "Mage_L1", "Mage_L2"
            };

            foreach (string building in generatedBuildings)
            {
                string relativePath = $"Assets/Art/Models/Generated/{building}.fbx";
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Generated building candidate is missing: {relativePath}");
            }

            string index = File.ReadAllText(Path.Combine(root, "docs/VIS300_BUILDING_PIPELINE_INDEX_2026-08-17.md"));
            for (int id = 301; id <= 304; id++)
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-300 building pipeline index does not mention {token}.");
            }

            string metrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json"));
            string budget = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json"));
            foreach (string building in generatedBuildings)
            {
                if (!metrics.Contains($"\"id\": \"{building}\"", StringComparison.Ordinal) ||
                    !metrics.Contains($"\"path\": \"Assets/Art/Models/Generated/{building}.fbx\"", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building metrics do not cover generated candidate: {building}.");
            }

            string[] requiredMetricTokens =
            {
                "\"generated_candidate_count\": 6",
                "\"building_assets_with_socket_gaps\": []",
                "\"generated_candidate_budget_status\": \"Pass for current generated building FBX geometry/material soft budgets.\""
            };
            foreach (string token in requiredMetricTokens)
            {
                if (!metrics.Contains(token, StringComparison.Ordinal) &&
                    !budget.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building pipeline metrics do not mention required token: {token}");
            }

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            int outpostCount = catalog.outpostVisuals.Count(entry => entry != null);
            if (outpostCount != 6)
                throw new InvalidOperationException($"Outpost catalog entry count mismatch: {outpostCount}/6.");
            foreach (OutpostKind kind in Enum.GetValues(typeof(OutpostKind)))
            {
                for (int level = 1; level <= 2; level++)
                {
                    if (catalog.OutpostPrefab(kind, level) == null)
                        throw new InvalidOperationException($"Outpost catalog prefab is missing: {kind} L{level}.");
                }
            }

            var warnings = catalog.VisualDevelopmentValidationWarnings();
            if (warnings.Any(item => item.Contains("VIS-304", StringComparison.Ordinal) ||
                                     item.Contains("VIS-301", StringComparison.Ordinal) ||
                                     item.Contains("VIS-302", StringComparison.Ordinal)))
                throw new InvalidOperationException("Building pipeline catalog coverage/material warnings remain:\n- " +
                                                    string.Join("\n- ", warnings));

            Debug.Log("[DC_BUILDING_PIPELINE_INDEX_CHECKPOINT] passed " +
                      "vis301_304=indexed generatedBuildings=6 socketGaps=0 materials<=3 catalogOutposts=6 hardFail=open");
        }

        public static void ValidateBuildingFamilyIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS310_330_BUILDING_FAMILY_INDEX_2026-08-17.md",
                "docs/ARCHER_BUILDING_PROGRESSION_2026-08-16.md",
                "docs/BUILDING_GENERATED_CANDIDATE_PASS_2026-08-16.md",
                "docs/BUILDING_GENERATED_CATALOG_BINDING_RUNTIME_PASS_2026-08-16.md",
                "docs/BUILDING_MATERIAL_IMPORT_STANDARD_2026-08-16.md",
                "docs/BUILDING_QA_READABILITY_2026-08-16.md",
                "docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json",
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json",
                "docs/evidence/visual-development/building-readability-sheet-2026-08-16.png",
                "docs/evidence/visual-development/building-qa-sheet-2026-08-16.png",
                "docs/evidence/visual-development/building-blind-recognition-key-2026-08-16.json",
                "docs/evidence/runtime/defender-simulator-building-generated-catalog-binding-upgraded-state-landscape-readable.png",
                "Builds/unity-visual-building-binding.log"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Building family evidence file is missing: {relativePath}");
            }

            string index = File.ReadAllText(Path.Combine(root, "docs/VIS310_330_BUILDING_FAMILY_INDEX_2026-08-17.md"));
            int[] requiredIds =
            {
                310, 311, 312, 313, 315, 316, 317,
                320, 321, 322, 323, 324, 325, 326,
                330, 331, 332, 333, 334, 335, 336
            };
            foreach (int id in requiredIds)
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building family index does not mention {token}.");
            }

            string[] generatedBuildings =
            {
                "Archer_L1", "Archer_L2", "Barracks_L1", "Barracks_L2", "Mage_L1", "Mage_L2"
            };
            string metrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/building-proxy-metrics-2026-08-16.json"));
            string budget = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json"));
            foreach (string building in generatedBuildings)
            {
                string relativePath = $"Assets/Art/Models/Generated/{building}.fbx";
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Generated building family candidate is missing: {relativePath}");
                if (!metrics.Contains($"\"id\": \"{building}\"", StringComparison.Ordinal) ||
                    !budget.Contains($"\"id\": \"{building}\"", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Metrics/budget evidence does not cover: {building}.");
            }

            string[] requiredEvidenceTokens =
            {
                "\"generated_candidate_count\": 6",
                "\"building_assets_with_socket_gaps\": []",
                "\"building_assets_over_material_budget\": []",
                "\"generated_candidate_budget_status\": \"Pass for current generated building FBX geometry/material soft budgets.\""
            };
            foreach (string token in requiredEvidenceTokens)
            {
                if (!metrics.Contains(token, StringComparison.Ordinal) &&
                    !budget.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building family evidence does not mention required token: {token}");
            }

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            foreach (OutpostKind kind in Enum.GetValues(typeof(OutpostKind)))
            {
                GameObject l1 = catalog.OutpostPrefab(kind, 1);
                GameObject l2 = catalog.OutpostPrefab(kind, 2);
                if (l1 == null || l2 == null)
                    throw new InvalidOperationException($"Outpost family is missing catalog binding: {kind}.");
                if (l1 == l2)
                    throw new InvalidOperationException($"Outpost family L1/L2 share the same prefab: {kind}.");
            }

            Debug.Log("[DC_BUILDING_FAMILY_INDEX_CHECKPOINT] passed " +
                      "vis310_336=indexed families=Archer/Barracks/Mage generatedCandidates=6 " +
                      "socketGaps=0 materials<=3 catalogBindings=6 runtimeEvidence=upgradedScreenshot");
        }

        public static void ValidateBuildingStateVfxQaIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS340_BUILDING_STATE_VFX_QA_INDEX_2026-08-17.md",
                "docs/SLOT_STATE_STANDARD_2026-08-16.md",
                "docs/BUILD_VFX_STANDARD_2026-08-16.md",
                "docs/UPGRADE_VFX_STANDARD_2026-08-16.md",
                "docs/SELL_VFX_STANDARD_2026-08-16.md",
                "docs/BUILDING_QA_READABILITY_2026-08-16.md",
                "docs/BUILDING_BUDGET_REPORT_2026-08-16.md",
                "docs/evidence/visual-development/slot-state-sheet-2026-08-16.png",
                "docs/evidence/visual-development/slot-state-metrics-2026-08-16.json",
                "docs/evidence/visual-development/build-vfx-sheet-2026-08-16.png",
                "docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json",
                "docs/evidence/visual-development/upgrade-vfx-sheet-2026-08-16.png",
                "docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json",
                "docs/evidence/visual-development/sell-vfx-sheet-2026-08-16.png",
                "docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json",
                "docs/evidence/visual-development/building-qa-sheet-2026-08-16.png",
                "docs/evidence/visual-development/building-blind-recognition-key-2026-08-16.json",
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Building state/VFX/QA evidence file is missing: {relativePath}");
            }

            string index = File.ReadAllText(Path.Combine(root, "docs/VIS340_BUILDING_STATE_VFX_QA_INDEX_2026-08-17.md"));
            foreach (int id in new[] { 340, 341, 342, 343, 344, 346 })
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building state/VFX/QA index does not mention {token}.");
            }

            string[] generatedAssets =
            {
                "OutpostSlot_Empty", "OutpostSlot_Selected", "OutpostSlot_Available",
                "OutpostSlot_Insufficient", "OutpostSlot_Locked",
                "BuildVfx_Dust", "BuildVfx_Ring", "BuildVfx_Spark",
                "UpgradeVfx_Anchor", "UpgradeVfx_Column", "UpgradeVfx_Crest",
                "SellVfx_Ring", "SellVfx_Chips", "SellVfx_Glint"
            };

            foreach (string asset in generatedAssets)
            {
                string relativePath = $"Assets/Art/Models/Generated/{asset}.fbx";
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Generated building state/VFX asset is missing: {relativePath}");
            }

            string slotMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/slot-state-metrics-2026-08-16.json"));
            string buildMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/build-vfx-metrics-2026-08-16.json"));
            string upgradeMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/upgrade-vfx-metrics-2026-08-16.json"));
            string sellMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/sell-vfx-metrics-2026-08-16.json"));
            string budget = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/building-budget-report-2026-08-16.json"));

            string[] requiredTokens =
            {
                "OutpostSlot_Empty",
                "OutpostSlot_Selected",
                "OutpostSlot_Available",
                "OutpostSlot_Insufficient",
                "OutpostSlot_Locked",
                "BuildVfx_Dust",
                "UpgradeVfx_Anchor",
                "SellVfx_Ring",
                "\"building_assets_with_socket_gaps\": []",
                "\"building_assets_over_material_budget\": []",
                "\"vfx_assets_over_material_budget\": []",
                "\"vfx_total_over_triangle_soft_budget\": false"
            };
            string combined = slotMetrics + buildMetrics + upgradeMetrics + sellMetrics + budget;
            foreach (string token in requiredTokens)
            {
                if (!combined.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Building state/VFX/QA evidence does not mention required token: {token}");
            }

            Debug.Log("[DC_BUILDING_STATE_VFX_QA_INDEX_CHECKPOINT] passed " +
                      "vis340_344_346=indexed slotStates=5 actionVfx=9 materials<=3 " +
                      "buildingQaSheet=present budget=pass runtimeBinding=open");
        }

        public static void ValidateEnvironmentIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS400_ENVIRONMENT_INDEX_2026-08-17.md",
                "docs/GROUND_VARIATION_PASS_2026-08-16.md",
                "docs/ROAD_VARIATION_PASS_2026-08-16.md",
                "docs/ENVIRONMENT_VALUE_RUNTIME_PASS_2026-08-16.md",
                "docs/ROAD_SURFACE_RUNTIME_PASS_2026-08-16.md",
                "docs/ROAD_BORDER_RHYTHM_PASS_2026-08-16.md",
                "docs/BOUNDARY_DRESSING_PASS_2026-08-16.md",
                "docs/ATMOSPHERE_PASS_2026-08-16.md",
                "docs/LIGHTING_PASS_2026-08-16.md",
                "docs/LIGHTING_MATERIAL_BALANCE_RUNTIME_PASS_2026-08-16.md",
                "docs/LIGHTING_QUALITY_56_CONTROL_2026-08-16.md",
                "docs/evidence/visual-development/ground-variation-metrics-2026-08-16.json",
                "docs/evidence/visual-development/ground-variation-sheet-2026-08-16.png",
                "docs/evidence/visual-development/road-variation-metrics-2026-08-16.json",
                "docs/evidence/visual-development/road-variation-sheet-2026-08-16.png",
                "docs/evidence/visual-development/boundary-layout-2026-08-16.json",
                "docs/evidence/visual-development/boundary-layout-metrics-2026-08-16.json",
                "docs/evidence/visual-development/boundary-layout-2026-08-16.png",
                "docs/evidence/visual-development/boundary-asset-metrics-2026-08-16.json",
                "docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json",
                "docs/evidence/runtime/defender-simulator-environment-value-pass-adjusted-late-2026-08-16-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-road-organic-surface-normals-late-2026-08-16-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-road-cobble-rhythm-late-2026-08-16-landscape-readable.png",
                "docs/evidence/runtime/defender-simulator-lighting-material-balance-late-2026-08-16-landscape-readable.png"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Environment evidence file is missing: {relativePath}");
            }

            string index = File.ReadAllText(Path.Combine(root, "docs/VIS400_ENVIRONMENT_INDEX_2026-08-17.md"));
            foreach (int id in new[] { 404, 405, 410, 411, 412, 413, 414, 415 })
            {
                string token = $"VIS-{id}";
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Environment index does not mention {token}.");
            }

            foreach (int openId in new[] { 413, 414, 415 })
            {
                string token = $"VIS-{openId}:";
                if (!index.Contains(token, StringComparison.Ordinal) ||
                    !index.Contains("açık kalır", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Environment index must explicitly leave VIS-{openId} open.");
            }

            string[] generatedAssets =
            {
                "GreenwatchGround_West", "GreenwatchGround_Mid", "GreenwatchGround_Final",
                "GreenwatchRoad_Surface", "GreenwatchRoad_Edges", "GreenwatchRoad_Wear",
                "GreenwatchBoundary_RidgeWall_A", "GreenwatchBoundary_RidgeWall_B",
                "GreenwatchBoundary_ForegroundStrip_A", "GreenwatchBoundary_ForegroundStrip_B",
                "GreenwatchBoundary_SilhouetteWall", "GreenwatchBoundary_ForestSilhouette",
                "Atmosphere_MoonDisc", "Atmosphere_FogRibbon", "Atmosphere_DustVeil"
            };

            foreach (string asset in generatedAssets)
            {
                string relativePath = $"Assets/Art/Models/Generated/{asset}.fbx";
                string metaPath = $"{relativePath}.meta";
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Generated environment asset is missing: {relativePath}");
                if (!File.Exists(Path.Combine(root, metaPath)))
                    throw new InvalidOperationException($"Generated environment asset meta is missing: {metaPath}");
            }

            string groundMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/ground-variation-metrics-2026-08-16.json"));
            string roadMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/road-variation-metrics-2026-08-16.json"));
            string boundaryLayoutMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/boundary-layout-metrics-2026-08-16.json"));
            string boundaryAssetMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/boundary-asset-metrics-2026-08-16.json"));
            string atmosphereMetrics = File.ReadAllText(Path.Combine(root,
                "docs/evidence/visual-development/atmosphere-asset-metrics-2026-08-16.json"));

            string combined = groundMetrics + roadMetrics + boundaryLayoutMetrics +
                              boundaryAssetMetrics + atmosphereMetrics;
            string[] requiredTokens =
            {
                "\"generated_asset_count\": 3",
                "\"total_triangles\": 126",
                "\"total_triangles\": 1343",
                "\"total_placements\": 10",
                "\"violations_count\": 0",
                "\"ready_for_runtime_integration\": true",
                "\"generated_asset_count\": 6",
                "\"total_triangles\": 4632",
                "\"total_triangles\": 688",
                "\"assets_with_imported_colliders_enabled\": []"
            };
            foreach (string token in requiredTokens)
            {
                if (!combined.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Environment evidence does not mention required token: {token}");
            }

            string todo = File.ReadAllText(Path.Combine(root, "docs/VISUAL_DEVELOPMENT_TODO.md"));
            foreach (int reviewId in new[] { 404, 405, 410, 411, 412 })
            {
                string rowToken = $"| VIS-{reviewId} |";
                string reviewToken = $"| VIS-{reviewId} |";
                int rowStart = todo.IndexOf(rowToken, StringComparison.Ordinal);
                if (rowStart < 0)
                    throw new InvalidOperationException($"Visual development TODO does not mention VIS-{reviewId}.");

                int rowEnd = todo.IndexOf('\n', rowStart);
                string row = rowEnd >= 0 ? todo.Substring(rowStart, rowEnd - rowStart) : todo.Substring(rowStart);
                if (!row.Contains("| İncelemede |", StringComparison.Ordinal))
                    throw new InvalidOperationException($"{reviewToken} is not marked İncelemede.");
            }

            Debug.Log("[DC_ENVIRONMENT_INDEX_CHECKPOINT] passed " +
                      "vis404_405_410_411_412=indexed ground=3 road=3 boundary=6 " +
                      "atmosphere=3 layoutViolations=0 runtimeProof=4 qaOpen=VIS413/VIS414/VIS415");
        }

        public static void ValidateEnvironmentQaIndexCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS413_414_ENVIRONMENT_QA_INDEX_2026-08-17.md",
                "docs/ENVIRONMENT_56_CONTROL_2026-08-16.md",
                "Builds/editmode-results-vis413.xml",
                "Builds/playmode-results-vis414.xml",
                "Builds/editmode-vis413.log",
                "Builds/playmode-vis414.log"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Environment QA evidence file is missing: {relativePath}");
            }

            string index = File.ReadAllText(Path.Combine(root,
                "docs/VIS413_414_ENVIRONMENT_QA_INDEX_2026-08-17.md"));
            string editXml = File.ReadAllText(Path.Combine(root, "Builds/editmode-results-vis413.xml"));
            string playXml = File.ReadAllText(Path.Combine(root, "Builds/playmode-results-vis414.xml"));
            string todo = File.ReadAllText(Path.Combine(root, "docs/VISUAL_DEVELOPMENT_TODO.md"));

            string[] requiredIndexTokens =
            {
                "VIS-413 | Yapılıyor",
                "VIS-414 | İncelemede",
                "CameraFramingCoversVisualDevelopmentAspectRatios",
                "ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot",
                "screenshot seti hâlâ açık kalır",
                "VIS-415"
            };
            foreach (string token in requiredIndexTokens)
            {
                if (!index.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Environment QA index does not mention required token: {token}");
            }

            string[] requiredEditTokens =
            {
                "testcasecount=\"1\"",
                "result=\"Passed\"",
                "failed=\"0\"",
                "CameraFramingCoversVisualDevelopmentAspectRatios"
            };
            foreach (string token in requiredEditTokens)
            {
                if (!editXml.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-413 EditMode XML does not mention required token: {token}");
            }

            string[] requiredPlayTokens =
            {
                "testcasecount=\"1\"",
                "result=\"Passed\"",
                "failed=\"0\"",
                "ProductionPresentationAllowsBuildUpgradeSellOnEveryFixedSlot"
            };
            foreach (string token in requiredPlayTokens)
            {
                if (!playXml.Contains(token, StringComparison.Ordinal))
                    throw new InvalidOperationException($"VIS-414 PlayMode XML does not mention required token: {token}");
            }

            AssertTodoRowStatus(todo, 413, "Yapılıyor");
            AssertTodoRowStatus(todo, 414, "İncelemede");
            AssertTodoRowStatus(todo, 415, "Bekliyor");

            Debug.Log("[DC_ENVIRONMENT_QA_INDEX_CHECKPOINT] passed " +
                      "vis413=editmodeRatio1/1_screenshotsOpen " +
                      "vis414=playmodeSlotPath1/1 profilerOpen=VIS415");
        }

        public static void ValidateEnvironmentBudgetProxyCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            var sessionObject = new GameObject("Environment Budget Proxy Session");
            var session = sessionObject.AddComponent<GameSessionController>();
            try
            {
                session.ConfigurePresentation(catalog);
                session.Initialize();

                string[] requiredObjects =
                {
                    "Greenwatch Environment",
                    "Scenic Battlefield Dressing",
                    "Road Organic Earth Surface",
                    "Road Hand-Laid Stone Borders",
                    "Ground Low-Contrast Valley Fields",
                    "Ground Moonlit Grass Breakup",
                    "Ground Warm Entry Scuffs",
                    "Enemy Gate Warm Read Pool",
                    "Defender Keep Cool Read Pool",
                    "Continuous Battlefield Ground"
                };
                foreach (string objectName in requiredObjects)
                {
                    if (GameObject.Find(objectName) == null)
                        throw new InvalidOperationException($"Environment runtime object is missing: {objectName}");
                }

                Transform dressing = GameObject.Find("Scenic Battlefield Dressing").transform;
                Renderer[] renderers = dressing.GetComponentsInChildren<Renderer>(includeInactive: true);
                MeshFilter[] meshFilters = dressing.GetComponentsInChildren<MeshFilter>(includeInactive: true);
                Collider[] colliders = dressing.GetComponentsInChildren<Collider>(includeInactive: true);

                int triangleCount = meshFilters.Sum(filter =>
                    filter != null && filter.sharedMesh != null ? filter.sharedMesh.triangles.Length / 3 : 0);
                int materialSlots = renderers.Sum(renderer => renderer.sharedMaterials.Count(material => material != null));
                int uniqueMaterials = renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Distinct().Count();
                int shadowCasters = renderers.Count(renderer =>
                    renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off);
                int receiveShadowRenderers = renderers.Count(renderer => renderer.receiveShadows);
                int transparentLikeMaterials = renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Distinct()
                    .Count(material => material.HasProperty("_BaseColor") &&
                                       material.GetColor("_BaseColor").a < 0.995f);

                const int triangleSoftBudget = 8500;
                const int rendererSoftBudget = 16;
                const int uniqueMaterialSoftBudget = 16;
                const int materialSlotSoftBudget = 20;

                if (renderers.Length > rendererSoftBudget)
                    throw new InvalidOperationException($"Environment renderer budget exceeded: {renderers.Length}/{rendererSoftBudget}.");
                if (triangleCount > triangleSoftBudget)
                    throw new InvalidOperationException($"Environment triangle budget exceeded: {triangleCount}/{triangleSoftBudget}.");
                if (uniqueMaterials > uniqueMaterialSoftBudget)
                    throw new InvalidOperationException($"Environment unique material budget exceeded: {uniqueMaterials}/{uniqueMaterialSoftBudget}.");
                if (materialSlots > materialSlotSoftBudget)
                    throw new InvalidOperationException($"Environment material slot budget exceeded: {materialSlots}/{materialSlotSoftBudget}.");
                if (shadowCasters != 0 || receiveShadowRenderers != 0)
                    throw new InvalidOperationException("Environment scenic dressing must not cast or receive shadows: " +
                                                        $"casters={shadowCasters} receivers={receiveShadowRenderers}.");
                if (colliders.Length != 0)
                    throw new InvalidOperationException($"Scenic environment dressing has unexpected colliders: {colliders.Length}.");

                string reportPath = Path.Combine(root,
                    "docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json");
                WriteEnvironmentBudgetProxyReport(reportPath, renderers.Length, meshFilters.Length, triangleCount,
                    materialSlots, uniqueMaterials, shadowCasters, receiveShadowRenderers, colliders.Length,
                    transparentLikeMaterials, triangleSoftBudget, rendererSoftBudget, uniqueMaterialSoftBudget,
                    materialSlotSoftBudget);

                Debug.Log("[DC_ENVIRONMENT_BUDGET_PROXY_CHECKPOINT] passed " +
                          $"renderers={renderers.Length}/{rendererSoftBudget} triangles={triangleCount}/{triangleSoftBudget} " +
                          $"materials={uniqueMaterials}/{uniqueMaterialSoftBudget} materialSlots={materialSlots}/{materialSlotSoftBudget} " +
                          $"shadowCasters={shadowCasters} colliders={colliders.Length} profilerDeviceOpen=VIS415");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sessionObject);
            }
        }

        public static void ValidateQualityProfileMatrixCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Project root could not be resolved.");

            string[] requiredFiles =
            {
                "docs/VIS800_QUALITY_PROFILE_MATRIX_2026-08-17.md",
                "Builds/playmode-results-vis801-803.xml",
                "docs/VIS415_ENVIRONMENT_BUDGET_PROXY_2026-08-17.md",
                "docs/evidence/visual-development/environment-budget-proxy-report-2026-08-17.json",
                "docs/evidence/visual-development/vfx-profiler-overdraw-report-2026-08-17.json",
                "docs/LIGHTING_PASS_2026-08-16.md",
                "docs/LIGHTING_QUALITY_56_CONTROL_2026-08-16.md",
                "Assets/Settings/UniversalRP.asset"
            };

            foreach (string relativePath in requiredFiles)
            {
                if (!File.Exists(Path.Combine(root, relativePath)))
                    throw new InvalidOperationException($"Quality profile evidence file is missing: {relativePath}");
            }

            string playXml = File.ReadAllText(Path.Combine(root, "Builds/playmode-results-vis801-803.xml"));
            if (!playXml.Contains("SceneLookRespectsQualityLightingProfile", StringComparison.Ordinal) ||
                !playXml.Contains("testcasecount=\"1\"", StringComparison.Ordinal) ||
                !playXml.Contains("result=\"Passed\"", StringComparison.Ordinal) ||
                !playXml.Contains("failed=\"0\"", StringComparison.Ordinal))
                throw new InvalidOperationException("VIS-801/803 PlayMode profile XML is not a clean passed single-test report.");

            string urp = File.ReadAllText(Path.Combine(root, "Assets/Settings/UniversalRP.asset"));
            if (!urp.Contains("m_AdditionalLightShadowsSupported: 0", StringComparison.Ordinal))
                throw new InvalidOperationException("URP additional light shadows must stay disabled.");
            if (!urp.Contains("m_AdditionalLightsPerObjectLimit: 4", StringComparison.Ordinal))
                throw new InvalidOperationException("URP additional lights per object limit is not recorded as 4.");

            int originalQuality = QualitySettings.GetQualityLevel();
            int low = FindQualityIndexByName("low");
            int medium = FindQualityIndexByName("medium");
            int high = FindQualityIndexByName("high");
            if (low < 0 || medium < 0 || high < 0)
                throw new InvalidOperationException("Low, Medium and High quality tiers must exist.");

            var sceneRoot = new GameObject("Quality Profile Matrix Checkpoint");
            var camera = sceneRoot.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = Vector3.up * 5f;
            camera.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
            camera.tag = "MainCamera";

            try
            {
                QualityProfileSnapshot lowSnapshot = ApplyAndMeasureQuality(low, camera);
                QualityProfileSnapshot mediumSnapshot = ApplyAndMeasureQuality(medium, camera);
                QualityProfileSnapshot highSnapshot = ApplyAndMeasureQuality(high, camera);

                if (lowSnapshot.keyShadows != LightShadows.None || mediumSnapshot.keyShadows != LightShadows.None)
                    throw new InvalidOperationException("Low/Medium key light shadows must be disabled.");
                if (highSnapshot.keyShadows != LightShadows.Soft)
                    throw new InvalidOperationException("High key light must use soft shadows for the authored profile.");
                foreach (QualityProfileSnapshot snapshot in new[] { lowSnapshot, mediumSnapshot, highSnapshot })
                {
                    if (snapshot.pointLightCount != 2 || snapshot.pointShadowCasters != 0)
                        throw new InvalidOperationException($"Point light shadow budget failed for {snapshot.qualityName}: " +
                                                            $"points={snapshot.pointLightCount} pointShadows={snapshot.pointShadowCasters}");
                    if (snapshot.totalLightCount != 3)
                        throw new InvalidOperationException($"Expected exactly 3 runtime look lights for {snapshot.qualityName}, got {snapshot.totalLightCount}.");
                }
                if (Math.Abs(lowSnapshot.moonFillIntensity - 0.96f * 0.65f) > 0.001f ||
                    Math.Abs(lowSnapshot.moonFillRange - 24f * 0.6f) > 0.001f)
                    throw new InvalidOperationException("Low quality Moon Fill attenuation is incorrect.");

                string reportPath = Path.Combine(root,
                    "docs/evidence/visual-development/quality-profile-matrix-report-2026-08-17.json");
                WriteQualityProfileMatrixReport(reportPath, lowSnapshot, mediumSnapshot, highSnapshot);

                Debug.Log("[DC_QUALITY_PROFILE_MATRIX_CHECKPOINT] passed " +
                          "vis801_803=matrix low=keyNone/fillReduced medium=keyNone high=keySoft " +
                          "pointLights=2 pointShadows=0 urpAdditionalShadows=0 deviceProfilerOpen=VIS807");
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: true);
                UnityEngine.Object.DestroyImmediate(sceneRoot);
                foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light == null) continue;
                    if (light.name == "Greenwatch Moon Key" || light.name == "Moon Fill" ||
                        light.name == "Gate Ember Fill")
                        UnityEngine.Object.DestroyImmediate(light.gameObject);
                }
            }
        }

        public static void ValidateActorVfxVisualDevelopmentCheckpoint()
        {
            AssertVfx("Knight slash", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAttacked, Vector3.forward, actor: PresentationActorKind.Knight)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Fire, DefenderVfxImpactClass.PhysicalLight,
                true, 0.82f, "Socket_Weapon_Tip");

            AssertVfx("Knight shield bash", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Knight)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalBlock,
                false, 3.2f, "Socket_Ground");

            AssertVfx("Guard spear thrust", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostAttacked, Vector3.zero, actor: PresentationActorKind.Guard,
                outpost: OutpostKind.Barracks)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalLight,
                false, 0.56f, "Socket_Weapon_Tip");

            AssertVfx("Guard block parry", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Guard)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalBlock,
                false, 1.65f, "Socket_Chest");

            AssertVfx("Raider spawn", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.UnitSpawned, Vector3.zero, actor: PresentationActorKind.Raider)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.SpawnDeath,
                false, 0.95f, "Socket_Ground");

            AssertVfx("Raider hit", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalLight,
                true, 0.52f, "Socket_Chest");

            AssertVfx("Raider death", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.EnemyDefeated, Vector3.zero, actor: PresentationActorKind.Raider)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.SpawnDeath,
                false, 1.18f, "Socket_Ground");

            AssertVfx("Vanguard heavy impact", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Vanguard)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalHeavy,
                false, 0.88f, "Socket_Weapon_Tip", DefenderVfxPriority.High, "VanguardImpact", 6);

            AssertVfx("Siege impact", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Siege)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.PhysicalHeavy,
                true, 1.12f, "Socket_Muzzle", DefenderVfxPriority.High, "SiegeImpact", 4);

            AssertVfx("Regenerator pulse", DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HealPulse, Vector3.zero, actor: PresentationActorKind.Regenerator)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.Magical,
                false, 1.18f, "Socket_Core", DefenderVfxPriority.Normal, "RegeneratorPulse", 8, "Socket_Ground");

            Debug.Log("[DC_ACTOR_VFX_VISUAL_CHECKPOINT] passed " +
                      "knightSlash=FireDiamond knightShield=DefenderRing " +
                      "guardThrust=DefenderDiamond guardBlock=DefenderRing " +
                      "raiderSpawn=DarkRing raiderHit=EnemyDiamond raiderDeath=EnemyRing " +
                      "vanguardHeavy=EnemyDiamond siegeImpact=DarkDiamond/Socket_Muzzle " +
                      "regeneratorPulse=EnemyRing/Socket_Core socketOrigins=WeaponTip/Muzzle/Ground/Chest/Core");
        }

        public static void ValidatePhysicalVfxFamilyCheckpoint()
        {
            AssertVfx("Light physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalLight,
                true, 0.52f, "Socket_Chest");

            AssertVfx("Block physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Knight)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalBlock,
                false, 3.2f, "Socket_Ground");

            AssertVfx("Guard block physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Guard)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalBlock,
                false, 1.65f, "Socket_Chest");

            AssertVfx("Heavy physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.BossAttack, Vector3.zero, actor: PresentationActorKind.Boss)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalHeavy,
                true, 1.05f, "Socket_Weapon_Tip", DefenderVfxPriority.High, "BossImpact", 3);

            AssertVfx("Vanguard heavy physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Vanguard)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalHeavy,
                false, 0.88f, "Socket_Weapon_Tip", DefenderVfxPriority.High, "VanguardImpact", 6);

            AssertVfx("Siege heavy physical", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Siege)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.PhysicalHeavy,
                true, 1.12f, "Socket_Muzzle", DefenderVfxPriority.High, "SiegeImpact", 4);

            Debug.Log("[DC_PHYSICAL_VFX_FAMILY_CHECKPOINT] passed " +
                      "light=RaiderHit/Diamond/Chest block=KnightShield/Ring/Ground guardBlock=Ring/Chest " +
                      "heavy=BossAttack/Diamond/WeaponTip vanguardHeavy=Diamond/WeaponTip siegeHeavy=Diamond/Muzzle " +
                      "classes=PhysicalLight/PhysicalBlock/PhysicalHeavy");
        }

        public static void ValidateArrowVfxFamilyCheckpoint()
        {
            AssertVfx("Archer arrow", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostAttacked, Vector3.zero, outpost: OutpostKind.Archer)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Fire, DefenderVfxImpactClass.PhysicalLight,
                true, 0.44f, "Socket_Muzzle");

            AssertVfx("Guard melee", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostAttacked, Vector3.zero, actor: PresentationActorKind.Guard,
                    outpost: OutpostKind.Barracks)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.PhysicalLight,
                false, 0.56f, "Socket_Weapon_Tip");

            AssertVfx("Mage impact", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.MageImpact, Vector3.zero, outpost: OutpostKind.Mage)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Magic, DefenderVfxImpactClass.Magical,
                true, 0.72f, "Socket_Cast");

            Debug.Log("[DC_ARROW_VFX_FAMILY_CHECKPOINT] passed " +
                      "arrow=Archer/Projectile/Socket_Muzzle guard=DefenderNoProjectile/Socket_Weapon_Tip " +
                      "mage=Magical/Socket_Cast");
        }

        public static void ValidateArcaneVfxFamilyCheckpoint()
        {
            AssertVfx("Arcane projectile", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.MageImpact, Vector3.zero, outpost: OutpostKind.Mage)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Magic, DefenderVfxImpactClass.Magical,
                true, 0.72f, "Socket_Cast");

            AssertVfx("Arrow projectile", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostAttacked, Vector3.zero, outpost: OutpostKind.Archer)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Fire, DefenderVfxImpactClass.PhysicalLight,
                true, 0.44f, "Socket_Muzzle");

            AssertVfx("Heal pulse", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.Magical,
                false, 1.45f, "Socket_Ground");

            Debug.Log("[DC_ARCANE_VFX_FAMILY_CHECKPOINT] passed " +
                      "arcane=Mage/Diamond/Magic/Socket_Cast arrow=Archer/Diamond/Fire/Socket_Muzzle " +
                      "heal=Ring/Defender/Socket_Ground");
        }

        public static void ValidateHealRegenVfxFamilyCheckpoint()
        {
            AssertVfx("Support heal", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero, actor: PresentationActorKind.Support)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.Magical,
                true, 0.96f, "Socket_Cast", DefenderVfxPriority.Normal, "SupportHeal", 6);

            AssertVfx("Regenerator pulse", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero, actor: PresentationActorKind.Regenerator)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.Magical,
                false, 1.18f, "Socket_Core", DefenderVfxPriority.Normal, "RegeneratorPulse", 8, "Socket_Ground");

            AssertVfx("Fallback heal", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender, DefenderVfxImpactClass.Magical,
                false, 1.45f, "Socket_Ground");

            Debug.Log("[DC_HEAL_REGEN_VFX_FAMILY_CHECKPOINT] passed " +
                      "support=Ring/Defender/Socket_Cast->World regen=Ring/Enemy/Socket_Core " +
                      "fallback=Ring/Defender/Socket_Ground");
        }

        public static void ValidateStatusMarkerVfxCheckpoint()
        {
            AssertVfx("Slow marker", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.SlowApplied, Vector3.zero, actor: PresentationActorKind.Raider)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Magic, DefenderVfxImpactClass.Status,
                false, 1.25f, "Socket_Chest");

            AssertVfx("Outpost disrupt marker", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.OutpostDisrupted, Vector3.zero, outpost: OutpostKind.Archer)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.Status,
                false, 1.35f, "Socket_Ground");

            AssertVfx("Boss phase marker", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.Status,
                false, 2.7f, "Socket_Ground", DefenderVfxPriority.Critical, "BossTelegraph", 2);

            Debug.Log("[DC_STATUS_MARKER_VFX_CHECKPOINT] passed " +
                      "slow=Ring/Magic/Socket_Chest disrupt=Ring/Dark/Socket_Ground " +
                      "bossPhase=Ring/Dark/Socket_Ground");
        }

        public static void ValidateBossVfxPriorityCheckpoint()
        {
            AssertVfx("Boss attack", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.BossAttack, Vector3.zero, actor: PresentationActorKind.Boss)),
                DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy, DefenderVfxImpactClass.PhysicalHeavy,
                true, 1.05f, "Socket_Weapon_Tip", DefenderVfxPriority.High, "BossImpact", 3);

            AssertVfx("Boss phase", DefenderVfxPool.Describe(new PresentationSignal(
                    PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss)),
                DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark, DefenderVfxImpactClass.Status,
                false, 2.7f, "Socket_Ground", DefenderVfxPriority.Critical, "BossTelegraph", 2);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            var host = new GameObject("Boss VFX Priority Checkpoint");
            var source = new GameObject("Boss VFX Priority Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));

            try
            {
                var pool = host.AddComponent<DefenderVfxPool>();
                pool.Initialize(catalog, 3);
                for (int i = 0; i < 6; i++)
                    pool.Play(new PresentationSignal(PresentationCue.BossAttack, Vector3.right * i, source,
                        PresentationActorKind.Boss));

                if (pool.PlayingCountForGroup("BossImpact") != 3)
                    throw new InvalidOperationException("Boss impact concurrency cap failed: " +
                                                        $"bossImpact={pool.PlayingCountForGroup("BossImpact")}");

                pool.Play(new PresentationSignal(PresentationCue.BossPhase, Vector3.zero, source,
                    PresentationActorKind.Boss));
                for (int i = 0; i < 24; i++)
                    pool.Play(new PresentationSignal(PresentationCue.EnemyHit, Vector3.forward * i, source,
                        PresentationActorKind.Raider));

                if (pool.PlayingCountForGroup("BossTelegraph") != 1 ||
                    pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical) != 1)
                    throw new InvalidOperationException("Boss telegraph priority was not preserved: " +
                                                        $"telegraph={pool.PlayingCountForGroup("BossTelegraph")} " +
                                                        $"critical={pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical)}");

                Debug.Log("[DC_BOSS_VFX_PRIORITY_CHECKPOINT] passed " +
                          "bossImpact=High/max3 bossPhase=Critical/BossTelegraph denseCombat=24 criticalPreserved=true");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        public static void ValidateFeedbackIntensityCheckpoint()
        {
            var bossAttack = new PresentationSignal(PresentationCue.BossAttack, Vector3.zero,
                actor: PresentationActorKind.Boss);
            PresentationFeedbackSpec full = PresentationFeedbackContract.Describe(bossAttack);
            PresentationFeedbackSpec reduced = PresentationFeedbackContract.Describe(bossAttack,
                PresentationFeedbackIntensityMode.Reduced);
            PresentationFeedbackSpec off = PresentationFeedbackContract.Describe(bossAttack,
                PresentationFeedbackIntensityMode.Off);
            PresentationFeedbackSpec bossPhase = PresentationFeedbackContract.Describe(
                new PresentationSignal(PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss));
            PresentationFeedbackSpec enemyHit = PresentationFeedbackContract.Describe(
                new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider));

            if (Math.Abs(full.cameraShakeAmplitude - 0.12f) > 0.001f ||
                Math.Abs(full.hitStopSeconds - 0.045f) > 0.001f ||
                reduced.cameraShakeAmplitude >= full.cameraShakeAmplitude ||
                reduced.hitStopSeconds >= full.hitStopSeconds ||
                off.cameraShakeAmplitude != 0f || off.cameraShakeSeconds != 0f || off.hitStopSeconds != 0f ||
                full.touchInputBlocked || reduced.touchInputBlocked || off.touchInputBlocked ||
                bossPhase.cameraShakeAmplitude <= enemyHit.cameraShakeAmplitude ||
                bossPhase.cameraShakeSeconds <= enemyHit.cameraShakeSeconds)
                throw new InvalidOperationException("Feedback intensity contract mismatch.");

            var host = new GameObject("Feedback Controller Checkpoint");
            try
            {
                var controller = host.AddComponent<DefenderPresentationFeedbackController>();
                controller.Initialize(null);
                controller.Mode = PresentationFeedbackIntensityMode.Off;
                controller.Play(bossAttack);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            Debug.Log("[DC_FEEDBACK_INTENSITY_CHECKPOINT] passed " +
                      "bossAttack=shake0.12/hitStop0.045 reduced=scaled off=zero touchBlocked=false " +
                      "bossPhaseAboveEnemyHit=true");
        }

        public static void ValidateBuildButtonStateCheckpoint()
        {
            Color accent = Color.yellow;
            DefenderHudController.BuildButtonPresentationSpec noSlot =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, false, 200, accent);
            DefenderHudController.BuildButtonPresentationSpec affordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 75, accent);
            DefenderHudController.BuildButtonPresentationSpec unaffordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 40, accent);

            if (noSlot.state != DefenderHudController.BuildButtonAvailabilityState.NoSlot ||
                noSlot.interactable || noSlot.detail != "SLOT SEÇ" || noSlot.warningStripe)
                throw new InvalidOperationException("Build button no-slot state mismatch.");

            if (affordable.state != DefenderHudController.BuildButtonAvailabilityState.Affordable ||
                !affordable.interactable || affordable.title != "OKÇU" || affordable.detail != "75" ||
                affordable.warningStripe)
                throw new InvalidOperationException("Build button affordable state mismatch.");

            if (unaffordable.state != DefenderHudController.BuildButtonAvailabilityState.Unaffordable ||
                unaffordable.interactable || unaffordable.title != "YETERSİZ" ||
                unaffordable.detail != "OKÇU 75" || !unaffordable.warningStripe)
                throw new InvalidOperationException("Build button unaffordable state mismatch.");

            Debug.Log("[DC_BUILD_BUTTON_STATE_CHECKPOINT] passed " +
	                      "noSlot=OKÇU/SLOT_SEÇ affordable=OKÇU/75 unaffordable=YETERSİZ/OKÇU_75 " +
	                      "warningStripe=true");
        }

        public static void ValidateTopBarHierarchyCheckpoint()
        {
            DefenderHudController.TopBarPresentationSpec stable =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 200, 20, 0, 7, GamePhase.Preparing);
            DefenderHudController.TopBarPresentationSpec warning =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 80, 5, 2, 7, GamePhase.Wave);
            DefenderHudController.TopBarPresentationSpec boss =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 120, 12, 6, 7, GamePhase.Intermission);
            DefenderHudController.TopBarPresentationSpec terminal =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 300, 0, 7, 7, GamePhase.Victory);

            if (stable.state != DefenderHudController.TopBarSignalState.Stable ||
                !stable.goldText.Contains("ALTIN", StringComparison.Ordinal) ||
                !stable.livesText.Contains("KALE", StringComparison.Ordinal) ||
                !stable.waveText.Contains("0/7", StringComparison.Ordinal) ||
                stable.livesWarning || stable.bossSignal)
                throw new InvalidOperationException("Top bar stable state mismatch.");

            if (warning.state != DefenderHudController.TopBarSignalState.Warning ||
                !warning.livesText.Contains("KALE!", StringComparison.Ordinal) ||
                !warning.waveText.Contains("3/7", StringComparison.Ordinal) ||
                !warning.livesWarning)
                throw new InvalidOperationException("Top bar warning state mismatch.");

            if (boss.state != DefenderHudController.TopBarSignalState.Boss ||
                !boss.waveText.Contains("BOSS", StringComparison.Ordinal) ||
                !boss.waveText.Contains("6/7", StringComparison.Ordinal) ||
                !boss.bossSignal)
                throw new InvalidOperationException("Top bar boss state mismatch.");

            if (terminal.state != DefenderHudController.TopBarSignalState.Terminal ||
                !terminal.waveText.Contains("7/7", StringComparison.Ordinal))
                throw new InvalidOperationException("Top bar terminal state mismatch.");

            Debug.Log("[DC_TOP_BAR_HIERARCHY_CHECKPOINT] passed " +
                      "stable=ALTIN/KALE/DALGA_0_7 warning=KALE_5 boss=BOSS_6_7 terminal=7_7");
        }

        public static void ValidatePhaseBadgeMotionCheckpoint()
        {
            DefenderHudController.PhaseBadgePresentationSpec preparing =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Preparing, false, false, 0f);
            DefenderHudController.PhaseBadgePresentationSpec transition =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Wave, false, false, 1f);
            DefenderHudController.PhaseBadgePresentationSpec bossUpcoming =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Intermission, true, false, 0f);
            DefenderHudController.PhaseBadgePresentationSpec bossActive =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Wave, false, true, 0f);

            if (preparing.hint != "DURUM" || preparing.label != "DALGA ÖNCESİ" ||
                Math.Abs(preparing.stripeHeight - 5f) > 0.001f || preparing.pulseAlpha != 0f)
                throw new InvalidOperationException("Phase badge preparing state mismatch.");

            if (transition.label != "SAVUNMA HATTI" ||
                transition.stripeHeight <= preparing.stripeHeight || transition.pulseAlpha <= 0f)
                throw new InvalidOperationException("Phase badge transition pulse mismatch.");

            if (bossUpcoming.hint != "KRİTİK" || bossUpcoming.label != "BOSS YAKLAŞIYOR" ||
                bossUpcoming.stripeHeight <= preparing.stripeHeight || bossUpcoming.pulseAlpha <= 0f)
                throw new InvalidOperationException("Phase badge boss upcoming mismatch.");

            if (bossActive.hint != "KRİTİK" || bossActive.label != "BOSS BASKISI" ||
                bossActive.stripeHeight <= bossUpcoming.stripeHeight || bossActive.pulseAlpha <= bossUpcoming.pulseAlpha)
                throw new InvalidOperationException("Phase badge boss active hierarchy mismatch.");

            Debug.Log("[DC_PHASE_BADGE_MOTION_CHECKPOINT] passed " +
	                      "prep=DALGA_ÖNCESİ/stripe5 transition=SAVUNMA_HATTI/pulse " +
	                      "bossUpcoming=BOSS_YAKLAŞIYOR bossActive=BOSS_BASKISI");
        }

        public static void ValidateSelectedPanelStateCheckpoint()
        {
            DefenderHudController.SelectedPanelPresentationSpec slot =
                DefenderHudController.SelectedSlotPanelSpec(4);
            DefenderHudController.SelectedPanelPresentationSpec upgrade =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Archer, 1, "Hızlı tek hedef",
                    24, 5.5f, 1.2f, true, 95, 45, false, 0f);
            DefenderHudController.SelectedPanelPresentationSpec max =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Mage, 2, "Alan kontrolü",
                    18, 4.5f, 0.8f, false, 0, 70, false, 0f);
            DefenderHudController.SelectedPanelPresentationSpec disrupted =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Barracks, 1, "Yol tutar",
                    10, 3.5f, 0.7f, true, 100, 60, true, 3.2f);

            if (slot.state != DefenderHudController.SelectedPanelState.EmptySlot ||
                slot.badge != "#5" || slot.upgradeInteractable || slot.sellInteractable)
                throw new InvalidOperationException("Selected panel empty slot state mismatch.");

            if (upgrade.state != DefenderHudController.SelectedPanelState.UpgradeAvailable ||
                upgrade.title != "OKÇU KARAKOLU" || upgrade.badge != "SV 1" ||
                !upgrade.hint.Contains("Yükseltme 95 altın", StringComparison.Ordinal) ||
                !upgrade.stats.Contains("HASAR", StringComparison.Ordinal) ||
                upgrade.upgradeLabel != "YÜKSELT\n95" || upgrade.sellLabel != "SAT\n+45" ||
                !upgrade.upgradeInteractable || !upgrade.sellInteractable)
                throw new InvalidOperationException("Selected panel upgrade state mismatch.");

            if (max.state != DefenderHudController.SelectedPanelState.MaxLevel ||
                !max.hint.Contains("Maks seviye", StringComparison.Ordinal) ||
                max.upgradeLabel != "MAKS\nSV" || max.upgradeInteractable || !max.sellInteractable)
                throw new InvalidOperationException("Selected panel max state mismatch.");

            if (disrupted.state != DefenderHudController.SelectedPanelState.Disrupted ||
                !disrupted.role.Contains("BOZULDU 4 sn", StringComparison.Ordinal) ||
                !disrupted.hint.Contains("Geçici baskı altında", StringComparison.Ordinal) ||
                !disrupted.warningStripe)
                throw new InvalidOperationException("Selected panel disrupted state mismatch.");

            Debug.Log("[DC_SELECTED_PANEL_STATE_CHECKPOINT] passed " +
                      "slot=#5 upgrade=OKÇU/SV1/YÜKSELT_95 sell=+45 max=MAKS disrupted=BOZULDU_4sn");
        }

        public static void ValidateHudButtonStateFamilyCheckpoint()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null || catalog.hudPrefab == null)
                throw new InvalidOperationException("Production HUD prefab is missing.");

            DefenderHudController.BuildButtonPresentationSpec noSlot =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, false, 200, Color.yellow);
            DefenderHudController.BuildButtonPresentationSpec affordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 75, Color.yellow);
            DefenderHudController.BuildButtonPresentationSpec unaffordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 40, Color.yellow);
            DefenderHudController.SelectedPanelPresentationSpec emptySlot =
                DefenderHudController.SelectedSlotPanelSpec(0);
            DefenderHudController.SelectedPanelPresentationSpec upgrade =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Archer, 1, "Hızlı tek hedef",
                    24, 5.5f, 1.2f, true, 95, 45, false, 0f);
            DefenderHudController.SelectedPanelPresentationSpec max =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Mage, 2, "Alan kontrolü",
                    18, 4.5f, 0.8f, false, 0, 70, false, 0f);

            if (noSlot.state != DefenderHudController.BuildButtonAvailabilityState.NoSlot ||
                noSlot.interactable || noSlot.warningStripe)
                throw new InvalidOperationException("HUD button family no-slot state mismatch.");
            if (affordable.state != DefenderHudController.BuildButtonAvailabilityState.Affordable ||
                !affordable.interactable || affordable.warningStripe)
                throw new InvalidOperationException("HUD button family affordable state mismatch.");
            if (unaffordable.state != DefenderHudController.BuildButtonAvailabilityState.Unaffordable ||
                unaffordable.interactable || !unaffordable.warningStripe)
                throw new InvalidOperationException("HUD button family unaffordable state mismatch.");
            if (emptySlot.upgradeInteractable || emptySlot.sellInteractable ||
                emptySlot.state != DefenderHudController.SelectedPanelState.EmptySlot)
                throw new InvalidOperationException("HUD button family selected empty-slot state mismatch.");
            if (!upgrade.upgradeInteractable || !upgrade.sellInteractable ||
                upgrade.state != DefenderHudController.SelectedPanelState.UpgradeAvailable)
                throw new InvalidOperationException("HUD button family selected upgrade state mismatch.");
            if (max.upgradeInteractable || !max.sellInteractable ||
                max.state != DefenderHudController.SelectedPanelState.MaxLevel)
                throw new InvalidOperationException("HUD button family selected max state mismatch.");

            GameObject instance = PrefabUtility.InstantiatePrefab(catalog.hudPrefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate production HUD prefab.");

            try
            {
                var hud = instance.GetComponent<DefenderHudController>();
                if (hud == null)
                    throw new InvalidOperationException("Production HUD prefab has no DefenderHudController.");

                hud.Initialize(catalog, _ => { }, () => { }, () => { }, () => { }, () => { }, () => { }, () => { },
                    () => { }, () => { }, () => { }, () => { });

                Button archerButton = FindButton(instance.transform, "Archer");
                Button abilityButton = FindButton(instance.transform, "Ability");
                if (archerButton == null || abilityButton == null)
                    throw new InvalidOperationException("Production HUD key buttons are missing.");

                ColorBlock archerColors = archerButton.colors;
                if (archerColors.normalColor == archerColors.pressedColor ||
                    archerColors.normalColor == archerColors.selectedColor ||
                    archerColors.normalColor == archerColors.disabledColor)
                    throw new InvalidOperationException("HUD button colors do not separate normal/pressed/selected/disabled.");

                if (!HasDescendant(abilityButton.transform, "CooldownFill") ||
                    !HasDescendant(abilityButton.transform, "ReadyGlow") ||
                    !HasDescendant(abilityButton.transform, "BlockedVeil"))
                    throw new InvalidOperationException("Ability button is missing cooldown/ready/blocked visual layers.");

                if (!HasDescendant(archerButton.transform, "StateStripe") ||
                    !HasDescendant(abilityButton.transform, "StateStripe"))
                    throw new InvalidOperationException("HUD buttons are missing state stripe layers.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log("[DC_HUD_BUTTON_STATE_FAMILY_CHECKPOINT] passed " +
                      "normal/pressed/selected/disabled build=noSlot/affordable/unaffordable " +
                      "ability=cooldown/ready/blocked selected=empty/upgrade/max");
        }

        public static void ValidateHudMaterialFrameCheckpoint()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null || catalog.hudPrefab == null)
                throw new InvalidOperationException("Production HUD prefab is missing.");
            if (catalog.panelSprite == null || catalog.uiFrameSprite == null)
                throw new InvalidOperationException("HUD panel/frame sprites are missing from the presentation catalog.");

            GameObject instance = PrefabUtility.InstantiatePrefab(catalog.hudPrefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate production HUD prefab.");

            try
            {
                var hud = instance.GetComponent<DefenderHudController>();
                if (hud == null)
                    throw new InvalidOperationException("Production HUD prefab has no DefenderHudController.");

                hud.Initialize(catalog, _ => { }, () => { }, () => { }, () => { }, () => { }, () => { }, () => { },
                    () => { }, () => { }, () => { }, () => { });

                string[] panelNames = { "TopBar", "BottomBar", "SelectedPanel", "ResultPanel" };
                foreach (string panelName in panelNames)
                {
                    Image panel = FindImage(instance.transform, panelName);
                    if (panel == null)
                        throw new InvalidOperationException($"HUD panel image is missing: {panelName}.");
                    if (panel.sprite != catalog.panelSprite || panel.type != Image.Type.Sliced)
                        throw new InvalidOperationException($"HUD panel does not use the sliced catalog panel sprite: {panelName}.");
                }

                string[] framedPanelNames = { "SelectedPanel", "ResultPanel" };
                foreach (string panelName in framedPanelNames)
                {
                    Image panel = FindImage(instance.transform, panelName);
                    Image frame = FindChildImage(panel.transform, "FrameOverlay");
                    if (frame == null || frame.sprite != catalog.uiFrameSprite || frame.type != Image.Type.Sliced)
                        throw new InvalidOperationException($"HUD panel frame overlay is missing or not sliced: {panelName}.");
                    if (frame.raycastTarget)
                        throw new InvalidOperationException($"HUD panel frame overlay should not block input: {panelName}.");
                }

                Image phaseChrome = FindImage(instance.transform, "PhaseChrome");
                if (phaseChrome == null || phaseChrome.sprite != catalog.panelSprite ||
                    phaseChrome.type != Image.Type.Sliced)
                    throw new InvalidOperationException("Phase badge chrome does not use the catalog panel sprite.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log("[DC_HUD_MATERIAL_FRAME_CHECKPOINT] passed " +
                      "catalogPanel=sliced catalogFrame=sliced panels=TopBar/BottomBar/SelectedPanel/ResultPanel phaseChrome=sliced");
        }

        public static void ValidateActorAnimationVisualDevelopmentCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            if (catalog.actorAnimator == null)
                throw new InvalidOperationException("Shared actor AnimatorController is missing.");

            foreach (string clipName in PresentationActorAnimationContract.ClipNames)
            {
                if (catalog.actorClips.All(clip => clip == null || clip.name != clipName))
                    throw new InvalidOperationException($"Shared actor animation clip is missing: {clipName}.");
            }

            AssertAnimationCue("Knight slash", PresentationCue.HeroAttacked, PresentationActorKind.Knight,
                PresentationActorAnimationContract.AttackState);
            AssertAnimationCue("Knight shield bash", PresentationCue.HeroAbility, PresentationActorKind.Knight,
                PresentationActorAnimationContract.AbilityState);
            AssertAnimationCue("Raider hit", PresentationCue.EnemyHit, PresentationActorKind.Raider,
                PresentationActorAnimationContract.HitState);
            AssertAnimationCue("Raider death echo", PresentationCue.EnemyDefeated, PresentationActorKind.Raider,
                PresentationActorAnimationContract.DefeatState);
            AssertAnimationCue("Vanguard heavy attack", PresentationCue.OutpostAttacked, PresentationActorKind.Vanguard,
                PresentationActorAnimationContract.AttackState);
            AssertAnimationCue("Vanguard stagger", PresentationCue.OutpostDisrupted, PresentationActorKind.Vanguard,
                PresentationActorAnimationContract.HitState);
            AssertAnimationCue("Regenerator regen pulse", PresentationCue.HealPulse, PresentationActorKind.Regenerator,
                PresentationActorAnimationContract.AbilityState);
            AssertAnimationCue("Support heal cast", PresentationCue.HealPulse, PresentationActorKind.Support,
                PresentationActorAnimationContract.AbilityState);
            AssertAnimationCue("Siege ram recoil", PresentationCue.OutpostDisrupted, PresentationActorKind.Siege,
                PresentationActorAnimationContract.HitState);

            Debug.Log("[DC_ACTOR_ANIMATION_VISUAL_CHECKPOINT] passed " +
                      $"clips={PresentationActorAnimationContract.ClipNames.Length} " +
                      "knightSlash=Attack knightShield=Ability raiderHit=Hit raiderDeath=Defeat " +
                      "vanguardHeavy=Attack vanguardStagger=Hit regeneratorPulse=Ability supportHeal=Ability " +
                      "siegeRecoil=Hit");
        }

        public static void ValidateActorVfxStressVisualDevelopmentCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            var host = new GameObject("Actor VFX Stress Checkpoint");
            var source = new GameObject("Socketed Actor Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));
            CreateSocket(source.transform, "Socket_Cast", new Vector3(1f, 1.35f, 0.18f));
            CreateSocket(source.transform, "Socket_Core", new Vector3(1f, 0.98f, -0.12f));

            try
            {
                var pool = host.AddComponent<DefenderVfxPool>();
                pool.Initialize(catalog, 96);
                for (int i = 0; i < 60; i++)
                    pool.Play(StressSignal(i, source));

                if (pool.PoolCount != 96 || pool.PlayingCount != 60)
                    throw new InvalidOperationException("Actor VFX stress checkpoint failed: " +
                                                        $"pool={pool.PoolCount} playing={pool.PlayingCount}");

                Debug.Log("[DC_ACTOR_VFX_STRESS_CHECKPOINT] passed signals=60 pool=96 playing=60 " +
                          "growth=0 families=KnightSlash/KnightShield/RaiderSpawn/RaiderHit/RaiderDeath");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        public static void ValidateVfxProfilerOverdrawCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            var host = new GameObject("VFX Profiler Overdraw Checkpoint");
            var source = new GameObject("VFX Profiler Socketed Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));
            CreateSocket(source.transform, "Socket_Muzzle", new Vector3(1f, 1.35f, 0.18f));
            CreateSocket(source.transform, "Socket_Cast", new Vector3(1f, 1.45f, -0.18f));

            try
            {
                var pool = host.AddComponent<DefenderVfxPool>();
                const int prewarm = 128;
                pool.Initialize(catalog, prewarm);

                List<PresentationSignal> signals = DenseVfxSignals(source);
                float transparentCost = 0f;
                foreach (PresentationSignal signal in signals)
                    transparentCost += EstimatedTransparentCost(DefenderVfxPool.Describe(signal));

                long heapBefore = GC.GetTotalMemory(false);
                foreach (PresentationSignal signal in signals)
                    pool.Play(signal);
                long heapAfter = GC.GetTotalMemory(false);
                long managedDelta = Math.Max(0, heapAfter - heapBefore);

                if (pool.PoolCount != prewarm)
                    throw new InvalidOperationException($"VFX pool grew during dense stress: pool={pool.PoolCount}.");
                int expectedPlaying = ExpectedPlayingWithConcurrencyCaps(signals);
                if (pool.PlayingCount != expectedPlaying)
                    throw new InvalidOperationException("Dense VFX playing count mismatch: " +
                                                        $"playing={pool.PlayingCount} expected={expectedPlaying} signals={signals.Count}.");
                if (pool.PlayingCountForGroup("BossImpact") != 3 || pool.PlayingCountForGroup("BossTelegraph") != 2)
                    throw new InvalidOperationException("Boss VFX concurrency caps failed during VIS-515 stress: " +
                                                        $"bossImpact={pool.PlayingCountForGroup("BossImpact")} " +
                                                        $"bossTelegraph={pool.PlayingCountForGroup("BossTelegraph")}");
                if (pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical) != 2)
                    throw new InvalidOperationException("Critical boss telegraph was not preserved during VIS-515 stress.");

                const float transparentBudget = 120f;
                if (transparentCost > transparentBudget)
                    throw new InvalidOperationException("Estimated transparent VFX budget exceeded: " +
                                                        $"cost={transparentCost:0.###} budget={transparentBudget:0.###}");

                WriteVfxProfilerOverdrawReport(signals.Count, pool.PoolCount, pool.PlayingCount, managedDelta,
                    transparentCost, transparentBudget);

                Debug.Log("[DC_VFX_PROFILER_OVERDRAW_CHECKPOINT] passed " +
                          $"signals={signals.Count} pool={pool.PoolCount} playing={pool.PlayingCount} growth=0 " +
                          $"bossImpact={pool.PlayingCountForGroup("BossImpact")} " +
                          $"bossTelegraph={pool.PlayingCountForGroup("BossTelegraph")} " +
                          $"critical={pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical)} " +
                          $"managedDeltaBytes={managedDelta} transparentCost={transparentCost:0.###}/{transparentBudget:0.###}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        public static void ValidateVfxQualityProfileCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");

            int originalQuality = QualitySettings.GetQualityLevel();
            int low = FindQualityIndexByName("low");
            int medium = FindQualityIndexByName("medium");
            int high = FindQualityIndexByName("high");
            if (low < 0 || medium < 0 || high < 0)
                throw new InvalidOperationException("VFX quality profile checkpoint needs Low, Medium and High quality names. Found: " +
                                                    string.Join(", ", QualitySettings.names ?? Array.Empty<string>()));

            var host = new GameObject("VFX Quality Profile Checkpoint");
            var source = new GameObject("VFX Quality Profile Socketed Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));
            CreateSocket(source.transform, "Socket_Muzzle", new Vector3(1f, 1.35f, 0.18f));
            CreateSocket(source.transform, "Socket_Cast", new Vector3(1f, 1.45f, -0.18f));

            try
            {
                List<PresentationSignal> signals = DenseVfxSignals(source);
                VfxQualityProfileSnapshot lowSnapshot = ApplyAndMeasureVfxQuality(catalog, host.transform, low, signals);
                VfxQualityProfileSnapshot mediumSnapshot = ApplyAndMeasureVfxQuality(catalog, host.transform, medium, signals);
                VfxQualityProfileSnapshot highSnapshot = ApplyAndMeasureVfxQuality(catalog, host.transform, high, signals);

                if (lowSnapshot.prewarmPoolSize >= mediumSnapshot.prewarmPoolSize ||
                    mediumSnapshot.prewarmPoolSize >= highSnapshot.prewarmPoolSize)
                    throw new InvalidOperationException("VFX quality prewarm sizes are not ordered Low < Medium < High.");
                if (lowSnapshot.bossImpactMaxConcurrent >= mediumSnapshot.bossImpactMaxConcurrent ||
                    lowSnapshot.bossTelegraphMaxConcurrent >= mediumSnapshot.bossTelegraphMaxConcurrent)
                    throw new InvalidOperationException("Low VFX boss concurrency caps were not reduced versus Medium.");
                if (highSnapshot.transparentCostBudget < mediumSnapshot.transparentCostBudget ||
                    mediumSnapshot.transparentCostBudget < lowSnapshot.transparentCostBudget)
                    throw new InvalidOperationException("VFX transparent budgets are not ordered Low <= Medium <= High.");

                string reportPath = Path.Combine(Application.dataPath, "../docs/evidence/visual-development/vfx-quality-profile-report-2026-08-17.json");
                WriteVfxQualityProfileReport(reportPath, lowSnapshot, mediumSnapshot, highSnapshot);

                Debug.Log("[DC_VFX_QUALITY_PROFILE_CHECKPOINT] passed " +
                          $"low=pool{lowSnapshot.poolCount}/play{lowSnapshot.playingCount}/boss{lowSnapshot.bossImpactPlaying}/{lowSnapshot.bossTelegraphPlaying}/budget{lowSnapshot.estimatedTransparentCost:0.###}/{lowSnapshot.transparentCostBudget:0.###} " +
                          $"medium=pool{mediumSnapshot.poolCount}/play{mediumSnapshot.playingCount}/boss{mediumSnapshot.bossImpactPlaying}/{mediumSnapshot.bossTelegraphPlaying}/budget{mediumSnapshot.estimatedTransparentCost:0.###}/{mediumSnapshot.transparentCostBudget:0.###} " +
                          $"high=pool{highSnapshot.poolCount}/play{highSnapshot.playingCount}/boss{highSnapshot.bossImpactPlaying}/{highSnapshot.bossTelegraphPlaying}/budget{highSnapshot.estimatedTransparentCost:0.###}/{highSnapshot.transparentCostBudget:0.###} " +
                          "growth=0 criticalPreserved=true");
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: true);
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        public static void ValidateTextureImportProfileCheckpoint()
        {
            DefenderPresentationAssetBuilder.ApplyMobileTextureImportProfile();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextureImportExpectation[] expectations =
            {
                new("Actor", "Assets/ThirdParty/KayKit/Adventurers/Models/knight_texture.png", 512, true, TextureImporterType.Default, TextureImporterFormat.ASTC_6x6),
                new("Actor", "Assets/ThirdParty/KayKit/Adventurers/Models/barbarian_texture.png", 512, true, TextureImporterType.Default, TextureImporterFormat.ASTC_6x6),
                new("Actor", "Assets/ThirdParty/KayKit/Adventurers/Models/mage_texture.png", 512, true, TextureImporterType.Default, TextureImporterFormat.ASTC_6x6),
                new("Actor", "Assets/ThirdParty/KayKit/Adventurers/Models/rogue_texture.png", 512, true, TextureImporterType.Default, TextureImporterFormat.ASTC_6x6),
                new("Environment/Building", "Assets/ThirdParty/KayKit/MedievalHexagon/Models/hexagons_medieval.png", 1024, true, TextureImporterType.Default, TextureImporterFormat.ASTC_8x8),
                new("UI", "Assets/Art/UI/DefenderHudPanel.png", 1024, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/ThirdParty/Kenney/FantasyUiBorders/HudFrameBorder.png", 64, false, TextureImporterType.Sprite, TextureImporterFormat.RGBA32),
                new("UI", "Assets/Art/UI/DefenderIconAtlas.png", 512, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconGold.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconWave.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconKeep.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconAbility.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconArcher.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconBarracks.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("UI", "Assets/Art/UI/IconMage.png", 256, false, TextureImporterType.Sprite, TextureImporterFormat.ASTC_4x4),
                new("VFX helper", "Assets/Art/White32.png", 32, false, TextureImporterType.Default, TextureImporterFormat.RGBA32)
            };

            var results = new List<TextureImportSnapshot>();
            foreach (TextureImportExpectation expectation in expectations)
                results.Add(ValidateTextureImport(expectation));

            int actorCount = results.Count(item => item.category == "Actor");
            int uiCount = results.Count(item => item.category == "UI");
            int environmentCount = results.Count(item => item.category == "Environment/Building");
            int vfxCount = results.Count(item => item.category == "VFX helper");
            string reportPath = Path.Combine(Application.dataPath, "../docs/evidence/visual-development/texture-import-profile-report-2026-08-17.json");
            WriteTextureImportProfileReport(reportPath, results);

            Debug.Log("[DC_TEXTURE_IMPORT_PROFILE_CHECKPOINT] passed " +
                      $"textures={results.Count} actor={actorCount} environmentBuilding={environmentCount} " +
                      $"ui={uiCount} vfxHelper={vfxCount} iosOverrides={results.Count} " +
                      "formats=ASTC_4x4/ASTC_6x6/ASTC_8x8/RGBA32 mipmaps=atlasOnly");
        }

        public static void ValidateActionTimingVisualDevelopmentCheckpoint()
        {
            AssertTiming("Knight slash", PresentationCue.HeroAttacked, PresentationActorKind.Knight,
                PresentationActorAnimationContract.AttackState, 0.45f, "Socket_Weapon_Tip");
            AssertTiming("Knight shield bash", PresentationCue.HeroAbility, PresentationActorKind.Knight,
                PresentationActorAnimationContract.AbilityState, 0.18f, "Socket_Ground");
            AssertTiming("Raider hit", PresentationCue.EnemyHit, PresentationActorKind.Raider,
                PresentationActorAnimationContract.HitState, 0.05f, "Socket_Chest");
            AssertTiming("Raider death", PresentationCue.EnemyDefeated, PresentationActorKind.Raider,
                PresentationActorAnimationContract.DefeatState, 0f, "Socket_Ground");
            AssertTiming("Boss attack", PresentationCue.BossAttack, PresentationActorKind.Boss,
                PresentationActorAnimationContract.BossAttackState, 0.55f, "Socket_Weapon_Tip");
            AssertTiming("Siege recoil", PresentationCue.OutpostDisrupted, PresentationActorKind.Siege,
                PresentationActorAnimationContract.HitState, 0.1f, "Socket_Muzzle");
            AssertTiming("Boss phase", PresentationCue.BossPhase, PresentationActorKind.Boss,
                PresentationActorAnimationContract.PhaseState, 0.15f, "Socket_Ground");

            Debug.Log("[DC_ACTION_TIMING_CHECKPOINT] passed " +
                      "knightSlash=Attack@0.45/WeaponTip knightShield=Ability@0.18/Ground " +
                      "raiderHit=Hit@0.05/Chest raiderDeath=Defeat@0/Ground " +
                      "bossAttack=BossAttack@0.55/WeaponTip siegeRecoil=Hit@0.1/Muzzle bossPhase=Phase@0.15/Ground");
        }

        public static void ValidateActorAnimatorOverrideCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");
            if (catalog.actorAnimator == null)
                throw new InvalidOperationException("Shared actor AnimatorController is missing.");

            int fallbackCount = 0;
            int overrideCount = 0;
            foreach (PresentationActorKind kind in Enum.GetValues(typeof(PresentationActorKind)))
            {
                if (catalog.ActorAnimator(kind) == null)
                    throw new InvalidOperationException($"{kind} actor animator resolved to null.");
                bool expectsOverride = kind is PresentationActorKind.Knight or PresentationActorKind.Guard
                    or PresentationActorKind.Raider or PresentationActorKind.Vanguard
                    or PresentationActorKind.Regenerator or PresentationActorKind.Support
                    or PresentationActorKind.Siege;
                if (expectsOverride)
                {
                    if (catalog.ActorAnimator(kind) == catalog.actorAnimator || catalog.UsesSharedActorAnimator(kind))
                        throw new InvalidOperationException($"{kind} actor animator override is missing.");
                    overrideCount++;
                }
                else
                {
                    if (catalog.ActorAnimator(kind) != catalog.actorAnimator || !catalog.UsesSharedActorAnimator(kind))
                        throw new InvalidOperationException($"{kind} actor animator fallback state is not explicit.");
                    fallbackCount++;
                }
            }

            Debug.Log("[DC_ACTOR_ANIMATOR_OVERRIDE_CHECKPOINT] passed " +
                      $"roles={fallbackCount + overrideCount} roleOverrides={overrideCount} sharedFallback={fallbackCount} " +
                      "knightOverride=true guardOverride=true raiderOverride=true vanguardOverride=true " +
                      "regeneratorOverride=true supportOverride=true siegeOverride=true");
        }

        public static void ValidateActorAnimatorPriorityCheckpoint()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            if (catalog == null)
                throw new InvalidOperationException("DefenderPresentationCatalog is missing.");
            if (catalog.actorAnimator is not AnimatorController controller)
                throw new InvalidOperationException("Shared actor animator is not an AnimatorController.");
            if (controller.layers.Length == 0)
                throw new InvalidOperationException("Shared actor animator has no layers.");

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorStateTransition defeat = AnyStateTransition(machine, PresentationActorAnimationContract.DefeatState);
            AnimatorStateTransition hit = AnyStateTransition(machine, PresentationActorAnimationContract.HitState);
            AnimatorStateTransition victory = AnyStateTransition(machine, PresentationActorAnimationContract.VictoryState);

            if (defeat == null || defeat.hasExitTime || defeat.duration > 0.001f ||
                defeat.interruptionSource != TransitionInterruptionSource.SourceThenDestination ||
                !defeat.orderedInterruption)
                throw new InvalidOperationException("Defeat transition must interrupt immediately from AnyState.");

            if (hit == null || hit.hasExitTime || hit.canTransitionToSelf || hit.duration > 0.02f)
                throw new InvalidOperationException("Hit transition must be short and avoid self-transition spam.");

            if (victory == null || victory.hasExitTime || victory.duration > 0.05f)
                throw new InvalidOperationException("Victory transition must remain short and trigger-driven.");

            AnimatorState defeatState = State(machine, PresentationActorAnimationContract.DefeatState);
            if (defeatState == null || defeatState.transitions.Length == 0 ||
                defeatState.transitions.Any(transition => !transition.hasExitTime || transition.exitTime < 1f ||
                                                          transition.duration > 0.001f))
                throw new InvalidOperationException("Defeat state must return safely after its full fallback clip.");

            Debug.Log("[DC_ACTOR_ANIMATOR_PRIORITY_CHECKPOINT] passed " +
                      "defeat=AnyStateImmediateInterrupt hit=NoSelfShort victory=ShortTrigger defeatExit=FullClip");
        }

        public static void ValidateAnimationEventRelayCheckpoint()
        {
            if (!PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.HeroAttacked) ||
                !PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.BossAttack) ||
                PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.UiClick) ||
                PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.UiError) ||
                PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.WaveStarted))
                throw new InvalidOperationException("Animation event relay cue allowlist is invalid.");

            string[] forbiddenMethods = { "TakeDamage", "DamageEnemies", "EarnGold", "SpendGold" };
            var relayMethods = typeof(PresentationAnimationEventRelay).GetMethods()
                .Where(method => method.DeclaringType == typeof(PresentationAnimationEventRelay))
                .Select(method => method.Name)
                .ToArray();
            foreach (string forbidden in forbiddenMethods)
                if (relayMethods.Contains(forbidden))
                    throw new InvalidOperationException($"Animation event relay exposes gameplay method {forbidden}.");

            var root = new GameObject("Animation Relay Checkpoint Source");
            var relayObject = new GameObject("Animation Relay Checkpoint");
            relayObject.transform.SetParent(root.transform, false);
            var relay = relayObject.AddComponent<PresentationAnimationEventRelay>();
            relay.Configure(root, PresentationActorKind.Knight);
            int emitted = 0;
            PresentationAnimationFeedback last = default;
            relay.FeedbackSignaled += feedback =>
            {
                emitted++;
                last = feedback;
            };
            relay.Impact(PresentationCue.UiClick.ToString());
            relay.Impact(PresentationCue.HeroAttacked.ToString());
            UnityEngine.Object.DestroyImmediate(root);

            if (emitted != 1 || last.cue != PresentationCue.HeroAttacked ||
                last.actor != PresentationActorKind.Knight)
                throw new InvalidOperationException("Animation event relay failed presentation-only emission check.");

            Debug.Log("[DC_ANIMATION_EVENT_RELAY_CHECKPOINT] passed " +
                      "allowed=PresentationFeedback rejected=UiClick/UiError/WaveStarted gameplayAuthority=false");
        }

        private static void AssertVfx(string label, DefenderVfxSpec spec, DefenderVfxShape shape,
            DefenderVfxMaterialRole materialRole, DefenderVfxImpactClass impactClass, bool projectile,
            float endScale, string sourceSocket, DefenderVfxPriority priority = DefenderVfxPriority.Normal,
            string concurrencyGroup = null, int maxConcurrent = int.MaxValue, string targetSocket = null)
        {
            if (spec.shape != shape || spec.materialRole != materialRole || spec.impactClass != impactClass ||
                spec.projectile != projectile || Math.Abs(spec.endScale - endScale) > 0.001f ||
                spec.sourceSocket != sourceSocket || spec.priority != priority ||
                spec.concurrencyGroup != concurrencyGroup || spec.maxConcurrent != maxConcurrent ||
                spec.targetSocket != targetSocket)
                throw new InvalidOperationException($"{label} VFX contract mismatch: " +
                                                    $"shape={spec.shape} material={spec.materialRole} " +
                                                    $"impact={spec.impactClass} projectile={spec.projectile} " +
                                                    $"endScale={spec.endScale} sourceSocket={spec.sourceSocket ?? "null"} " +
                                                    $"targetSocket={spec.targetSocket ?? "null"} " +
                                                    $"priority={spec.priority} group={spec.concurrencyGroup ?? "null"} " +
                                                    $"max={spec.maxConcurrent}");
        }

        private static void AssertAnimationCue(string label, PresentationCue cue, PresentationActorKind actor,
            string expectedTrigger)
        {
            string actual = DefenderPresentationDirector.AnimationTriggerFor(
                new PresentationSignal(cue, Vector3.zero, actor: actor));
            if (actual != expectedTrigger)
                throw new InvalidOperationException($"{label} animation contract mismatch: " +
                                                    $"expected={expectedTrigger} actual={actual ?? "null"}");
        }

        private static void AssertTiming(string label, PresentationCue cue, PresentationActorKind actor,
            string animationTrigger, float impactNormalizedTime, string impactSocket)
        {
            PresentationActionTimingSpec spec = PresentationActionTimingContract.Describe(
                new PresentationSignal(cue, Vector3.zero, actor: actor));
            if (spec.animationTrigger != animationTrigger ||
                Math.Abs(spec.impactNormalizedTime - impactNormalizedTime) > 0.001f ||
                spec.impactSocket != impactSocket ||
                spec.gameplayAuthoritative)
                throw new InvalidOperationException($"{label} action timing mismatch: " +
                                                    $"trigger={spec.animationTrigger ?? "null"} " +
                                                    $"impact={spec.impactNormalizedTime:0.###} " +
                                                    $"socket={spec.impactSocket ?? "null"} " +
                                                    $"gameplayAuthoritative={spec.gameplayAuthoritative}");
        }

        private static void AssertTodoRowStatus(string todo, int id, string status)
        {
            string rowToken = $"| VIS-{id} |";
            int rowStart = todo.IndexOf(rowToken, StringComparison.Ordinal);
            if (rowStart < 0)
                throw new InvalidOperationException($"Visual development TODO does not mention VIS-{id}.");

            int rowEnd = todo.IndexOf('\n', rowStart);
            string row = rowEnd >= 0 ? todo.Substring(rowStart, rowEnd - rowStart) : todo.Substring(rowStart);
            if (!row.Contains($"| {status} |", StringComparison.Ordinal))
                throw new InvalidOperationException($"VIS-{id} is not marked {status}.");
        }

        private static int FindQualityIndexByName(string qualityName)
        {
            string target = qualityName?.ToLowerInvariant() ?? string.Empty;
            string[] names = QualitySettings.names ?? Array.Empty<string>();
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals((names[i] ?? string.Empty).ToLowerInvariant(), target, StringComparison.Ordinal))
                    return i;
            }
            for (int i = 0; i < names.Length; i++)
            {
                if ((names[i] ?? string.Empty).ToLowerInvariant().Contains(target))
                    return i;
            }
            return -1;
        }

        private static QualityProfileSnapshot ApplyAndMeasureQuality(int qualityIndex, Camera camera)
        {
            QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
            DefenderSceneLook.Apply(camera);

            Light key = GameObject.Find("Greenwatch Moon Key")?.GetComponent<Light>();
            Light moonFill = GameObject.Find("Moon Fill")?.GetComponent<Light>();
            Light gateFill = GameObject.Find("Gate Ember Fill")?.GetComponent<Light>();
            if (key == null || moonFill == null || gateFill == null)
                throw new InvalidOperationException("Quality profile look lights are missing.");

            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .Where(light => light.name == "Greenwatch Moon Key" || light.name == "Moon Fill" ||
                                light.name == "Gate Ember Fill")
                .ToArray();

            return new QualityProfileSnapshot
            {
                qualityName = QualitySettings.names[qualityIndex],
                keyShadows = key.shadows,
                pointLightCount = lights.Count(light => light.type == LightType.Point),
                pointShadowCasters = lights.Count(light => light.type == LightType.Point &&
                                                          light.shadows != LightShadows.None),
                totalLightCount = lights.Length,
                moonFillIntensity = moonFill.intensity,
                moonFillRange = moonFill.range,
                gateFillIntensity = gateFill.intensity,
                gateFillRange = gateFill.range
            };
        }

        private static AnimatorStateTransition AnyStateTransition(AnimatorStateMachine machine, string trigger)
        {
            return machine.anyStateTransitions.FirstOrDefault(transition =>
                transition.conditions.Any(condition => condition.parameter == trigger));
        }

        private static AnimatorState State(AnimatorStateMachine machine, string stateName)
        {
            return machine.states.FirstOrDefault(child => child.state != null && child.state.name == stateName).state;
        }

        private static void CreateSocket(Transform parent, string name, Vector3 position)
        {
            var socket = new GameObject(name);
            socket.transform.SetParent(parent, false);
            socket.transform.position = position;
        }

        private static PresentationSignal StressSignal(int index, GameObject source)
        {
            Vector3 position = new(index % 12, 0f, index / 12);
            return (index % 5) switch
            {
                0 => new PresentationSignal(PresentationCue.HeroAttacked, position, source,
                    PresentationActorKind.Knight),
                1 => new PresentationSignal(PresentationCue.HeroAbility, position, source,
                    PresentationActorKind.Knight),
                2 => new PresentationSignal(PresentationCue.UnitSpawned, position, source,
                    PresentationActorKind.Raider),
                3 => new PresentationSignal(PresentationCue.EnemyHit, position, source,
                    PresentationActorKind.Raider),
                _ => new PresentationSignal(PresentationCue.EnemyDefeated, position, source,
                    PresentationActorKind.Raider)
            };
        }

        private static Button FindButton(Transform root, string name)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == name)
                    return button;
            }

            return null;
        }

        private static Image FindImage(Transform root, string name)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name == name)
                    return image;
            }

            return null;
        }

        private static Image FindChildImage(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child == null || !child.TryGetComponent(out Image image))
                return null;
            return image;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item != root && item.name == name)
                    return item;
            }

            return null;
        }

        private static bool HasDescendant(Transform root, string name)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item != root && item.name == name)
                    return true;
            }

            return false;
        }

        private static List<PresentationSignal> DenseVfxSignals(GameObject source)
        {
            var signals = new List<PresentationSignal>(108);
            for (int i = 0; i < 60; i++)
                signals.Add(StressSignal(i, source));

            PresentationSignal[] dense =
            {
                new(PresentationCue.BossPhase, Vector3.zero, source, PresentationActorKind.Boss),
                new(PresentationCue.BossAttack, Vector3.right, source, PresentationActorKind.Boss),
                new(PresentationCue.BossAttack, Vector3.right * 2f, source, PresentationActorKind.Boss),
                new(PresentationCue.OutpostAttacked, Vector3.forward, source, outpost: OutpostKind.Archer),
                new(PresentationCue.MageImpact, Vector3.forward * 2f, source, outpost: OutpostKind.Mage),
                new(PresentationCue.HealPulse, Vector3.forward * 3f, source, PresentationActorKind.Support),
                new(PresentationCue.HealPulse, Vector3.forward * 4f, source, PresentationActorKind.Regenerator),
                new(PresentationCue.SlowApplied, Vector3.forward * 5f, source, PresentationActorKind.Raider),
                new(PresentationCue.OutpostDisrupted, Vector3.forward * 6f, source, outpost: OutpostKind.Archer),
                new(PresentationCue.EnemyHit, Vector3.forward * 7f, source, PresentationActorKind.Raider),
                new(PresentationCue.EnemyDefeated, Vector3.forward * 8f, source, PresentationActorKind.Raider),
                new(PresentationCue.HeroAbility, Vector3.forward * 9f, source, PresentationActorKind.Knight)
            };
            for (int i = 0; i < 4; i++)
                signals.AddRange(dense);
            return signals;
        }

        private static float EstimatedTransparentCost(DefenderVfxSpec spec)
        {
            float shapeFill = spec.shape == DefenderVfxShape.Ring ? 0.42f : 0.68f;
            float motionFill = spec.projectile ? 0.48f : 1f;
            float priorityBoost = spec.priority == DefenderVfxPriority.Critical ? 1.2f : 1f;
            return spec.endScale * spec.endScale * shapeFill * motionFill * priorityBoost;
        }

        private static int ExpectedPlayingWithConcurrencyCaps(IEnumerable<PresentationSignal> signals)
        {
            int total = 0;
            var groupCounts = new Dictionary<string, int>();
            foreach (PresentationSignal signal in signals)
            {
                DefenderVfxSpec spec = DefenderVfxPool.Describe(signal);
                if (string.IsNullOrWhiteSpace(spec.concurrencyGroup) || spec.maxConcurrent == int.MaxValue)
                {
                    total++;
                    continue;
                }

                groupCounts.TryGetValue(spec.concurrencyGroup, out int groupCount);
                if (groupCount < spec.maxConcurrent)
                {
                    groupCounts[spec.concurrencyGroup] = groupCount + 1;
                    total++;
                }
            }
            return total;
        }

        private static VfxQualityProfileSnapshot ApplyAndMeasureVfxQuality(DefenderPresentationCatalog catalog,
            Transform parent, int qualityIndex, List<PresentationSignal> signals)
        {
            QualitySettings.SetQualityLevel(qualityIndex, applyExpensiveChanges: true);
            DefenderVfxQualityConfig config = DefenderVfxPool.QualityConfigForCurrentQuality();
            var host = new GameObject($"VFX Quality {config.qualityName}");
            host.transform.SetParent(parent, false);
            var pool = host.AddComponent<DefenderVfxPool>();
            pool.InitializeForCurrentQuality(catalog);

            long heapBefore = GC.GetTotalMemory(false);
            foreach (PresentationSignal signal in signals)
                pool.Play(signal);
            long heapAfter = GC.GetTotalMemory(false);

            int expectedPlaying = Math.Min(config.maxPlayingCount,
                ExpectedPlayingWithQualityCaps(signals, config));
            if (pool.PoolCount != config.prewarmPoolSize)
                throw new InvalidOperationException($"{config.qualityName} VFX pool did not honor prewarm size: " +
                                                    $"pool={pool.PoolCount} expected={config.prewarmPoolSize}.");
            if (pool.PlayingCount != expectedPlaying)
                throw new InvalidOperationException($"{config.qualityName} VFX playing count mismatch: " +
                                                    $"playing={pool.PlayingCount} expected={expectedPlaying}.");
            if (pool.PlayingCountForGroup("BossImpact") != config.bossImpactMaxConcurrent ||
                pool.PlayingCountForGroup("BossTelegraph") != config.bossTelegraphMaxConcurrent)
                throw new InvalidOperationException($"{config.qualityName} VFX boss caps failed: " +
                                                    $"bossImpact={pool.PlayingCountForGroup("BossImpact")} " +
                                                    $"bossTelegraph={pool.PlayingCountForGroup("BossTelegraph")}.");
            if (pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical) < config.bossTelegraphMaxConcurrent)
                throw new InvalidOperationException($"{config.qualityName} critical boss telegraph was not preserved.");

            float transparentCost = EstimatedTransparentCostWithQualityCaps(signals, config);
            if (transparentCost > config.transparentCostBudget)
                throw new InvalidOperationException($"{config.qualityName} VFX transparent budget exceeded: " +
                                                    $"cost={transparentCost:0.###} budget={config.transparentCostBudget:0.###}");

            var snapshot = new VfxQualityProfileSnapshot
            {
                qualityName = config.qualityName,
                prewarmPoolSize = config.prewarmPoolSize,
                maxPlayingCount = config.maxPlayingCount,
                poolCount = pool.PoolCount,
                playingCount = pool.PlayingCount,
                bossImpactMaxConcurrent = config.bossImpactMaxConcurrent,
                bossImpactPlaying = pool.PlayingCountForGroup("BossImpact"),
                bossTelegraphMaxConcurrent = config.bossTelegraphMaxConcurrent,
                bossTelegraphPlaying = pool.PlayingCountForGroup("BossTelegraph"),
                criticalPlaying = pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical),
                estimatedTransparentCost = transparentCost,
                transparentCostBudget = config.transparentCostBudget,
                managedHeapDeltaBytes = Math.Max(0, heapAfter - heapBefore)
            };
            UnityEngine.Object.DestroyImmediate(host);
            return snapshot;
        }

        private static int ExpectedPlayingWithQualityCaps(IEnumerable<PresentationSignal> signals,
            DefenderVfxQualityConfig config)
        {
            int total = 0;
            var groupCounts = new Dictionary<string, int>();
            foreach (PresentationSignal signal in signals)
            {
                DefenderVfxSpec spec = DefenderVfxPool.Describe(signal);
                int maxConcurrent = QualityMaxConcurrent(spec, config);
                if (string.IsNullOrWhiteSpace(spec.concurrencyGroup) || maxConcurrent == int.MaxValue)
                {
                    total++;
                    continue;
                }

                groupCounts.TryGetValue(spec.concurrencyGroup, out int groupCount);
                if (groupCount < maxConcurrent)
                {
                    groupCounts[spec.concurrencyGroup] = groupCount + 1;
                    total++;
                }
            }

            return total;
        }

        private static float EstimatedTransparentCostWithQualityCaps(IEnumerable<PresentationSignal> signals,
            DefenderVfxQualityConfig config)
        {
            int accepted = 0;
            float cost = 0f;
            var groupCounts = new Dictionary<string, int>();
            foreach (PresentationSignal signal in signals)
            {
                DefenderVfxSpec spec = DefenderVfxPool.Describe(signal);
                int maxConcurrent = QualityMaxConcurrent(spec, config);
                bool acceptedByGroup = true;
                if (!string.IsNullOrWhiteSpace(spec.concurrencyGroup) && maxConcurrent != int.MaxValue)
                {
                    groupCounts.TryGetValue(spec.concurrencyGroup, out int groupCount);
                    acceptedByGroup = groupCount < maxConcurrent;
                    if (acceptedByGroup)
                        groupCounts[spec.concurrencyGroup] = groupCount + 1;
                }

                if (!acceptedByGroup || accepted >= config.maxPlayingCount)
                    continue;
                accepted++;
                cost += EstimatedTransparentCost(spec);
            }

            return cost;
        }

        private static int QualityMaxConcurrent(DefenderVfxSpec spec, DefenderVfxQualityConfig config)
        {
            if (spec.concurrencyGroup == "BossImpact") return config.bossImpactMaxConcurrent;
            if (spec.concurrencyGroup == "BossTelegraph") return config.bossTelegraphMaxConcurrent;
            return spec.maxConcurrent;
        }

        private static void WriteVfxProfilerOverdrawReport(int signals, int pool, int playing, long managedDelta,
            float transparentCost, float transparentBudget)
        {
            string reportPath = Path.Combine(Application.dataPath, "../docs/evidence/visual-development/vfx-profiler-overdraw-report-2026-08-17.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            string cost = transparentCost.ToString("0.###", CultureInfo.InvariantCulture);
            string budget = transparentBudget.ToString("0.###", CultureInfo.InvariantCulture);
            File.WriteAllText(reportPath,
                "{\n" +
                "  \"checkpoint\": \"VIS-515\",\n" +
                "  \"scenario\": \"60 actor VFX plus dense boss/status/projectile burst\",\n" +
                $"  \"signals\": {signals},\n" +
                $"  \"poolCount\": {pool},\n" +
                $"  \"playingCount\": {playing},\n" +
                "  \"poolGrowth\": 0,\n" +
                $"  \"managedHeapDeltaBytes\": {managedDelta},\n" +
                $"  \"estimatedTransparentCost\": {cost},\n" +
                $"  \"estimatedTransparentBudget\": {budget},\n" +
                "  \"criticalBossTelegraphPreserved\": true,\n" +
                "  \"notes\": \"Batch-mode proxy report; final device Profiler/Frame Debugger capture remains under VIS-006/VIS-807.\"\n" +
                "}\n");
        }

        private static void WriteVfxQualityProfileReport(string reportPath, params VfxQualityProfileSnapshot[] snapshots)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"checkpoint\": \"VIS-806\",");
            builder.AppendLine("  \"scenario\": \"Dense boss/status/projectile VFX burst across Low, Medium and High quality profiles\",");
            builder.AppendLine("  \"profiles\": [");
            for (int i = 0; i < snapshots.Length; i++)
                AppendVfxQualityProfile(builder, snapshots[i], i < snapshots.Length - 1);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"poolGrowth\": 0,");
            builder.AppendLine("  \"criticalBossTelegraphPreserved\": true,");
            builder.AppendLine("  \"notes\": \"Batch-mode quality config proof; final device Profiler and GC spike capture remain under VIS-807.\"");
            builder.AppendLine("}");
            File.WriteAllText(reportPath, builder.ToString());
        }

        private static void AppendVfxQualityProfile(StringBuilder builder, VfxQualityProfileSnapshot snapshot,
            bool trailingComma)
        {
            builder.AppendLine("    {");
            builder.AppendLine($"      \"qualityName\": \"{snapshot.qualityName}\",");
            builder.AppendLine($"      \"prewarmPoolSize\": {snapshot.prewarmPoolSize},");
            builder.AppendLine($"      \"maxPlayingCount\": {snapshot.maxPlayingCount},");
            builder.AppendLine($"      \"poolCount\": {snapshot.poolCount},");
            builder.AppendLine($"      \"playingCount\": {snapshot.playingCount},");
            builder.AppendLine($"      \"bossImpactMaxConcurrent\": {snapshot.bossImpactMaxConcurrent},");
            builder.AppendLine($"      \"bossImpactPlaying\": {snapshot.bossImpactPlaying},");
            builder.AppendLine($"      \"bossTelegraphMaxConcurrent\": {snapshot.bossTelegraphMaxConcurrent},");
            builder.AppendLine($"      \"bossTelegraphPlaying\": {snapshot.bossTelegraphPlaying},");
            builder.AppendLine($"      \"criticalPlaying\": {snapshot.criticalPlaying},");
            builder.AppendLine($"      \"estimatedTransparentCost\": {snapshot.estimatedTransparentCost.ToString("0.###", CultureInfo.InvariantCulture)},");
            builder.AppendLine($"      \"transparentCostBudget\": {snapshot.transparentCostBudget.ToString("0.###", CultureInfo.InvariantCulture)},");
            builder.AppendLine($"      \"managedHeapDeltaBytes\": {snapshot.managedHeapDeltaBytes}");
            builder.AppendLine(trailingComma ? "    }," : "    }");
        }

        private static TextureImportSnapshot ValidateTextureImport(TextureImportExpectation expectation)
        {
            if (AssetImporter.GetAtPath(expectation.path) is not TextureImporter importer)
                throw new InvalidOperationException($"Texture importer is missing: {expectation.path}");
            TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings("iPhone");
            if (importer.textureType != expectation.textureType)
                throw new InvalidOperationException($"{expectation.path} texture type mismatch: " +
                                                    $"{importer.textureType} expected {expectation.textureType}.");
            if (importer.mipmapEnabled != expectation.mipmapEnabled)
                throw new InvalidOperationException($"{expectation.path} mipmap mismatch: " +
                                                    $"{importer.mipmapEnabled} expected {expectation.mipmapEnabled}.");
            if (importer.maxTextureSize != expectation.maxTextureSize)
                throw new InvalidOperationException($"{expectation.path} max texture size mismatch: " +
                                                    $"{importer.maxTextureSize} expected {expectation.maxTextureSize}.");
            if (!ios.overridden || ios.maxTextureSize != expectation.maxTextureSize || ios.format != expectation.iosFormat)
                throw new InvalidOperationException($"{expectation.path} iOS override mismatch: " +
                                                    $"overridden={ios.overridden} max={ios.maxTextureSize} format={ios.format} " +
                                                    $"expectedMax={expectation.maxTextureSize} expectedFormat={expectation.iosFormat}.");

            return new TextureImportSnapshot
            {
                category = expectation.category,
                path = expectation.path,
                maxTextureSize = importer.maxTextureSize,
                mipmapEnabled = importer.mipmapEnabled,
                textureType = importer.textureType.ToString(),
                iosFormat = ios.format.ToString(),
                iosMaxTextureSize = ios.maxTextureSize
            };
        }

        private static void WriteTextureImportProfileReport(string reportPath, IReadOnlyList<TextureImportSnapshot> snapshots)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"checkpoint\": \"VIS-804\",");
            builder.AppendLine("  \"scenario\": \"Mobile texture import profile for actor, building/environment, UI and VFX helper textures\",");
            builder.AppendLine("  \"targetPlatform\": \"iPhone\",");
            builder.AppendLine("  \"textures\": [");
            for (int i = 0; i < snapshots.Count; i++)
                AppendTextureImportSnapshot(builder, snapshots[i], i < snapshots.Count - 1);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"vfxTextureStatus\": \"Runtime VFX use generated mesh/material colors plus White32 helper; no particle flipbook atlas is present.\",");
            builder.AppendLine("  \"deviceReviewStatus\": \"Open: gameplay zoom acceptance and device memory delta remain under VIS-807/VIS-812.\"");
            builder.AppendLine("}");
            File.WriteAllText(reportPath, builder.ToString());
        }

        private static void AppendTextureImportSnapshot(StringBuilder builder, TextureImportSnapshot snapshot,
            bool trailingComma)
        {
            builder.AppendLine("    {");
            builder.AppendLine($"      \"category\": \"{snapshot.category}\",");
            builder.AppendLine($"      \"path\": \"{snapshot.path}\",");
            builder.AppendLine($"      \"textureType\": \"{snapshot.textureType}\",");
            builder.AppendLine($"      \"maxTextureSize\": {snapshot.maxTextureSize},");
            builder.AppendLine($"      \"mipmapEnabled\": {snapshot.mipmapEnabled.ToString().ToLowerInvariant()},");
            builder.AppendLine($"      \"iosFormat\": \"{snapshot.iosFormat}\",");
            builder.AppendLine($"      \"iosMaxTextureSize\": {snapshot.iosMaxTextureSize}");
            builder.AppendLine(trailingComma ? "    }," : "    }");
        }

        private static void WriteEnvironmentBudgetProxyReport(string reportPath, int rendererCount,
            int meshFilterCount, int triangleCount, int materialSlots, int uniqueMaterials, int shadowCasters,
            int receiveShadowRenderers, int colliderCount, int transparentLikeMaterials, int triangleSoftBudget,
            int rendererSoftBudget, int uniqueMaterialSoftBudget, int materialSlotSoftBudget)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"checkpoint\": \"VIS-415\",");
            builder.AppendLine("  \"scenario\": \"Production presentation scenic environment dressing budget proxy\",");
            builder.AppendLine($"  \"rendererCount\": {rendererCount},");
            builder.AppendLine($"  \"rendererSoftBudget\": {rendererSoftBudget},");
            builder.AppendLine($"  \"meshFilterCount\": {meshFilterCount},");
            builder.AppendLine($"  \"triangleCount\": {triangleCount},");
            builder.AppendLine($"  \"triangleSoftBudget\": {triangleSoftBudget},");
            builder.AppendLine($"  \"materialSlots\": {materialSlots},");
            builder.AppendLine($"  \"materialSlotSoftBudget\": {materialSlotSoftBudget},");
            builder.AppendLine($"  \"uniqueMaterials\": {uniqueMaterials},");
            builder.AppendLine($"  \"uniqueMaterialSoftBudget\": {uniqueMaterialSoftBudget},");
            builder.AppendLine($"  \"transparentLikeMaterials\": {transparentLikeMaterials},");
            builder.AppendLine($"  \"shadowCasters\": {shadowCasters},");
            builder.AppendLine($"  \"receiveShadowRenderers\": {receiveShadowRenderers},");
            builder.AppendLine($"  \"colliderCount\": {colliderCount},");
            builder.AppendLine("  \"passesBatchProxyBudget\": true,");
            builder.AppendLine("  \"deviceProfilerFrameDebuggerStatus\": \"Open: final iPhone-class Profiler and Frame Debugger capture still required for VIS-415 final acceptance.\",");
            builder.AppendLine("  \"notes\": \"Batch-mode scene proxy; validates runtime object counts, shadow/collider safety, mesh triangles and material pressure before device profiling.\"");
            builder.AppendLine("}");
            File.WriteAllText(reportPath, builder.ToString());
        }

        private static void WriteQualityProfileMatrixReport(string reportPath,
            QualityProfileSnapshot low, QualityProfileSnapshot medium, QualityProfileSnapshot high)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"checkpoint\": \"VIS-801/VIS-802/VIS-803\",");
            builder.AppendLine("  \"scenario\": \"Low/Medium/High runtime lighting and URP shadow matrix\",");
            builder.AppendLine("  \"urpAdditionalLightShadowsSupported\": false,");
            builder.AppendLine("  \"urpAdditionalLightsPerObjectLimit\": 4,");
            builder.AppendLine("  \"profiles\": [");
            AppendQualityProfile(builder, low, trailingComma: true);
            AppendQualityProfile(builder, medium, trailingComma: true);
            AppendQualityProfile(builder, high, trailingComma: false);
            builder.AppendLine("  ],");
            builder.AppendLine("  \"passesBatchProfileMatrix\": true,");
            builder.AppendLine("  \"deviceProfilerStatus\": \"Open: final device profile matrix and FPS capture remain under VIS-807.\",");
            builder.AppendLine("  \"notes\": \"Batch-mode profile matrix validates runtime light counts, shadow rules and Low fill attenuation before final device profiling.\"");
            builder.AppendLine("}");
            File.WriteAllText(reportPath, builder.ToString());
        }

        private static void AppendQualityProfile(StringBuilder builder, QualityProfileSnapshot snapshot,
            bool trailingComma)
        {
            builder.AppendLine("    {");
            builder.AppendLine($"      \"qualityName\": \"{snapshot.qualityName}\",");
            builder.AppendLine($"      \"keyShadows\": \"{snapshot.keyShadows}\",");
            builder.AppendLine($"      \"pointLightCount\": {snapshot.pointLightCount},");
            builder.AppendLine($"      \"pointShadowCasters\": {snapshot.pointShadowCasters},");
            builder.AppendLine($"      \"totalLightCount\": {snapshot.totalLightCount},");
            builder.AppendLine($"      \"moonFillIntensity\": {snapshot.moonFillIntensity.ToString("0.###", CultureInfo.InvariantCulture)},");
            builder.AppendLine($"      \"moonFillRange\": {snapshot.moonFillRange.ToString("0.###", CultureInfo.InvariantCulture)},");
            builder.AppendLine($"      \"gateFillIntensity\": {snapshot.gateFillIntensity.ToString("0.###", CultureInfo.InvariantCulture)},");
            builder.AppendLine($"      \"gateFillRange\": {snapshot.gateFillRange.ToString("0.###", CultureInfo.InvariantCulture)}");
            builder.AppendLine(trailingComma ? "    }," : "    }");
        }

        private struct QualityProfileSnapshot
        {
            public string qualityName;
            public LightShadows keyShadows;
            public int pointLightCount;
            public int pointShadowCasters;
            public int totalLightCount;
            public float moonFillIntensity;
            public float moonFillRange;
            public float gateFillIntensity;
            public float gateFillRange;
        }

        private struct VfxQualityProfileSnapshot
        {
            public string qualityName;
            public int prewarmPoolSize;
            public int maxPlayingCount;
            public int poolCount;
            public int playingCount;
            public int bossImpactMaxConcurrent;
            public int bossImpactPlaying;
            public int bossTelegraphMaxConcurrent;
            public int bossTelegraphPlaying;
            public int criticalPlaying;
            public float estimatedTransparentCost;
            public float transparentCostBudget;
            public long managedHeapDeltaBytes;
        }

        private readonly struct TextureImportExpectation
        {
            public readonly string category;
            public readonly string path;
            public readonly int maxTextureSize;
            public readonly bool mipmapEnabled;
            public readonly TextureImporterType textureType;
            public readonly TextureImporterFormat iosFormat;

            public TextureImportExpectation(string category, string path, int maxTextureSize, bool mipmapEnabled,
                TextureImporterType textureType, TextureImporterFormat iosFormat)
            {
                this.category = category;
                this.path = path;
                this.maxTextureSize = maxTextureSize;
                this.mipmapEnabled = mipmapEnabled;
                this.textureType = textureType;
                this.iosFormat = iosFormat;
            }
        }

        private struct TextureImportSnapshot
        {
            public string category;
            public string path;
            public int maxTextureSize;
            public bool mipmapEnabled;
            public string textureType;
            public string iosFormat;
            public int iosMaxTextureSize;
        }
    }
}
#endif
