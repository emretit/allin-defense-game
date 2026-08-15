#if UNITY_EDITOR
using System;
using System.IO;
using DefenderChronicles.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
    }
}
#endif
