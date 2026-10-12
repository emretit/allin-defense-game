using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;

namespace DefenderChronicles.Tests
{
    public sealed class CoreRulesTests
    {
        [TestCase(100, 25, 75)]
        [TestCase(10, 50, 0)]
        [TestCase(10, -2, 10)]
        public void DamageNeverCreatesNegativeHealth(int health, int damage, int expected)
        {
            Assert.That(CombatMath.ApplyDamage(health, damage), Is.EqualTo(expected));
        }

        [Test]
        public void MobileRuntimeSelectsMediumQualityByName()
        {
            string[] names = { "Very Low", "Low", "Medium", "High" };

            Assert.That(MobileRuntimeSettings.ResolveQualityIndex(names, "Medium"), Is.EqualTo(2));
        }

        [Test]
        public void MobileRuntimeQualitySelectionIsCaseInsensitive()
        {
            string[] names = { "Low", "MEDIUM", "High" };

            Assert.That(MobileRuntimeSettings.ResolveQualityIndex(names, "medium"), Is.EqualTo(1));
        }

        [Test]
        public void MobileRuntimeFallsBackToMiddleQualityWhenPreferredProfileIsMissing()
        {
            string[] names = { "Low", "Balanced", "High" };

            Assert.That(MobileRuntimeSettings.ResolveQualityIndex(names, "Medium"), Is.EqualTo(1));
            Assert.That(MobileRuntimeSettings.ResolveQualityIndex(System.Array.Empty<string>(), "Medium"), Is.EqualTo(-1));
        }

        [Test]
        public void SelectableLayerIsReservedForWorldInteraction()
        {
            Assert.That(LayerMask.NameToLayer("Selectable"), Is.EqualTo(10));
            Assert.That(LayerMask.GetMask("Selectable"), Is.EqualTo(1 << 10));
        }

#if UNITY_EDITOR
        [TestCase("Assets/Settings/UniversalRP.asset")]
        [TestCase("Assets/Settings/UniversalRPCustomAxisSort.asset")]
        public void MobileUrpAssetsDoNotRequestUnusedDepthTexture(string assetPath)
        {
            Object pipeline = AssetDatabase.LoadMainAssetAtPath(assetPath);
            Assert.That(pipeline, Is.Not.Null);
            var serialized = new SerializedObject(pipeline);
            Assert.That(serialized.FindProperty("m_RequireDepthTexture").boolValue, Is.False);
        }
#endif

        [Test]
        public void EconomyRefundIsSixtyPercent()
        {
            Assert.That(CombatMath.Refund(175), Is.EqualTo(105));
        }

        [Test]
        public void RouteProgressUsesXzSegmentProjection()
        {
            float progress = CombatMath.RouteProgress(Vector3.zero, new Vector3(10f, 0f, 0f),
                new Vector3(4f, 9f, 2f));
            Assert.That(progress, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void CooldownStopsAtZero()
        {
            Assert.That(CombatMath.TickCooldown(0.25f, 1f), Is.Zero);
        }

        [Test]
        public void GameSpeedLabelsMatchRealTimeScales()
        {
            Assert.That(GameSessionController.SpeedForStep(0), Is.EqualTo(1f));
            Assert.That(GameSessionController.SpeedForStep(1), Is.EqualTo(2f));
            Assert.That(GameSessionController.SpeedForStep(2), Is.EqualTo(4f));
            Assert.That(GameSessionController.SpeedForStep(3), Is.EqualTo(8f));
            Assert.That(GameSessionController.SpeedLabelForScale(1f), Is.EqualTo("1x"));
            Assert.That(GameSessionController.SpeedLabelForScale(8f), Is.EqualTo("8x"));
            Assert.That(GameSessionController.GameplayDelay(2f), Is.EqualTo(2f));
        }

        [Test]
        public void CampaignContainsFiveProgressiveRegionsAndFiftyUniqueLevels()
        {
            Assert.That(CampaignCatalog.Regions.Count, Is.EqualTo(5));
            Assert.That(CampaignCatalog.Regions.SelectMany(region => region.levels).Count(), Is.EqualTo(50));
            Assert.That(CampaignCatalog.Regions.SelectMany(region => region.levels).Select(level => level.id).Distinct().Count(),
                Is.EqualTo(50));

            for (int regionIndex = 0; regionIndex < CampaignCatalog.RegionCount; regionIndex++)
            {
                CampaignRegionDefinition region = CampaignCatalog.Regions[regionIndex];
                Assert.That(region.levels, Has.Count.EqualTo(10));
                Assert.That(region.levels[^1].isBoss, Is.True);
                Assert.That(region.levels[^1].difficulty, Is.GreaterThan(region.levels[^2].difficulty));
            }

            Assert.That(CampaignCatalog.Level(50).difficulty, Is.GreaterThan(CampaignCatalog.Level(10).difficulty));
            Assert.That(CampaignCatalog.Level(50).targetMinutes, Is.EqualTo(10f));
        }

        [Test]
        public void CampaignProgressUnlocksOneLevelAndKeepsBestResult()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dc-campaign-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new CampaignProgressStore(path);
                store.SignIn(PlayerAuthProvider.Guest, "test-guardian", "Test Muhafızı");
                store.CompleteLevel(1, 2, 8);
                store.CompleteLevel(1, 1, 4);

                var reloaded = new CampaignProgressStore(path);
                Assert.That(reloaded.Data.unlockedLevel, Is.EqualTo(2));
                Assert.That(reloaded.Data.authProvider, Is.EqualTo(PlayerAuthProvider.Guest));
                Assert.That(reloaded.ProgressFor(1).stars, Is.EqualTo(2));
                Assert.That(reloaded.ProgressFor(1).bestLives, Is.EqualTo(8));
                Assert.That(reloaded.Data.valor, Is.EqualTo(CampaignCatalog.Level(1).valorReward + 10));
                Assert.That(reloaded.Data.heroXp, Is.EqualTo(CampaignCatalog.Level(1).heroXpReward));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void CampaignMergeKeepsBestLocalAndCloudProgress()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dc-merge-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new CampaignProgressStore(path);
                store.CompleteLevel(1, 3, 18);
                var cloud = new CampaignSaveData
                {
                    unlockedLevel = 4,
                    lastSelectedLevel = 3,
                    valor = 120,
                    heroXp = 500,
                    levels = new List<CampaignLevelProgress>
                    {
                        new() { level = 1, stars = 1, bestLives = 5 },
                        new() { level = 2, stars = 2, bestLives = 10 },
                        new() { level = 3, stars = 1, bestLives = 2 }
                    }
                };

                store.Merge(cloud);
                Assert.That(store.Data.unlockedLevel, Is.EqualTo(4));
                Assert.That(store.Data.lastSelectedLevel, Is.EqualTo(3));
                Assert.That(store.ProgressFor(1).stars, Is.EqualTo(3));
                Assert.That(store.ProgressFor(1).bestLives, Is.EqualTo(18));
                Assert.That(store.ProgressFor(2).stars, Is.EqualTo(2));
                Assert.That(store.Data.TotalStars, Is.EqualTo(6));
                Assert.That(store.Data.valor, Is.EqualTo(120));
                Assert.That(store.Data.heroXp, Is.EqualTo(500));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void CampaignSaveNormalizationRepairsInvalidAndDuplicateProgress()
        {
            var data = new CampaignSaveData
            {
                version = -4,
                unlockedLevel = 500,
                lastSelectedLevel = 200,
                valor = -40,
                heroXp = -20,
                levels = new List<CampaignLevelProgress>
                {
                    new() { level = 2, stars = 1, bestLives = -5 },
                    new() { level = 2, stars = 8, bestLives = 11 },
                    new() { level = 99, stars = 3, bestLives = 20 }
                }
            };

            CampaignProgressStore.Normalize(data);
            Assert.That(data.version, Is.EqualTo(CampaignProgressStore.CurrentVersion));
            Assert.That(data.unlockedLevel, Is.EqualTo(CampaignCatalog.LevelCount));
            Assert.That(data.lastSelectedLevel, Is.EqualTo(CampaignCatalog.LevelCount));
            Assert.That(data.levels, Has.Count.EqualTo(1));
            Assert.That(data.levels[0].stars, Is.EqualTo(3));
            Assert.That(data.levels[0].bestLives, Is.EqualTo(11));
            Assert.That(data.valor, Is.Zero);
            Assert.That(data.heroXp, Is.Zero);
        }

        [Test]
        public void CampaignRewardsFirstCompletionAndOnlyImprovedStarsAfterward()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dc-reward-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new CampaignProgressStore(path);
                CampaignLevelDefinition level = CampaignCatalog.Level(5);

                CampaignCompletionReward first = store.CompleteLevel(5, 1, 4);
                CampaignCompletionReward replay = store.CompleteLevel(5, 1, 6);
                CampaignCompletionReward improvement = store.CompleteLevel(5, 3, 16);

                Assert.That(first.Valor, Is.EqualTo(level.valorReward + 5));
                Assert.That(first.HeroXp, Is.EqualTo(level.heroXpReward));
                Assert.That(replay.Valor, Is.Zero);
                Assert.That(replay.HeroXp, Is.Zero);
                Assert.That(improvement.Valor, Is.EqualTo(10));
                Assert.That(improvement.HeroXp, Is.Zero);
                Assert.That(store.Data.valor, Is.EqualTo(level.valorReward + 15));
                Assert.That(store.Data.heroXp, Is.EqualTo(level.heroXpReward));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void CampaignCompletionClaimsPersistInOrderAcrossRestart()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dc-claims-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new CampaignProgressStore(path);
                store.CompleteLevel(1, 2, 8);
                store.CompleteLevel(2, 1, 7);

                var reloaded = new CampaignProgressStore(path);
                Assert.That(reloaded.PendingClaimCount, Is.EqualTo(2));
                Assert.That(reloaded.PendingClaims().Select(item => item.sequence), Is.EqualTo(new long[] { 1, 2 }));
                Assert.That(reloaded.PendingClaims().Select(item => item.levelId),
                    Is.EqualTo(new[] { "level-01", "level-02" }));
                Assert.That(reloaded.PendingClaims().All(item =>
                    item.rulesVersion == CampaignRulesVersion.Current), Is.True);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void CampaignServerSyncRemovesProcessedClaimsAndUsesCanonicalReward()
        {
            string path = Path.Combine(Path.GetTempPath(), $"dc-sync-{System.Guid.NewGuid():N}.json");
            try
            {
                var store = new CampaignProgressStore(path);
                store.CompleteLevel(1, 3, 9);
                CampaignCompletionClaim claim = store.PendingClaims().Single();
                CampaignLevelDefinition definition = CampaignCatalog.Level(1);
                var result = new CampaignSyncResult
                {
                    state = new CampaignServerState
                    {
                        revision = 7,
                        unlockedLevel = 2,
                        valor = definition.valorReward + 15,
                        heroXp = definition.heroXpReward,
                        levels = new List<CampaignLevelProgress>
                        {
                            new() { level = 1, stars = 3, bestLives = 9 }
                        }
                    },
                    decisions = new List<CampaignClaimDecision>
                    {
                        new() { claimId = claim.claimId, accepted = true, reason = "accepted" }
                    }
                };

                store.ApplyServerSync(result);

                Assert.That(store.PendingClaimCount, Is.Zero);
                Assert.That(store.Data.serverRevision, Is.EqualTo(7));
                Assert.That(store.Data.valor, Is.EqualTo(definition.valorReward + 15));
                Assert.That(store.ProgressFor(1).stars, Is.EqualTo(3));
                Assert.That(store.SyncStatus, Is.EqualTo(CampaignSyncStatus.Current));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void FirstRegionHasTenPlayableDataDrivenCombatDefinitions()
        {
            for (int levelNumber = 1; levelNumber <= 10; levelNumber++)
            {
                RuntimeCampaignLevel runtime = CampaignLevelFactory.Create(levelNumber);
                Assert.That(runtime.campaign.number, Is.EqualTo(levelNumber));
                Assert.That(runtime.path.Length, Is.GreaterThanOrEqualTo(8));
                Assert.That(runtime.outpostSlots, Has.Length.EqualTo(8));
                Assert.That(runtime.waves, Has.Length.EqualTo(runtime.campaign.waveCount));
                Assert.That(runtime.waves.All(wave => wave.groups != null && wave.groups.Length > 0), Is.True);
                Assert.That(runtime.waves[^1].bossWave, Is.EqualTo(levelNumber == 10));
            }

            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(1), EnemyArchetype.Vanguard), Is.False);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(4), EnemyArchetype.Vanguard), Is.True);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(3), EnemyArchetype.Regenerator), Is.False);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(6), EnemyArchetype.Regenerator), Is.True);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(7), EnemyArchetype.Support), Is.True);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(8), EnemyArchetype.Siege), Is.True);
            Assert.That(ContainsEnemy(CampaignLevelFactory.Create(10), EnemyArchetype.Boss), Is.True);
        }

        private static bool ContainsEnemy(RuntimeCampaignLevel level, EnemyArchetype archetype) =>
            level.waves.SelectMany(wave => wave.groups).Any(group => group.enemy == archetype);

        [Test]
        public void AllFiftyCampaignLevelsHaveBoundedPlayableContentAndRegionalBosses()
        {
            for (int levelNumber = 1; levelNumber <= CampaignCatalog.LevelCount; levelNumber++)
            {
                RuntimeCampaignLevel runtime = CampaignLevelFactory.Create(levelNumber);
                Assert.That(runtime.waves, Has.Length.EqualTo(runtime.campaign.waveCount));
                Assert.That(runtime.waves.Length, Is.InRange(3, 12));
                Assert.That(runtime.outpostSlots, Has.Length.EqualTo(8));
                Assert.That(runtime.waves[^1].bossWave, Is.EqualTo(levelNumber % 10 == 0));
                Assert.That(runtime.waves.SelectMany(wave => wave.groups).All(group => group.count <= (levelNumber <= 10 ? 110 : 50)), Is.True,
                    $"Level {levelNumber} exceeded the authored spawn-group budget.");
                Assert.That(runtime.waves.SelectMany(wave => wave.groups).Select(group => group.enemy).Distinct().Count(),
                    Is.GreaterThanOrEqualTo(levelNumber <= 3 ? 1 : 2));
            }
        }

        [Test]
        public void CampaignMapsUseStableIdsDistinctLayoutsAndProgressiveEntrances()
        {
            var signatures = new HashSet<string>();
            for (int levelNumber = 1; levelNumber <= CampaignCatalog.LevelCount; levelNumber++)
            {
                RuntimeCampaignLevel runtime = CampaignLevelFactory.Create(levelNumber);
                Assert.That(runtime.campaign.mapId, Is.EqualTo($"map-{levelNumber:00}"));
                Assert.That(runtime.routes, Has.Length.EqualTo(runtime.entranceCount));
                Assert.That(runtime.entranceCount, Is.EqualTo(levelNumber >= 43 ? 3 : levelNumber >= 14 ? 2 : 1));
                Assert.That(runtime.routes.All(route => route != null && route.Length >= 2), Is.True);
                Assert.That(runtime.routes.All(route => route[^1] == runtime.path[^1]), Is.True);
                string signature = string.Join("|", runtime.path.Select(point =>
                    $"{point.x:0.00},{point.z:0.00}"));
                signatures.Add(signature);
            }

            Assert.That(signatures, Has.Count.EqualTo(CampaignCatalog.LevelCount));
            RuntimeCampaignLevel reference = CampaignLevelFactory.Create(10);
            Assert.That(reference.path[0], Is.EqualTo(new Vector3(-2f, 0f, 2f)));
            Assert.That(reference.path[^1], Is.EqualTo(new Vector3(24f, 0f, 9f)));
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(14)]
        [TestCase(24)]
        [TestCase(43)]
        public void CampaignDecorationLeavesSlotFootprintsClear(int levelNumber)
        {
            RuntimeCampaignLevel level = CampaignLevelFactory.Create(levelNumber);
            DefenderPresentationCatalog catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            GameObject environment = Object.Instantiate(catalog.environmentPrefab);
            try
            {
                environment.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
                bool[] enabledBefore = renderers.Select(renderer => renderer.enabled).ToArray();
                DefenderBattlefieldDressing.HideObstructingDecoration(environment.transform, level.outpostSlots, level.routes);
                int visible = 0;
                for (int index = 0; index < renderers.Length; index++)
                {
                    Renderer renderer = renderers[index];
                    if (!DefenderBattlefieldDressing.IsDecoration(renderer.transform, environment.transform))
                    {
                        Assert.That(renderer.enabled, Is.EqualTo(enabledBefore[index]), "Terrain and backdrop must be preserved.");
                        continue;
                    }
                    if (!renderer.enabled) continue;
                    visible++;
                    foreach (Vector3 slot in level.outpostSlots)
                    {
                        Vector3 nearest = renderer.bounds.ClosestPoint(slot);
                        float distance = Vector2.Distance(new Vector2(slot.x, slot.z), new Vector2(nearest.x, nearest.z));
                        Assert.That(distance, Is.GreaterThan(1.1f), $"Level {levelNumber}: {renderer.name} covers a slot footprint.");
                    }
                    Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(renderer.bounds, null, level.routes),
                        Is.False, $"Level {levelNumber}: {renderer.name} covers a road corridor.");
                }
                Assert.That(visible, Is.GreaterThan(0), "Decoration outside gameplay corridors should remain visible.");
            }
            finally { Object.DestroyImmediate(environment); }
        }

        [Test]
        public void DecorationClearanceUsesFootprintsAndRouteCrossings()
        {
            var tree = new Bounds(new Vector3(3f, 8f, 0f), new Vector3(4f, 6f, 2f));
            Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(tree,
                new[] { Vector3.zero }, null), Is.True, "Overhanging foliage matters even if the pivot is distant.");
            Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(tree,
                new[] { new Vector3(-0.2f, 0f, 0f) }, null), Is.False);
            var routes = new[] { new[] { new Vector3(-10f, 0f, 0f), new Vector3(10f, 0f, 0f) } };
            Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(tree, null, routes), Is.True);
            var distantRock = new Bounds(new Vector3(0f, 2f, 4f), Vector3.one);
            Assert.That(DefenderBattlefieldDressing.DecorationOverlapsGameplay(distantRock, null, routes), Is.False);
        }

        [TestCase(1f, 0.88f, true)]
        [TestCase(1f, -0.88f, false)]
        [TestCase(3f, 0.88f, false)]
        public void BranchJunctionOpensOnlyItsIncomingPrimaryBorder(float x, float z, bool omitted)
        {
            var primary = new[] { Vector3.zero, new Vector3(6f, 0f, 0f) };
            var branch = new[] { new Vector3(1f, 0f, 5f), new Vector3(1f, 0f, 0f), primary[1] };
            Assert.That(DefenderBattlefieldDressing.ShouldOmitRoadBorder(new Vector3(x, 0f, z), 0,
                new[] { primary, branch }), Is.EqualTo(omitted));
            Assert.That(DefenderBattlefieldDressing.ShouldOmitRoadBorder(new Vector3(1.88f, 0f, 0.5f), 1,
                new[] { primary, branch }), Is.True, "Branch borders must stop at the primary pavement.");
            Assert.That(DefenderBattlefieldDressing.ShouldOmitRoadBorder(new Vector3(1.88f, 0f, 2f), 1,
                new[] { primary, branch }), Is.False);
        }

        [Test]
        public void CampaignOutpostSlotsPreserveGateAndKeepClearance()
        {
            for (int levelNumber = 1; levelNumber <= CampaignCatalog.LevelCount; levelNumber++)
            {
                RuntimeCampaignLevel level = CampaignLevelFactory.Create(levelNumber);
                foreach (Vector3 slot in level.outpostSlots)
                foreach (Vector3[] route in level.routes)
                {
                    Assert.That(Vector2.Distance(new Vector2(slot.x, slot.z), new Vector2(route[0].x, route[0].z)),
                        Is.GreaterThanOrEqualTo(2f), $"Level {levelNumber}: slot is too close to an entrance.");
                    Assert.That(Vector2.Distance(new Vector2(slot.x, slot.z), new Vector2(route[^1].x, route[^1].z)),
                        Is.GreaterThanOrEqualTo(3f), $"Level {levelNumber}: slot is too close to the keep.");
                }
            }
        }

        [Test]
        public void CampaignOutpostSlotsStayClearOfEveryRoute()
        {
            for (int levelNumber = 1; levelNumber <= CampaignCatalog.LevelCount; levelNumber++)
            {
                RuntimeCampaignLevel runtime = CampaignLevelFactory.Create(levelNumber);
                for (int slotIndex = 0; slotIndex < runtime.outpostSlots.Length; slotIndex++)
                {
                    float clearance = runtime.routes
                        .SelectMany(route => route.Zip(route.Skip(1), (from, to) => new { from, to }))
                        .Min(segment => DistanceToSegment2D(runtime.outpostSlots[slotIndex], segment.from, segment.to));
                    Assert.That(clearance, Is.GreaterThanOrEqualTo(1.55f),
                        $"Level {levelNumber} slot {slotIndex + 1} overlaps a road (clearance {clearance:0.00}).");
                    Assert.That(clearance, Is.LessThanOrEqualTo(CampaignLevelFactory.SlotRouteClearanceMax),
                        $"Level {levelNumber} slot {slotIndex + 1} floats away from the road edge (clearance {clearance:0.00}).");
                }

                for (int first = 0; first < runtime.outpostSlots.Length; first++)
                for (int second = first + 1; second < runtime.outpostSlots.Length; second++)
                    Assert.That(Vector3.Distance(runtime.outpostSlots[first], runtime.outpostSlots[second]),
                        Is.GreaterThanOrEqualTo(1.2f),
                        $"Level {levelNumber} slots {first + 1} and {second + 1} overlap.");
            }
        }

        [Test]
        public void CampaignBranchRoutesJoinPrimaryOnceWithoutOverlap()
        {
            for (int levelNumber = 1; levelNumber <= CampaignCatalog.LevelCount; levelNumber++)
            {
                RuntimeCampaignLevel runtime = CampaignLevelFactory.Create(levelNumber);
                Vector3[] primary = runtime.routes[0];
                for (int routeIndex = 1; routeIndex < runtime.routes.Length; routeIndex++)
                {
                    Vector3[] branch = runtime.routes[routeIndex];
                    int owned = CampaignLevelFactory.OwnedBranchSegments(branch, primary);
                    Assert.That(owned, Is.GreaterThan(0), $"Level {levelNumber} route {routeIndex + 1} starts on the primary road.");
                    for (int index = owned; index < branch.Length; index++)
                        Assert.That(CampaignLevelFactory.DistanceToRoute(branch[index], primary), Is.LessThan(0.05f),
                            $"Level {levelNumber} route {routeIndex + 1} leaves the primary road after joining it.");

                    // Everything before the perpendicular approach into the junction keeps clear of other roads.
                    float ownedLength = 0f;
                    for (int segment = 0; segment < owned; segment++)
                        ownedLength += Vector3.Distance(branch[segment], branch[segment + 1]);
                    float walked = 0f;
                    for (int segment = 0; segment < owned; segment++)
                    {
                        float length = Vector3.Distance(branch[segment], branch[segment + 1]);
                        for (float along = 0f; along <= length; along += 0.25f)
                        {
                            if (walked + along > ownedLength - CampaignLevelFactory.RouteSeparation - 0.1f) break;
                            Vector3 point = Vector3.Lerp(branch[segment], branch[segment + 1], along / length);
                            for (int other = 0; other < runtime.routes.Length; other++)
                            {
                                if (other == routeIndex) continue;
                                float clearance = CampaignLevelFactory.DistanceToRoute(point, runtime.routes[other]);
                                Assert.That(clearance, Is.GreaterThanOrEqualTo(CampaignLevelFactory.RouteSeparation - 0.01f),
                                    $"Level {levelNumber} route {routeIndex + 1} overlaps route {other + 1} (clearance {clearance:0.00}).");
                            }
                        }
                        walked += length;
                    }
                }
            }
        }

        private static float DistanceToSegment2D(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            segment.y = 0f;
            Vector3 delta = point - from;
            delta.y = 0f;
            float denominator = segment.sqrMagnitude;
            float t = denominator <= 0.001f ? 0f : Mathf.Clamp01(Vector3.Dot(delta, segment) / denominator);
            Vector3 closest = from + segment * t;
            closest.y = point.y;
            return Vector3.Distance(point, closest);
        }

        [Test]
        public void SpawnGroupsRejectInvalidCountsAndIntervals()
        {
            var group = new SpawnGroupDefinition(EnemyArchetype.Raider, -4, -1f, 0f);
            Assert.That(group.count, Is.EqualTo(1));
            Assert.That(group.startDelay, Is.Zero);
            Assert.That(group.interval, Is.EqualTo(0.1f));
        }

        [Test]
        public void ProductionPresentationCatalogIsComplete()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ValidationErrors(), Is.Empty);
            Assert.That(catalog.actorVisuals, Has.Length.EqualTo(8));
            Assert.That(catalog.outpostVisuals, Has.Length.EqualTo(6));
            Assert.That(catalog.uiMixer, Is.Not.SameAs(catalog.sfxMixer));
            Assert.That(catalog.uiMixer, Is.Not.SameAs(catalog.musicMixer));
            Assert.That(catalog.sfxMixer, Is.Not.SameAs(catalog.musicMixer));
            Assert.That(catalog.provenance, Has.Length.GreaterThanOrEqualTo(13));
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Cinzel SemiBold"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Greenwatch welcome artwork"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Greenwatch war table artwork"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Defender Chronicles V2 logo"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Defender Chronicles V2 brand mark"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Defender Chronicles V2 application icon"), Is.True);
            Assert.That(catalog.provenance.Any(entry => entry.assetName == "Defender Chronicles V2 launch artwork"), Is.True);
            Assert.That(catalog.frontendTheme, Is.Not.Null);
            Assert.That(catalog.frontendTheme.brandLogo, Is.Not.Null);
            Assert.That(catalog.frontendTheme.brandMark, Is.Not.Null);
            Assert.That(catalog.frontendTheme.welcomeBackdrop, Is.Not.Null);
            Assert.That(catalog.frontendTheme.warTableBackdrop, Is.Not.Null);
            Assert.That(catalog.licensedAtlasMaterials, Has.Length.EqualTo(5));
            Assert.That(catalog.uiFrameSprite, Is.Not.Null);
            Assert.That(catalog.audioLicense, Is.Not.Null);
            Assert.That(catalog.thirdPartyArtSources, Is.Not.Null);
        }

        [Test]
        public void FrontendThemeAndFontsMeetTheMedievalAccessibilityContract()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.frontendTheme.fadeDuration, Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(catalog.frontendTheme.slideDuration, Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(catalog.frontendTheme.reducedMotionDuration, Is.EqualTo(0.10f).Within(0.001f));

            const string turkishGlyphs = "ĞğİıŞşÇçÖöÜü";
            foreach (char character in turkishGlyphs)
            {
                Assert.That(catalog.titleFont.sourceFontFile.HasCharacter(character), Is.True,
                    $"Cinzel title font must contain Turkish glyph {character}.");
                Assert.That(catalog.bodyFont.sourceFontFile.HasCharacter(character), Is.True,
                    $"Noto Sans body font must contain Turkish glyph {character}.");
            }
        }

        [Test]
        public void BrandAssetsMeetTheMobileImportAndAlphaContract()
        {
            const string logoPath =
                "Assets/Resources/Presentation/Brand/DefenderChronicles-Logo-v2.png";
            const string markPath =
                "Assets/Resources/Presentation/Brand/DefenderChronicles-Mark-v2.png";
            const string iconPath =
                "Assets/Art/AppIcon/BrandV2/DefenderChronicles-AppIcon-v2-1024.png";
            const string launchPath = "Assets/Art/Brand/Launch/GreenwatchLaunch-v2.png";

            Assert.That(Resources.Load<Sprite>("Presentation/Brand/DefenderChronicles-Logo-v2"), Is.Not.Null);
            Assert.That(Resources.Load<Sprite>("Presentation/Brand/DefenderChronicles-Mark-v2"), Is.Not.Null);

#if UNITY_EDITOR
            foreach (string path in new[] { logoPath, markPath })
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, $"Brand sprite importer is missing for {path}.");
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.maxTextureSize, Is.EqualTo(1024));
                Assert.That(importer.GetPlatformTextureSettings("iPhone").format,
                    Is.EqualTo(TextureImporterFormat.ASTC_4x4));
                Assert.That(importer.GetPlatformTextureSettings("Android").format,
                    Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            }
#endif

            AssertPngAlphaContract(logoPath, 2048, 2048, true);
            AssertPngAlphaContract(markPath, 1024, 1024, true);
            AssertPngAlphaContract(iconPath, 1024, 1024, false);
            AssertPngAlphaContract(launchPath, 853, 1844, false);
        }

        private static void AssertPngAlphaContract(string path, int width, int height, bool expectTransparency)
        {
            Assert.That(File.Exists(path), Is.True, $"Required brand PNG is missing: {path}");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false), Is.True);
                Assert.That(texture.width, Is.EqualTo(width));
                Assert.That(texture.height, Is.EqualTo(height));
                Color32[] pixels = texture.GetPixels32();
                if (expectTransparency)
                {
                    Assert.That(pixels.Any(pixel => pixel.a < byte.MaxValue), Is.True,
                        $"Brand asset must preserve transparency: {path}");
                    Assert.That(pixels.Any(pixel => pixel.a > 0), Is.True,
                        $"Brand asset must contain visible pixels: {path}");
                }
                else
                {
                    Assert.That(pixels.All(pixel => pixel.a == byte.MaxValue), Is.True,
                        $"Native mobile artwork must be fully opaque: {path}");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void VisualDevelopmentCatalogPassesActorBuildingSocketAndBudgetGates()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            IReadOnlyList<string> warnings = catalog.VisualDevelopmentValidationWarnings();
            Assert.That(warnings, Is.Empty);
            Assert.That(warnings.Any(item => item.Contains("VIS-203") && item.Contains("Socket_")), Is.False,
                "Generated actor proxies should keep required role sockets wired.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Knight")), Is.False,
                "Knight proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Raider")), Is.False,
                "Raider proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Vanguard")), Is.False,
                "Vanguard proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Regenerator")), Is.False,
                "Regenerator proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Support")), Is.False,
                "Support proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-206") && item.Contains("Siege")), Is.False,
                "Siege proxy should stay within the 2-material review budget.");
            Assert.That(warnings.Any(item => item.Contains("VIS-303") && item.Contains("Socket_")), Is.False,
                "Current outpost catalog should keep required generated sockets wired.");
            Assert.That(warnings.Any(item => item.Contains("VIS-201")), Is.False,
                "Current actor catalog must keep one unique prefab per actor role.");
        }

        [Test]
        public void ActorVfxContractUsesRoleSpecificFamilies()
        {
            DefenderVfxSpec knightSlash = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAttacked, Vector3.forward, actor: PresentationActorKind.Knight));
            Assert.That(knightSlash.projectile, Is.True);
            Assert.That(knightSlash.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(knightSlash.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Fire));
            Assert.That(knightSlash.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalLight));
            Assert.That(knightSlash.sourceSocket, Is.EqualTo("Socket_Weapon_Tip"));

            DefenderVfxSpec shieldBash = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Knight));
            Assert.That(shieldBash.projectile, Is.False);
            Assert.That(shieldBash.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(shieldBash.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(shieldBash.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalBlock));
            Assert.That(shieldBash.endScale, Is.EqualTo(3.2f).Within(0.001f));
            Assert.That(shieldBash.sourceSocket, Is.EqualTo("Socket_Ground"));

            DefenderVfxSpec guardThrust = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostAttacked, Vector3.zero, actor: PresentationActorKind.Guard,
                outpost: OutpostKind.Barracks));
            Assert.That(guardThrust.projectile, Is.False);
            Assert.That(guardThrust.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(guardThrust.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(guardThrust.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalLight));
            Assert.That(guardThrust.endScale, Is.EqualTo(0.56f).Within(0.001f));
            Assert.That(guardThrust.sourceSocket, Is.EqualTo("Socket_Weapon_Tip"));

            DefenderVfxSpec guardBlock = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Guard));
            Assert.That(guardBlock.projectile, Is.False);
            Assert.That(guardBlock.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(guardBlock.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(guardBlock.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalBlock));
            Assert.That(guardBlock.endScale, Is.EqualTo(1.65f).Within(0.001f));
            Assert.That(guardBlock.sourceSocket, Is.EqualTo("Socket_Chest"));

            DefenderVfxSpec raiderSpawn = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.UnitSpawned, Vector3.zero, actor: PresentationActorKind.Raider));
            Assert.That(raiderSpawn.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(raiderSpawn.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Dark));
            Assert.That(raiderSpawn.impactClass, Is.EqualTo(DefenderVfxImpactClass.SpawnDeath));
            Assert.That(raiderSpawn.sourceSocket, Is.EqualTo("Socket_Ground"));

            DefenderVfxSpec raiderHit = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider));
            Assert.That(raiderHit.projectile, Is.True);
            Assert.That(raiderHit.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Enemy));
            Assert.That(raiderHit.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalLight));
            Assert.That(raiderHit.sourceSocket, Is.EqualTo("Socket_Chest"));

            DefenderVfxSpec raiderDeath = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.EnemyDefeated, Vector3.zero, actor: PresentationActorKind.Raider));
            Assert.That(raiderDeath.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(raiderDeath.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Enemy));
            Assert.That(raiderDeath.impactClass, Is.EqualTo(DefenderVfxImpactClass.SpawnDeath));
            Assert.That(raiderDeath.sourceSocket, Is.EqualTo("Socket_Ground"));

            DefenderVfxSpec vanguardHeavy = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Vanguard));
            Assert.That(vanguardHeavy.projectile, Is.False);
            Assert.That(vanguardHeavy.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(vanguardHeavy.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Enemy));
            Assert.That(vanguardHeavy.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalHeavy));
            Assert.That(vanguardHeavy.priority, Is.EqualTo(DefenderVfxPriority.High));
            Assert.That(vanguardHeavy.concurrencyGroup, Is.EqualTo("VanguardImpact"));
            Assert.That(vanguardHeavy.maxConcurrent, Is.EqualTo(6));
            Assert.That(vanguardHeavy.endScale, Is.EqualTo(0.88f).Within(0.001f));
            Assert.That(vanguardHeavy.sourceSocket, Is.EqualTo("Socket_Weapon_Tip"));

            DefenderVfxSpec siegeImpact = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Siege));
            Assert.That(siegeImpact.projectile, Is.True);
            Assert.That(siegeImpact.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(siegeImpact.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Dark));
            Assert.That(siegeImpact.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalHeavy));
            Assert.That(siegeImpact.priority, Is.EqualTo(DefenderVfxPriority.High));
            Assert.That(siegeImpact.concurrencyGroup, Is.EqualTo("SiegeImpact"));
            Assert.That(siegeImpact.maxConcurrent, Is.EqualTo(4));
            Assert.That(siegeImpact.endScale, Is.EqualTo(1.12f).Within(0.001f));
            Assert.That(siegeImpact.sourceSocket, Is.EqualTo("Socket_Muzzle"));

            DefenderVfxSpec bossAttack = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.BossAttack, Vector3.zero, actor: PresentationActorKind.Boss));
            Assert.That(bossAttack.projectile, Is.True);
            Assert.That(bossAttack.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(bossAttack.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Enemy));
            Assert.That(bossAttack.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalHeavy));
            Assert.That(bossAttack.priority, Is.EqualTo(DefenderVfxPriority.High));
            Assert.That(bossAttack.concurrencyGroup, Is.EqualTo("BossImpact"));
            Assert.That(bossAttack.maxConcurrent, Is.EqualTo(3));
            Assert.That(bossAttack.endScale, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(bossAttack.sourceSocket, Is.EqualTo("Socket_Weapon_Tip"));
        }

        [Test]
        public void BossVfxPriorityKeepsCriticalPhaseAboveCombatBurst()
        {
            DefenderVfxSpec phase = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss));
            Assert.That(phase.priority, Is.EqualTo(DefenderVfxPriority.Critical));
            Assert.That(phase.concurrencyGroup, Is.EqualTo("BossTelegraph"));
            Assert.That(phase.maxConcurrent, Is.EqualTo(2));

            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            var host = new GameObject("Boss VFX Priority Test");
            var source = new GameObject("Boss VFX Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));
            CreateSocket(source.transform, "Socket_Cast", new Vector3(1f, 1.35f, 0.18f));

            var pool = host.AddComponent<DefenderVfxPool>();
            pool.Initialize(catalog, 4);
            pool.Play(new PresentationSignal(PresentationCue.BossPhase, Vector3.zero, source,
                PresentationActorKind.Boss));
            for (int i = 0; i < 12; i++)
                pool.Play(new PresentationSignal(PresentationCue.EnemyHit, Vector3.right * i, source,
                    PresentationActorKind.Raider));

            Assert.That(pool.PlayingCountAtPriorityOrAbove(DefenderVfxPriority.Critical), Is.EqualTo(1));
            Assert.That(pool.PlayingCountForGroup("BossTelegraph"), Is.EqualTo(1));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void ArrowProjectileVfxUsesMuzzleAndStaysDistinctFromMeleeAndMagic()
        {
            DefenderVfxSpec archerArrow = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostAttacked, Vector3.forward, outpost: OutpostKind.Archer));
            Assert.That(archerArrow.projectile, Is.True);
            Assert.That(archerArrow.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(archerArrow.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Fire));
            Assert.That(archerArrow.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalLight));
            Assert.That(archerArrow.endScale, Is.EqualTo(0.44f).Within(0.001f));
            Assert.That(archerArrow.sourceSocket, Is.EqualTo("Socket_Muzzle"));

            DefenderVfxSpec guardStrike = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostAttacked, Vector3.forward, actor: PresentationActorKind.Guard,
                outpost: OutpostKind.Barracks));
            Assert.That(guardStrike.projectile, Is.False);
            Assert.That(guardStrike.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalLight));
            Assert.That(guardStrike.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(guardStrike.endScale, Is.EqualTo(0.56f).Within(0.001f));
            Assert.That(guardStrike.sourceSocket, Is.EqualTo("Socket_Weapon_Tip"));

            DefenderVfxSpec mageImpact = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.MageImpact, Vector3.forward, outpost: OutpostKind.Mage));
            Assert.That(mageImpact.projectile, Is.True);
            Assert.That(mageImpact.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Magic));
            Assert.That(mageImpact.impactClass, Is.EqualTo(DefenderVfxImpactClass.Magical));
            Assert.That(mageImpact.sourceSocket, Is.EqualTo("Socket_Cast"));

            DefenderVfxSpec siegeShot = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.forward, actor: PresentationActorKind.Siege));
            Assert.That(siegeShot.projectile, Is.True);
            Assert.That(siegeShot.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Dark));
            Assert.That(siegeShot.impactClass, Is.EqualTo(DefenderVfxImpactClass.PhysicalHeavy));
            Assert.That(siegeShot.sourceSocket, Is.EqualTo("Socket_Muzzle"));
        }

        [Test]
        public void ArcaneProjectileVfxStaysDistinctFromArrowAndHeal()
        {
            DefenderVfxSpec arcane = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.MageImpact, Vector3.forward, outpost: OutpostKind.Mage));
            Assert.That(arcane.projectile, Is.True);
            Assert.That(arcane.shape, Is.EqualTo(DefenderVfxShape.Diamond));
            Assert.That(arcane.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Magic));
            Assert.That(arcane.impactClass, Is.EqualTo(DefenderVfxImpactClass.Magical));
            Assert.That(arcane.endScale, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(arcane.sourceSocket, Is.EqualTo("Socket_Cast"));

            DefenderVfxSpec arrow = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostAttacked, Vector3.forward, outpost: OutpostKind.Archer));
            Assert.That(arrow.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Fire));
            Assert.That(arrow.sourceSocket, Is.EqualTo("Socket_Muzzle"));

            DefenderVfxSpec heal = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HealPulse, Vector3.forward));
            Assert.That(heal.projectile, Is.False);
            Assert.That(heal.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(heal.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(heal.sourceSocket, Is.EqualTo("Socket_Ground"));
        }

        [Test]
        public void HealAndRegenVfxUseDistinctActorFamilies()
        {
            DefenderVfxSpec supportHeal = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HealPulse, Vector3.forward, actor: PresentationActorKind.Support));
            Assert.That(supportHeal.projectile, Is.True);
            Assert.That(supportHeal.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(supportHeal.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Defender));
            Assert.That(supportHeal.impactClass, Is.EqualTo(DefenderVfxImpactClass.Magical));
            Assert.That(supportHeal.endScale, Is.EqualTo(0.96f).Within(0.001f));
            Assert.That(supportHeal.sourceSocket, Is.EqualTo("Socket_Cast"));
            Assert.That(supportHeal.concurrencyGroup, Is.EqualTo("SupportHeal"));
            Assert.That(supportHeal.maxConcurrent, Is.EqualTo(6));

            DefenderVfxSpec regenPulse = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.HealPulse, Vector3.forward, actor: PresentationActorKind.Regenerator));
            Assert.That(regenPulse.projectile, Is.False);
            Assert.That(regenPulse.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(regenPulse.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Enemy));
            Assert.That(regenPulse.impactClass, Is.EqualTo(DefenderVfxImpactClass.Magical));
            Assert.That(regenPulse.endScale, Is.EqualTo(1.18f).Within(0.001f));
            Assert.That(regenPulse.sourceSocket, Is.EqualTo("Socket_Core"));
            Assert.That(regenPulse.targetSocket, Is.EqualTo("Socket_Ground"));
            Assert.That(regenPulse.concurrencyGroup, Is.EqualTo("RegeneratorPulse"));
            Assert.That(regenPulse.maxConcurrent, Is.EqualTo(8));
        }

        [Test]
        public void SlowAndDisruptStatusVfxUseDistinctMarkers()
        {
            DefenderVfxSpec slow = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.SlowApplied, Vector3.forward, actor: PresentationActorKind.Raider));
            Assert.That(slow.projectile, Is.False);
            Assert.That(slow.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(slow.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Magic));
            Assert.That(slow.impactClass, Is.EqualTo(DefenderVfxImpactClass.Status));
            Assert.That(slow.endScale, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(slow.sourceSocket, Is.EqualTo("Socket_Chest"));

            DefenderVfxSpec disrupt = DefenderVfxPool.Describe(new PresentationSignal(
                PresentationCue.OutpostDisrupted, Vector3.forward, outpost: OutpostKind.Archer));
            Assert.That(disrupt.projectile, Is.False);
            Assert.That(disrupt.shape, Is.EqualTo(DefenderVfxShape.Ring));
            Assert.That(disrupt.materialRole, Is.EqualTo(DefenderVfxMaterialRole.Dark));
            Assert.That(disrupt.impactClass, Is.EqualTo(DefenderVfxImpactClass.Status));
            Assert.That(disrupt.endScale, Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(disrupt.sourceSocket, Is.EqualTo("Socket_Ground"));
        }

        [Test]
        public void FeedbackIntensityContractScalesAndNeverBlocksTouch()
        {
            var bossAttack = new PresentationSignal(PresentationCue.BossAttack, Vector3.zero,
                actor: PresentationActorKind.Boss);
            PresentationFeedbackSpec full = PresentationFeedbackContract.Describe(bossAttack);
            PresentationFeedbackSpec reduced = PresentationFeedbackContract.Describe(bossAttack,
                PresentationFeedbackIntensityMode.Reduced);
            PresentationFeedbackSpec off = PresentationFeedbackContract.Describe(bossAttack,
                PresentationFeedbackIntensityMode.Off);

            Assert.That(full.cameraShakeAmplitude, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(full.hitStopSeconds, Is.Zero);
            Assert.That(reduced.cameraShakeAmplitude, Is.LessThan(full.cameraShakeAmplitude));
            Assert.That(reduced.hitStopSeconds, Is.Zero);
            Assert.That(off.cameraShakeAmplitude, Is.Zero);
            Assert.That(off.cameraShakeSeconds, Is.Zero);
            Assert.That(off.hitStopSeconds, Is.Zero);
            Assert.That(full.touchInputBlocked, Is.False);
            Assert.That(reduced.touchInputBlocked, Is.False);
            Assert.That(off.touchInputBlocked, Is.False);

            PresentationFeedbackSpec bossPhase = PresentationFeedbackContract.Describe(
                new PresentationSignal(PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss));
            PresentationFeedbackSpec enemyHit = PresentationFeedbackContract.Describe(
                new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider));
            Assert.That(bossPhase.cameraShakeAmplitude, Is.GreaterThan(enemyHit.cameraShakeAmplitude));
            Assert.That(bossPhase.cameraShakeSeconds, Is.GreaterThan(enemyHit.cameraShakeSeconds));
        }

        [Test]
        public void CinematicFramingContractOnlyTargetsBossAndResultMoments()
        {
            PresentationCinematicFramingSpec bossPhase = PresentationCinematicFramingContract.Describe(
                new PresentationSignal(PresentationCue.BossPhase, Vector3.zero, actor: PresentationActorKind.Boss));
            PresentationCinematicFramingSpec bossAttack = PresentationCinematicFramingContract.Describe(
                new PresentationSignal(PresentationCue.BossAttack, Vector3.zero, actor: PresentationActorKind.Boss));
            PresentationCinematicFramingSpec victory = PresentationCinematicFramingContract.Describe(
                new PresentationSignal(PresentationCue.Victory, Vector3.zero));
            PresentationCinematicFramingSpec defeat = PresentationCinematicFramingContract.Describe(
                new PresentationSignal(PresentationCue.Defeat, Vector3.zero));
            PresentationCinematicFramingSpec enemyHit = PresentationCinematicFramingContract.Describe(
                new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider));

            Assert.That(bossPhase.enabled, Is.True);
            Assert.That(bossAttack.enabled, Is.True);
            Assert.That(victory.enabled, Is.True);
            Assert.That(defeat.enabled, Is.True);
            Assert.That(enemyHit.enabled, Is.False);
            Assert.That(bossPhase.orthographicSizeMultiplier, Is.LessThan(1f));
            Assert.That(bossPhase.holdSeconds, Is.GreaterThan(bossAttack.holdSeconds));
        }

        [Test]
        public void CinematicFramingContractHonorsReducedAndOffIntensityModes()
        {
            var signal = new PresentationSignal(PresentationCue.BossPhase, Vector3.zero,
                actor: PresentationActorKind.Boss);
            PresentationCinematicFramingSpec full = PresentationCinematicFramingContract.Describe(signal);
            PresentationCinematicFramingSpec reduced = PresentationCinematicFramingContract.Describe(signal,
                PresentationFeedbackIntensityMode.Reduced);
            PresentationCinematicFramingSpec off = PresentationCinematicFramingContract.Describe(signal,
                PresentationFeedbackIntensityMode.Off);

            Assert.That(reduced.enabled, Is.True);
            Assert.That(reduced.normalizedFocusOffset.magnitude, Is.LessThan(full.normalizedFocusOffset.magnitude));
            Assert.That(reduced.orthographicSizeMultiplier, Is.GreaterThan(full.orthographicSizeMultiplier));
            Assert.That(reduced.holdSeconds, Is.LessThan(full.holdSeconds));
            Assert.That(off.enabled, Is.False);
            Assert.That(off.orthographicSizeMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void ActorVfxSocketOriginsPreferNamedRuntimeSockets()
        {
            var root = new GameObject("Socket Test Actor");
            var tip = new GameObject("Socket_Weapon_Tip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.position = new Vector3(3f, 1.4f, 2f);
            var ground = new GameObject("Socket_Ground");
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(3f, 0.05f, 2f);
            var chest = new GameObject("Socket_Chest");
            chest.transform.SetParent(root.transform, false);
            chest.transform.position = new Vector3(3f, 0.9f, 2f);

            Assert.That(DefenderVfxPool.ResolveSocketPosition(root, "Socket_Weapon_Tip", Vector3.zero),
                Is.EqualTo(tip.transform.position));
            Assert.That(DefenderVfxPool.ResolveSocketPosition(root, "Socket_Ground", Vector3.zero),
                Is.EqualTo(ground.transform.position));
            Assert.That(DefenderVfxPool.ResolveSocketPosition(root, "Socket_Chest", Vector3.zero),
                Is.EqualTo(chest.transform.position));
            Assert.That(DefenderVfxPool.ResolveSocketPosition(root, "Socket_Missing", Vector3.one),
                Is.EqualTo(Vector3.one));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ActorVfxPoolHandlesSixtySignalBurstWithoutGrowing()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            var host = new GameObject("VFX Pool Stress Test");
            var source = new GameObject("Socketed Actor Source");
            CreateSocket(source.transform, "Socket_Weapon_Tip", new Vector3(1f, 1.2f, 0f));
            CreateSocket(source.transform, "Socket_Ground", new Vector3(1f, 0.02f, 0f));
            CreateSocket(source.transform, "Socket_Chest", new Vector3(1f, 0.8f, 0f));
            CreateSocket(source.transform, "Socket_Cast", new Vector3(1f, 1.35f, 0.18f));

            var pool = host.AddComponent<DefenderVfxPool>();
            pool.Initialize(catalog, 96);
            for (int i = 0; i < 60; i++)
                pool.Play(StressSignal(i, source));

            Assert.That(pool.PoolCount, Is.EqualTo(96));
            Assert.That(pool.PlayingCount, Is.EqualTo(60));

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void ActorAnimationCueContractUsesSharedTriggers()
        {
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.HeroAttacked, Vector3.zero, actor: PresentationActorKind.Knight)),
                Is.EqualTo(PresentationActorAnimationContract.AttackState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Knight)),
                Is.EqualTo(PresentationActorAnimationContract.AbilityState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider)),
                Is.EqualTo(PresentationActorAnimationContract.HitState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.EnemyDefeated, Vector3.zero, actor: PresentationActorKind.Raider)),
                Is.EqualTo(PresentationActorAnimationContract.DefeatState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero, actor: PresentationActorKind.Regenerator)),
                Is.EqualTo(PresentationActorAnimationContract.AbilityState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.HealPulse, Vector3.zero, actor: PresentationActorKind.Support)),
                Is.EqualTo(PresentationActorAnimationContract.AbilityState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Siege)),
                Is.EqualTo(PresentationActorAnimationContract.HitState));
            Assert.That(DefenderPresentationDirector.AnimationTriggerFor(new PresentationSignal(
                    PresentationCue.UnitSpawned, Vector3.zero, actor: PresentationActorKind.Raider)),
                Is.Null);
        }

        [Test]
        public void ActionTimingContractDocumentsImpactFramesAndSockets()
        {
            PresentationActionTimingSpec knightSlash = PresentationActionTimingContract.Describe(
                new PresentationSignal(PresentationCue.HeroAttacked, Vector3.zero, actor: PresentationActorKind.Knight));
            Assert.That(knightSlash.animationTrigger, Is.EqualTo(PresentationActorAnimationContract.AttackState));
            Assert.That(knightSlash.impactNormalizedTime, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(knightSlash.impactSocket, Is.EqualTo("Socket_Weapon_Tip"));
            Assert.That(knightSlash.gameplayAuthoritative, Is.False);

            PresentationActionTimingSpec knightShield = PresentationActionTimingContract.Describe(
                new PresentationSignal(PresentationCue.HeroAbility, Vector3.zero, actor: PresentationActorKind.Knight));
            Assert.That(knightShield.animationTrigger, Is.EqualTo(PresentationActorAnimationContract.AbilityState));
            Assert.That(knightShield.impactNormalizedTime, Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(knightShield.impactSocket, Is.EqualTo("Socket_Ground"));

            PresentationActionTimingSpec raiderHit = PresentationActionTimingContract.Describe(
                new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero, actor: PresentationActorKind.Raider));
            Assert.That(raiderHit.animationTrigger, Is.EqualTo(PresentationActorAnimationContract.HitState));
            Assert.That(raiderHit.impactNormalizedTime, Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(raiderHit.impactSocket, Is.EqualTo("Socket_Chest"));

            PresentationActionTimingSpec bossAttack = PresentationActionTimingContract.Describe(
                new PresentationSignal(PresentationCue.BossAttack, Vector3.zero, actor: PresentationActorKind.Boss));
            Assert.That(bossAttack.animationTrigger, Is.EqualTo(PresentationActorAnimationContract.BossAttackState));
            Assert.That(bossAttack.impactNormalizedTime, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(bossAttack.impactSocket, Is.EqualTo("Socket_Weapon_Tip"));

            PresentationActionTimingSpec siegeRecoil = PresentationActionTimingContract.Describe(
                new PresentationSignal(PresentationCue.OutpostDisrupted, Vector3.zero, actor: PresentationActorKind.Siege));
            Assert.That(siegeRecoil.animationTrigger, Is.EqualTo(PresentationActorAnimationContract.HitState));
            Assert.That(siegeRecoil.impactNormalizedTime, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(siegeRecoil.impactSocket, Is.EqualTo("Socket_Muzzle"));
        }

        [Test]
        public void AudioPolicyCoversEveryCueAndProtectsCriticalSignals()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            foreach (PresentationCue cue in System.Enum.GetValues(typeof(PresentationCue)))
            {
                PresentationAudioCueSpec spec = PresentationAudioPolicy.Describe(
                    new PresentationSignal(cue, Vector3.zero));
                AudioCueEntry entry = catalog.Audio(cue);
                Assert.That(entry, Is.Not.Null, $"{cue} needs a catalog audio entry.");
                Assert.That(spec.concurrencyGroup, Is.Not.Empty, $"{cue} needs a concurrency group.");
                Assert.That(spec.maxConcurrent, Is.InRange(1, 4), $"{cue} concurrency must stay mobile-safe.");
                Assert.That(entry.concurrency, Is.EqualTo(spec.maxConcurrent),
                    $"{cue} catalog concurrency must match the runtime policy.");
                Assert.That(spec.delaySeconds, Is.InRange(0f, 0.25f), $"{cue} audio delay is outside the impact window.");
                Assert.That(spec.musicDuck, Is.InRange(0f, 0.7f));
            }

            foreach (PresentationCue cue in new[]
                     {
                         PresentationCue.HeroAbility, PresentationCue.BossAttack,
                         PresentationCue.BossPhase, PresentationCue.Victory, PresentationCue.Defeat
                     })
            {
                PresentationAudioCueSpec spec = PresentationAudioPolicy.Describe(
                    new PresentationSignal(cue, Vector3.zero));
                Assert.That(spec.priority, Is.EqualTo(PresentationAudioPriority.Critical));
                Assert.That(spec.musicDuck, Is.GreaterThan(0f));
            }

            Assert.That(PresentationAudioPolicy.Describe(
                    new PresentationSignal(PresentationCue.EnemyHit, Vector3.zero)).maxConcurrent,
                Is.EqualTo(4));
            Assert.That(PresentationAudioPolicy.CanReplace(PresentationAudioPriority.Ambient,
                PresentationAudioPriority.Critical), Is.True);
            Assert.That(PresentationAudioPolicy.CanReplace(PresentationAudioPriority.Critical,
                PresentationAudioPriority.Normal), Is.False);
        }

        [Test]
        public void AudioReviewLevelsAndMusicTransitionsHaveDeterministicContracts()
        {
            Assert.That(PresentationAudioPolicy.ReviewGain(PresentationAudioReviewLevel.Silent), Is.Zero);
            Assert.That(PresentationAudioPolicy.ReviewGain(PresentationAudioReviewLevel.Low),
                Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(PresentationAudioPolicy.ReviewGain(PresentationAudioReviewLevel.Normal), Is.EqualTo(1f));

            foreach (MusicState state in System.Enum.GetValues(typeof(MusicState)))
            {
                float seconds = PresentationAudioPolicy.MusicTransitionSeconds(state);
                Assert.That(seconds, Is.InRange(0.25f, 1.2f), $"{state} transition must avoid a hard cut.");
            }

            Assert.That(PresentationAudioPolicy.MusicTransitionSeconds(MusicState.Boss),
                Is.LessThan(PresentationAudioPolicy.MusicTransitionSeconds(MusicState.Preparation)));
            Assert.That(PresentationAudioPolicy.UnityPriority(PresentationAudioPriority.Critical),
                Is.LessThan(PresentationAudioPolicy.UnityPriority(PresentationAudioPriority.Normal)));
        }

        [Test]
        public void EnvironmentAmbienceIsDisabledOnLowAndSubordinateOnHigherProfiles()
        {
            DefenderAmbientQualityConfig low = DefenderAmbientAudioController.QualityConfigForName("Low");
            DefenderAmbientQualityConfig medium = DefenderAmbientAudioController.QualityConfigForName("Medium");
            DefenderAmbientQualityConfig high = DefenderAmbientAudioController.QualityConfigForName("High");

            Assert.That(low.enabled, Is.False);
            Assert.That(low.windVolume, Is.Zero);
            Assert.That(medium.enabled, Is.True);
            Assert.That(high.enabled, Is.True);
            Assert.That(medium.windVolume, Is.LessThan(0.06f));
            Assert.That(high.windVolume, Is.LessThan(0.08f));
            Assert.That(medium.detailVolume, Is.LessThan(0.1f));
            Assert.That(high.minimumDetailInterval, Is.GreaterThanOrEqualTo(5f));
            Assert.That(medium.minimumDetailInterval, Is.GreaterThan(high.minimumDetailInterval));
            Assert.That(medium.maximumDetailInterval, Is.GreaterThan(medium.minimumDetailInterval));
        }

        [Test]
        public void EveryActorRoleUsesAnAuthoredAnimatorOverride()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.actorAnimator, Is.Not.Null);

            foreach (PresentationActorKind kind in System.Enum.GetValues(typeof(PresentationActorKind)))
            {
                Assert.That(catalog.ActorAnimator(kind), Is.Not.Null, $"{kind} should resolve an animator.");
                Assert.That(catalog.ActorAnimator(kind), Is.TypeOf<AnimatorOverrideController>(),
                    $"{kind} should use its authored role override.");
                Assert.That(catalog.ActorAnimator(kind), Is.Not.SameAs(catalog.actorAnimator),
                    $"{kind} should not fall back to the shared clips.");
                Assert.That(catalog.UsesSharedActorAnimator(kind), Is.False,
                    $"{kind} should no longer report a shared fallback.");
            }
        }

        [Test]
        public void KnightGuardRaiderVanguardRegeneratorSupportAndSiegeAnimatorOverridesBindRoleSpecificClips()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Knight), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Guard), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Raider), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Vanguard), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Regenerator), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Support), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Siege), Is.TypeOf<AnimatorOverrideController>());
            Assert.That(catalog.ActorAnimator(PresentationActorKind.Boss), Is.TypeOf<AnimatorOverrideController>());

            var knight = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Knight);
            var guard = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Guard);
            var raider = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Raider);
            var vanguard = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Vanguard);
            var regenerator = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Regenerator);
            var support = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Support);
            var siege = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Siege);
            var boss = (AnimatorOverrideController)catalog.ActorAnimator(PresentationActorKind.Boss);

            AssertRoleClip(knight, PresentationActorAnimationContract.AttackState, "Knight_Attack");
            AssertRoleClip(knight, PresentationActorAnimationContract.AbilityState, "Knight_Ability");
            AssertRoleClip(knight, PresentationActorAnimationContract.DefeatState, "Knight_Defeat");
            AssertRoleClip(guard, PresentationActorAnimationContract.MoveState, "Guard_Move");
            AssertRoleClip(guard, PresentationActorAnimationContract.AttackState, "Guard_Attack");
            AssertRoleClip(guard, PresentationActorAnimationContract.AbilityState, "Guard_Ability");
            AssertRoleClip(guard, PresentationActorAnimationContract.HitState, "Guard_Hit");
            AssertRoleClip(guard, PresentationActorAnimationContract.DefeatState, "Guard_Defeat");
            AssertRoleClip(raider, PresentationActorAnimationContract.MoveState, "Raider_Move");
            AssertRoleClip(raider, PresentationActorAnimationContract.AttackState, "Raider_Attack");
            AssertRoleClip(raider, PresentationActorAnimationContract.HitState, "Raider_Hit");
            AssertRoleClip(raider, PresentationActorAnimationContract.DefeatState, "Raider_Defeat");
            AssertRoleClip(vanguard, PresentationActorAnimationContract.MoveState, "Vanguard_Move");
            AssertRoleClip(vanguard, PresentationActorAnimationContract.AttackState, "Vanguard_Attack");
            AssertRoleClip(vanguard, PresentationActorAnimationContract.HitState, "Vanguard_Hit");
            AssertRoleClip(vanguard, PresentationActorAnimationContract.DefeatState, "Vanguard_Defeat");
            AssertRoleClip(vanguard, PresentationActorAnimationContract.AbilityState, "Vanguard_Ability");
            AssertRoleClip(regenerator, PresentationActorAnimationContract.MoveState, "Regenerator_Move");
            AssertRoleClip(regenerator, PresentationActorAnimationContract.AttackState, "Regenerator_Attack");
            AssertRoleClip(regenerator, PresentationActorAnimationContract.HitState, "Regenerator_Hit");
            AssertRoleClip(regenerator, PresentationActorAnimationContract.DefeatState, "Regenerator_Defeat");
            AssertRoleClip(regenerator, PresentationActorAnimationContract.AbilityState, "Regenerator_Ability");
            AssertRoleClip(support, PresentationActorAnimationContract.MoveState, "Support_Move");
            AssertRoleClip(support, PresentationActorAnimationContract.AttackState, "Support_Attack");
            AssertRoleClip(support, PresentationActorAnimationContract.HitState, "Support_Hit");
            AssertRoleClip(support, PresentationActorAnimationContract.DefeatState, "Support_Defeat");
            AssertRoleClip(support, PresentationActorAnimationContract.AbilityState, "Support_Ability");
            AssertRoleClip(siege, PresentationActorAnimationContract.MoveState, "Siege_Move");
            AssertRoleClip(siege, PresentationActorAnimationContract.AttackState, "Siege_Attack");
            AssertRoleClip(siege, PresentationActorAnimationContract.HitState, "Siege_Hit");
            AssertRoleClip(siege, PresentationActorAnimationContract.DefeatState, "Siege_Defeat");
            AssertRoleClip(siege, PresentationActorAnimationContract.AbilityState, "Siege_Ability");
            AssertRoleClip(boss, PresentationActorAnimationContract.AttackState, "Boss_Attack");
            AssertRoleClip(boss, PresentationActorAnimationContract.PhaseState, "Boss_Phase");
            AssertRoleClip(boss, PresentationActorAnimationContract.BossAttackState, "Boss_BossAttack");
        }

#if UNITY_EDITOR
        [Test]
        public void FinalOutpostsUseSixDistinctGeneratedModels()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            string[] paths = catalog.outpostVisuals
                .Select(entry => AssetDatabase.GetAssetPath(entry.prefab))
                .ToArray();
            Assert.That(paths, Has.Length.EqualTo(6));
            Assert.That(paths.Distinct().Count(), Is.EqualTo(6));
            Assert.That(paths.All(path => path.StartsWith("Assets/Art/Models/Generated/")), Is.True,
                "Runtime outposts must no longer resolve to the KayKit comparison proxies.");
        }

        [Test]
        public void RoleAnimationClipsContainRigBoneMotion()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);

            AssertRoleRigMotion(catalog, PresentationActorKind.Knight,
                PresentationActorAnimationContract.AttackState, "Arm.R");
            AssertRoleRigMotion(catalog, PresentationActorKind.Raider,
                PresentationActorAnimationContract.MoveState, "Leg.R");
            AssertRoleRigMotion(catalog, PresentationActorKind.Boss,
                PresentationActorAnimationContract.PhaseState, "Spine");
            AssertRoleRigMotion(catalog, PresentationActorKind.Siege,
                PresentationActorAnimationContract.AttackState, "Arm.R");

            AssertBoneOwnsVisibleGeometry(catalog.ActorPrefab(PresentationActorKind.Knight), "Arm.R");
            AssertBoneOwnsVisibleGeometry(catalog.ActorPrefab(PresentationActorKind.Raider), "Leg.R");
            AssertBoneOwnsVisibleGeometry(catalog.ActorPrefab(PresentationActorKind.Boss), "Spine");
            AssertBoneOwnsVisibleGeometry(catalog.ActorPrefab(PresentationActorKind.Siege), "Arm.R");
        }

        [Test]
        public void BossHasThreePhaseSilhouettePartsAndRuntimeToggleContract()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            GameObject source = catalog.ActorPrefab(PresentationActorKind.Boss);
            Assert.That(source, Is.Not.Null);

            GameObject instance = Object.Instantiate(source);
            try
            {
                var phaseTwo = instance.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith("Phase2_"))
                    .Select(item => item.gameObject).ToArray();
                var phaseThree = instance.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith("Phase3_"))
                    .Select(item => item.gameObject).ToArray();
                Assert.That(phaseTwo, Has.Length.GreaterThanOrEqualTo(3));
                Assert.That(phaseThree, Has.Length.GreaterThanOrEqualTo(4));

                var phaseVisual = instance.AddComponent<BossPhaseVisual>();
                phaseVisual.Configure(instance.transform, catalog);
                Assert.That(phaseTwo.All(part => !part.activeSelf), Is.True);
                Assert.That(phaseThree.All(part => !part.activeSelf), Is.True);

                phaseVisual.SetPhase(2);
                Assert.That(phaseTwo.All(part => part.activeSelf), Is.True);
                Assert.That(phaseThree.All(part => !part.activeSelf), Is.True);

                phaseVisual.SetPhase(3);
                Assert.That(phaseThree.All(part => part.activeSelf), Is.True);
                Assert.That(phaseVisual.CurrentPhase, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertRoleRigMotion(DefenderPresentationCatalog catalog,
            PresentationActorKind kind, string sharedClipName, string expectedBone)
        {
            var controller = (AnimatorOverrideController)catalog.ActorAnimator(kind);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            AnimationClip clip = overrides.First(pair => pair.Key != null && pair.Key.name == sharedClipName).Value;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.That(bindings.Any(binding => binding.path.EndsWith(expectedBone) &&
                                                binding.propertyName.StartsWith("localEulerAnglesRaw")), Is.True,
                $"{clip.name} should articulate {expectedBone} through a rig curve.");
        }

        private static void AssertBoneOwnsVisibleGeometry(GameObject actorPrefab, string boneName)
        {
            Transform bone = actorPrefab.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == boneName);
            Assert.That(bone, Is.Not.Null, $"{actorPrefab.name} should contain rig bone {boneName}.");
            Assert.That(bone.GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
                $"{actorPrefab.name} {boneName} needs rigid-parented geometry so its authored curve is visible.");
        }
#endif

        private static void AssertRoleClip(AnimatorOverrideController controller, string sharedClipName, string expectedRoleClipName)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            AnimationClip replacement = overrides.FirstOrDefault(pair => pair.Key != null && pair.Key.name == sharedClipName).Value;
            Assert.That(replacement, Is.Not.Null, $"{controller.name} should override {sharedClipName}.");
            Assert.That(replacement.name, Is.EqualTo(expectedRoleClipName));
        }

#if UNITY_EDITOR
        [Test]
        public void SharedActorAnimatorPrioritizesDefeatAndAvoidsHitSpam()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.actorAnimator, Is.TypeOf<AnimatorController>());
            var controller = (AnimatorController)catalog.actorAnimator;
            Assert.That(controller.layers, Is.Not.Empty);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorStateTransition defeat = AnyStateTransition(machine, PresentationActorAnimationContract.DefeatState);
            Assert.That(defeat, Is.Not.Null);
            Assert.That(defeat.hasExitTime, Is.False);
            Assert.That(defeat.duration, Is.EqualTo(0f).Within(0.001f));
            Assert.That(defeat.interruptionSource, Is.EqualTo(TransitionInterruptionSource.SourceThenDestination));
            Assert.That(defeat.orderedInterruption, Is.True);

            AnimatorStateTransition hit = AnyStateTransition(machine, PresentationActorAnimationContract.HitState);
            Assert.That(hit, Is.Not.Null);
            Assert.That(hit.canTransitionToSelf, Is.False);
            Assert.That(hit.duration, Is.LessThanOrEqualTo(0.02f));

            AnimatorStateTransition victory = AnyStateTransition(machine, PresentationActorAnimationContract.VictoryState);
            Assert.That(victory, Is.Not.Null);
            Assert.That(victory.hasExitTime, Is.False);
            Assert.That(victory.duration, Is.LessThanOrEqualTo(0.05f));
        }
#endif

        [Test]
        public void AnimationEventRelayIsPresentationOnly()
        {
            Assert.That(PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.HeroAttacked), Is.True);
            Assert.That(PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.BossAttack), Is.True);
            Assert.That(PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.UiClick), Is.False);
            Assert.That(PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.UiError), Is.False);
            Assert.That(PresentationAnimationEventRelay.IsPresentationOnlyCue(PresentationCue.WaveStarted), Is.False);

            var root = new GameObject("Animation Relay Source");
            var relayObject = new GameObject("Animation Relay");
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

            Assert.That(emitted, Is.EqualTo(1));
            Assert.That(last.cue, Is.EqualTo(PresentationCue.HeroAttacked));
            Assert.That(last.actor, Is.EqualTo(PresentationActorKind.Knight));
            Assert.That(last.source, Is.SameAs(root));

            string[] forbiddenMethods = { "TakeDamage", "DamageEnemies", "EarnGold", "SpendGold" };
            var relayMethods = typeof(PresentationAnimationEventRelay).GetMethods()
                .Where(method => method.DeclaringType == typeof(PresentationAnimationEventRelay))
                .Select(method => method.Name);
            foreach (string forbidden in forbiddenMethods)
                Assert.That(relayMethods, Does.Not.Contain(forbidden));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TurkishUiCopyHasEveryPublicKey()
        {
            var copy = Resources.Load<UiCopyCatalog>("Presentation/UiCopyTr");
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.locale, Is.EqualTo("tr-TR"));
            Assert.That(copy.ValidationErrors(), Is.Empty);
        }

        [Test]
        public void ProductionHudTouchTargetsStayAboveMinimum()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.hudPrefab, Is.Not.Null);

            AssertTouchTarget(catalog.hudPrefab.transform, "TopBar/Pause", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/Archer", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/Barracks", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/Mage", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/Ability", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/Cancel", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "BottomBar/NextWave", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "SelectedPanel/Upgrade", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "SelectedPanel/Sell", 44f);
            AssertTouchTarget(catalog.hudPrefab.transform, "ResultPanel/Restart", 44f);
        }

        [Test]
        public void BuildButtonStateContractSeparatesNoSlotAffordableAndUnaffordable()
        {
            Color accent = Color.yellow;
            DefenderHudController.BuildButtonPresentationSpec noSlot =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, false, 200, accent);
            Assert.That(noSlot.state, Is.EqualTo(DefenderHudController.BuildButtonAvailabilityState.NoSlot));
            Assert.That(noSlot.interactable, Is.False);
            Assert.That(noSlot.title, Is.EqualTo("OKÇU"));
            Assert.That(noSlot.detail, Does.Contain("75"));
            Assert.That(noSlot.warningStripe, Is.False);

            DefenderHudController.BuildButtonPresentationSpec affordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 75, accent);
            Assert.That(affordable.state, Is.EqualTo(DefenderHudController.BuildButtonAvailabilityState.Affordable));
            Assert.That(affordable.interactable, Is.True);
            Assert.That(affordable.title, Is.EqualTo("OKÇU"));
            Assert.That(affordable.detail, Is.EqualTo("75"));
            Assert.That(affordable.warningStripe, Is.False);

            DefenderHudController.BuildButtonPresentationSpec unaffordable =
                DefenderHudController.BuildButtonSpec("OKÇU", 75, true, 40, accent);
            Assert.That(unaffordable.state, Is.EqualTo(DefenderHudController.BuildButtonAvailabilityState.Unaffordable));
            Assert.That(unaffordable.interactable, Is.False);
            Assert.That(unaffordable.title, Is.EqualTo("OKÇU"));
            Assert.That(unaffordable.detail, Does.Contain("35"));
            Assert.That(unaffordable.warningStripe, Is.True);
        }

        [Test]
        public void TopBarSpecSeparatesStableWarningBossAndTerminalSignals()
        {
            DefenderHudController.TopBarPresentationSpec stable =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 200, 20, 0, 7, GamePhase.Preparing);
            Assert.That(stable.state, Is.EqualTo(DefenderHudController.TopBarSignalState.Stable));
            Assert.That(stable.goldText, Does.Contain("ALTIN"));
            Assert.That(stable.livesText, Does.Contain("KALE"));
            Assert.That(stable.waveText, Does.Contain("0/7"));
            Assert.That(stable.livesWarning, Is.False);
            Assert.That(stable.bossSignal, Is.False);

            DefenderHudController.TopBarPresentationSpec warning =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 80, 5, 2, 7, GamePhase.Wave);
            Assert.That(warning.state, Is.EqualTo(DefenderHudController.TopBarSignalState.Warning));
            Assert.That(warning.livesText, Does.Contain("KALE!"));
            Assert.That(warning.waveText, Does.Contain("3/7"));
            Assert.That(warning.livesWarning, Is.True);

            DefenderHudController.TopBarPresentationSpec boss =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 120, 12, 6, 7, GamePhase.Intermission);
            Assert.That(boss.state, Is.EqualTo(DefenderHudController.TopBarSignalState.Boss));
            Assert.That(boss.waveText, Does.Contain("BOSS"));
            Assert.That(boss.waveText, Does.Contain("6/7"));
            Assert.That(boss.bossSignal, Is.True);

            DefenderHudController.TopBarPresentationSpec terminal =
                DefenderHudController.TopBarSpec("ALTIN", "KALE", "DALGA", 300, 0, 7, 7, GamePhase.Victory);
            Assert.That(terminal.state, Is.EqualTo(DefenderHudController.TopBarSignalState.Terminal));
            Assert.That(terminal.waveText, Does.Contain("7/7"));
        }

        [Test]
        public void PhaseBadgeSpecUsesMotionPulseAndBossPriorityLanguage()
        {
            DefenderHudController.PhaseBadgePresentationSpec preparing =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Preparing, false, false, 0f);
            Assert.That(preparing.hint, Is.EqualTo("DURUM"));
            Assert.That(preparing.label, Is.EqualTo("DALGA ÖNCESİ"));
            Assert.That(preparing.stripeHeight, Is.EqualTo(5f).Within(0.001f));
            Assert.That(preparing.pulseAlpha, Is.Zero);

            DefenderHudController.PhaseBadgePresentationSpec transition =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Wave, false, false, 1f);
            Assert.That(transition.label, Is.EqualTo("SAVUNMA HATTI"));
            Assert.That(transition.stripeHeight, Is.GreaterThan(preparing.stripeHeight));
            Assert.That(transition.pulseAlpha, Is.GreaterThan(0f));

            DefenderHudController.PhaseBadgePresentationSpec boss =
                DefenderHudController.PhaseBadgeSpec(GamePhase.Wave, false, true, 0f);
            Assert.That(boss.hint, Is.EqualTo("KRİTİK"));
            Assert.That(boss.label, Is.EqualTo("BOSS BASKISI"));
            Assert.That(boss.stripeHeight, Is.GreaterThan(preparing.stripeHeight));
            Assert.That(boss.pulseAlpha, Is.GreaterThan(0f));
        }

        [Test]
        public void SelectedPanelSpecSeparatesSlotUpgradeMaxAndDisruptedStates()
        {
            DefenderHudController.SelectedPanelPresentationSpec slot =
                DefenderHudController.SelectedSlotPanelSpec(2);
            Assert.That(slot.state, Is.EqualTo(DefenderHudController.SelectedPanelState.EmptySlot));
            Assert.That(slot.title, Is.EqualTo("KARAKOL YUVASI"));
            Assert.That(slot.badge, Is.EqualTo("#3"));
            Assert.That(slot.upgradeInteractable, Is.False);
            Assert.That(slot.sellInteractable, Is.False);

            DefenderHudController.SelectedPanelPresentationSpec upgrade =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Archer, 1, "Hızlı tek hedef",
                    24, 5.5f, 1.2f, true, 95, 45, false, 0f);
            Assert.That(upgrade.state, Is.EqualTo(DefenderHudController.SelectedPanelState.UpgradeAvailable));
            Assert.That(upgrade.title, Is.EqualTo("OKÇU KARAKOLU"));
            Assert.That(upgrade.badge, Is.EqualTo("SV 1"));
            Assert.That(upgrade.hint, Does.Contain("Yükseltme 95 altın"));
            Assert.That(upgrade.stats, Does.Contain("HASAR"));
            Assert.That(upgrade.upgradeLabel, Is.EqualTo("YÜKSELT\n95"));
            Assert.That(upgrade.sellLabel, Is.EqualTo("SAT\n+45"));
            Assert.That(upgrade.upgradeInteractable, Is.True);
            Assert.That(upgrade.sellInteractable, Is.True);

            DefenderHudController.SelectedPanelPresentationSpec max =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Mage, 2, "Alan kontrolü",
                    18, 4.5f, 0.8f, false, 0, 70, false, 0f);
            Assert.That(max.state, Is.EqualTo(DefenderHudController.SelectedPanelState.MaxLevel));
            Assert.That(max.hint, Does.Contain("Maks seviye"));
            Assert.That(max.upgradeLabel, Is.EqualTo("MAKS\nSV"));
            Assert.That(max.upgradeInteractable, Is.False);
            Assert.That(max.sellInteractable, Is.True);

            DefenderHudController.SelectedPanelPresentationSpec disrupted =
                DefenderHudController.SelectedOutpostPanelSpec(OutpostKind.Barracks, 1, "Yol tutar",
                    10, 3.5f, 0.7f, true, 100, 60, true, 3.2f);
            Assert.That(disrupted.state, Is.EqualTo(DefenderHudController.SelectedPanelState.Disrupted));
            Assert.That(disrupted.role, Does.Contain("BOZULDU 4 s"));
            Assert.That(disrupted.hint, Does.Contain("Geçici baskı altında"));
            Assert.That(disrupted.warningStripe, Is.True);
        }

        [Test]
        public void UltrawideCameraUsesMoreOfTheScreenThanSixteenByNine()
        {
            float standard = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                new Vector2(1920f, 1080f), new Rect(0f, 0f, 1920f, 1080f));
            float ultrawide = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                new Vector2(2868f, 1320f), new Rect(132f, 0f, 2604f, 1320f));

            Assert.That(ultrawide, Is.LessThan(standard));
            Assert.That(ultrawide, Is.GreaterThanOrEqualTo(AdaptiveBattlefieldCamera.MinimumOrthographicSize));
        }

        [Test]
        public void CameraFramingCoversVisualDevelopmentAspectRatios()
        {
            float sixteenByNine = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                new Vector2(1920f, 1080f), new Rect(0f, 0f, 1920f, 1080f));
            float twentyByNine = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                new Vector2(2400f, 1080f), new Rect(0f, 0f, 2400f, 1080f));
            float fourByThree = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                new Vector2(1440f, 1080f), new Rect(0f, 0f, 1440f, 1080f));

            Assert.That(twentyByNine, Is.LessThanOrEqualTo(sixteenByNine));
            Assert.That(fourByThree, Is.GreaterThan(sixteenByNine));
            Assert.That(twentyByNine, Is.GreaterThanOrEqualTo(AdaptiveBattlefieldCamera.MinimumOrthographicSize));
            Assert.That(fourByThree, Is.LessThanOrEqualTo(AdaptiveBattlefieldCamera.MaximumOrthographicSize));
        }

        [Test]
        public void OrientationPolicyKeepsFrontendPortraitAndBattleFamilyLandscape()
        {
            Assert.That(AppFlowController.OrientationForScreen(AppScreen.MainMenu),
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            Assert.That(AppFlowController.OrientationForScreen(AppScreen.Campaign),
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            Assert.That(AppFlowController.OrientationForScreen(AppScreen.Briefing),
                Is.EqualTo(MobileOrientationMode.PortraitFrontend));
            Assert.That(AppFlowController.OrientationForScreen(AppScreen.Battle),
                Is.EqualTo(MobileOrientationMode.LandscapeBattle));
            Assert.That(AppFlowController.OrientationForScreen(AppScreen.Result),
                Is.EqualTo(MobileOrientationMode.LandscapeBattle));

            MobileOrientationSpec battle = MobileOrientationController.SpecFor(MobileOrientationMode.LandscapeBattle);
            Assert.That(battle.Portrait, Is.False);
            Assert.That(battle.LandscapeLeft, Is.True);
            Assert.That(battle.LandscapeRight, Is.True);
            Assert.That(MobileOrientationController.MatchesDimensions(
                MobileOrientationMode.LandscapeBattle, 2532, 1170), Is.True);
            Assert.That(MobileOrientationController.MatchesDimensions(
                MobileOrientationMode.LandscapeBattle, 1170, 2532), Is.False);
        }

        [Test]
        public void SafeAreaInsetsAreClampedBetweenZeroAndOne()
        {
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(132f, 0f, 2268f, 1170f),
                new Vector2Int(2532, 1170));

            Assert.That(metrics.SafeMinX, Is.InRange(0f, 1f));
            Assert.That(metrics.SafeMaxX, Is.InRange(0f, 1f));
            Assert.That(metrics.SafeMinY, Is.InRange(0f, 1f));
            Assert.That(metrics.SafeMaxY, Is.InRange(0f, 1f));
        }

        [Test]
        public void LandscapeLeftAndRightSafeAreasMirrorWithoutChangingHudWidth()
        {
            var landscapeLeft = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(132f, 0f, 2400f, 1170f),
                new Vector2Int(2532, 1170));
            var landscapeRight = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(0f, 0f, 2400f, 1170f),
                new Vector2Int(2532, 1170));

            Assert.That(landscapeLeft.SafeWidth, Is.EqualTo(landscapeRight.SafeWidth).Within(0.01f));
            Assert.That(landscapeLeft.SafeMinX,
                Is.EqualTo(1f - landscapeRight.SafeMaxX).Within(0.0001f));
            Assert.That(landscapeLeft.SafeCenterX,
                Is.EqualTo(1f - landscapeRight.SafeCenterX).Within(0.0001f));
        }

        [Test]
        public void PreviewAndMessageWidthsDoNotExceedSafeWidth()
        {
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(132f, 0f, 2268f, 1170f),
                new Vector2Int(2532, 1170));

            Assert.That(metrics.PreviewWidth, Is.LessThanOrEqualTo(metrics.SafeWidth));
            Assert.That(metrics.MessageWidth, Is.LessThanOrEqualTo(metrics.SafeWidth));
        }

        [Test]
        public void LandscapeHudBannersStayCompactOverTheBattlefield()
        {
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(1920f, 1080f),
                new Rect(80f, 0f, 1760f, 1080f),
                new Vector2Int(1920, 1080));

            Assert.That(metrics.PreviewWidth, Is.InRange(metrics.SafeWidth * 0.30f, metrics.SafeWidth * 0.55f));
            Assert.That(metrics.MessageWidth, Is.InRange(metrics.SafeWidth * 0.35f, metrics.SafeWidth * 0.50f));
        }

        [Test]
        public void LandscapeHudRailsLeaveMostOfTheScreenToTheBattlefield()
        {
            Assert.That(DefenderHudResponsiveLayout.TopContentHeight, Is.InRange(140f, 160f));
            Assert.That(DefenderHudResponsiveLayout.BottomContentHeight, Is.InRange(160f, 180f));
            Assert.That(DefenderHudResponsiveLayout.TopContentHeight +
                        DefenderHudResponsiveLayout.BottomContentHeight,
                Is.LessThan(1080f * 0.30f));
        }

        [Test]
        public void PortraitCampaignPathRunsFromTopToBottom()
        {
            Vector2 previous = AppFlowController.PortraitCampaignPathPosition(0);
            Assert.That(previous.x, Is.InRange(120f, 864f));
            for (int index = 1; index < CampaignCatalog.LevelsPerRegion; index++)
            {
                Vector2 current = AppFlowController.PortraitCampaignPathPosition(index);
                Assert.That(current.y, Is.GreaterThan(previous.y));
                Assert.That(current.x, Is.InRange(120f, 864f));
                previous = current;
            }
        }

        [Test]
        public void TopContentReservesTopInset()
        {
            var noInset = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(0f, 0f, 2532, 1170),
                new Vector2Int(2532, 1170));
            var notched = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(132f, 0f, 2268f, 1100f),
                new Vector2Int(2532, 1170));

            Assert.That(notched.TopInset, Is.GreaterThan(noInset.TopInset));
            Assert.That(notched.BottomInset, Is.EqualTo(noInset.BottomInset));
        }

        [Test]
        public void BottomContentReservesBottomInset()
        {
            var noInset = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(0f, 0f, 2532, 1170),
                new Vector2Int(2532, 1170));
            var notched = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2532f, 1170f),
                new Rect(132f, 70f, 2268f, 1100f),
                new Vector2Int(2532, 1170));

            Assert.That(notched.BottomInset, Is.GreaterThan(noInset.BottomInset));
            Assert.That(notched.TopInset, Is.EqualTo(noInset.TopInset));
        }

        [Test]
        public void SelectedPanelStaysInsideSafeArea()
        {
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(1920f, 1080f),
                new Rect(80f, 40f, 1760f, 1000f),
                new Vector2Int(1920, 1080));

            float panelWidth = Mathf.Min(360f, metrics.SafeWidth * 0.30f);
            float selectedMinX = metrics.SafeMaxX - (18f + panelWidth) / 1920f;
            float selectedMaxX = metrics.SafeMaxX - 18f / 1920f;
            float selectedMinY = metrics.SafeMinY +
                                 (DefenderHudResponsiveLayout.BottomContentHeight + 18f) / 1080f;
            float selectedMaxY = selectedMinY + 340f / 1080f;

            Assert.That(selectedMinX, Is.GreaterThanOrEqualTo(metrics.SafeMinX));
            Assert.That(selectedMaxX, Is.LessThanOrEqualTo(metrics.SafeMaxX));
            Assert.That(selectedMinY, Is.GreaterThanOrEqualTo(metrics.SafeMinY));
            Assert.That(selectedMaxY, Is.LessThanOrEqualTo(metrics.SafeMaxY));
        }

        [Test]
        public void ContextualTowerPanelClampsToBattleSafeArea()
        {
            Vector2 rootSize = new(1920f, 1080f);
            var metrics = DefenderHudResponsiveLayout.CalculateLayoutMetrics(rootSize,
                new Rect(80f, 40f, 1760f, 1000f), new Vector2Int(1920, 1080));
            Vector2 panelSize = new(620f, 210f);

            Vector2 topRight = DefenderHudResponsiveLayout.ClampContextPanelPosition(
                new Vector2(5000f, 5000f), panelSize, rootSize, metrics);
            Vector2 bottomLeft = DefenderHudResponsiveLayout.ClampContextPanelPosition(
                new Vector2(-5000f, -5000f), panelSize, rootSize, metrics);

            float safeLeft = -rootSize.x * 0.5f + metrics.SafeMinX * rootSize.x;
            float safeRight = -rootSize.x * 0.5f + metrics.SafeMaxX * rootSize.x;
            float safeBottom = -rootSize.y * 0.5f + metrics.SafeMinY * rootSize.y +
                               DefenderHudResponsiveLayout.BottomContentHeight;
            float safeTop = -rootSize.y * 0.5f + metrics.SafeMaxY * rootSize.y -
                            DefenderHudResponsiveLayout.TopContentHeight;

            Assert.That(topRight.x + panelSize.x * 0.5f, Is.LessThan(safeRight));
            Assert.That(topRight.y + panelSize.y * 0.5f, Is.LessThan(safeTop));
            Assert.That(bottomLeft.x - panelSize.x * 0.5f, Is.GreaterThan(safeLeft));
            Assert.That(bottomLeft.y - panelSize.y * 0.5f, Is.GreaterThan(safeBottom));
        }

        [Test]
        public void FirstDefenceVisitsCampaignMapBeforeBattle()
        {
            Assert.That(AppFlowController.ShouldOpenCampaignBeforeFirstBattle(0, 1, false), Is.True);
            Assert.That(AppFlowController.ShouldOpenCampaignBeforeFirstBattle(0, 1, true), Is.False);
            Assert.That(AppFlowController.ShouldOpenCampaignBeforeFirstBattle(1, 2, false), Is.False);
        }

        [Test]
        public void TacticalPreviewRepresentsTwoAndThreeEntranceMaps()
        {
            var root = new GameObject("Tactical Preview Test", typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(640f, 260f);
            var preview = root.AddComponent<FrontendTacticalMapPreview>();

            MapDefinition twoEntrances = CampaignLevelFactory.Map(CampaignCatalog.Level(14).mapId);
            preview.Configure(twoEntrances, Color.cyan, null);
            Assert.That(preview.EntranceMarkerCount, Is.EqualTo(2));
            Assert.That(preview.SlotMarkerCount, Is.EqualTo(8));

            MapDefinition threeEntrances = CampaignLevelFactory.Map(CampaignCatalog.Level(43).mapId);
            preview.Configure(threeEntrances, Color.cyan, null);
            Assert.That(preview.EntranceMarkerCount, Is.EqualTo(3));
            Assert.That(preview.SlotMarkerCount, Is.EqualTo(8));
            Assert.That(preview.RouteSegmentCount, Is.GreaterThan(twoEntrances.PrimaryRoute.Length - 1));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TwentyByNineHasWiderSafeContentThanSixteenByNine()
        {
            var sixteenByNine = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(1920f, 1080f),
                new Rect(0f, 0f, 1920, 1080),
                new Vector2Int(1920, 1080));
            var twentyByNine = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(2400f, 1080f),
                new Rect(0f, 0f, 2400, 1080),
                new Vector2Int(2400, 1080));

            Assert.That(twentyByNine.SafeWidth, Is.GreaterThan(sixteenByNine.SafeWidth));
        }

        [Test]
        public void FourByThreeHasNarrowerSafeContentThanSixteenByNine()
        {
            var sixteenByNine = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(1920f, 1080f),
                new Rect(0f, 0f, 1920, 1080),
                new Vector2Int(1920, 1080));
            var fourByThree = DefenderHudResponsiveLayout.CalculateLayoutMetrics(
                new Vector2(1440f, 1080f),
                new Rect(0f, 0f, 1440, 1080),
                new Vector2Int(1440, 1080));

            Assert.That(fourByThree.SafeWidth, Is.LessThan(sixteenByNine.SafeWidth));
        }

        [Test]
        public void CameraFramingReservesUnsafeHorizontalInsets()
        {
            Vector2 screen = new(2868f, 1320f);
            float edgeToEdge = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                screen, new Rect(0f, 0f, screen.x, screen.y));
            float insetSafeArea = AdaptiveBattlefieldCamera.CalculateOrthographicSize(
                screen, new Rect(132f, 0f, 2604f, 1320f));

            Assert.That(insetSafeArea, Is.GreaterThan(edgeToEdge));
        }

        [Test]
        public void AdaptiveCameraConfigureIsIdempotent()
        {
            var cameraObject = new GameObject("Adaptive Camera Idempotence");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(11f, 16f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(38f, 0f, 0f);
            var adaptive = cameraObject.AddComponent<AdaptiveBattlefieldCamera>();

            adaptive.Configure(camera);
            Vector3 firstPosition = cameraObject.transform.position;
            adaptive.Configure(camera);

            Assert.That(cameraObject.transform.position, Is.EqualTo(firstPosition));
            Object.DestroyImmediate(cameraObject);
        }


        private static void AssertTouchTarget(Transform root, string path, float minimumSize)
        {
            Transform node = root.Find(path);
            Assert.That(node, Is.Not.Null, $"HUD target is missing at {path}.");
            Assert.That(node, Is.TypeOf<RectTransform>(), $"HUD target at {path} must be a RectTransform.");
            var rect = (RectTransform)node;
            Assert.That(rect.sizeDelta.x, Is.GreaterThanOrEqualTo(minimumSize),
                $"{path} width fell below the minimum touch target.");
            Assert.That(rect.sizeDelta.y, Is.GreaterThanOrEqualTo(minimumSize),
                $"{path} height fell below the minimum touch target.");
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

#if UNITY_EDITOR
        private static AnimatorStateTransition AnyStateTransition(AnimatorStateMachine machine, string trigger)
        {
            return machine.anyStateTransitions.FirstOrDefault(transition =>
                transition.conditions.Any(condition => condition.parameter == trigger));
        }
#endif
    }
}
