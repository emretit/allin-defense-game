using UnityEngine;
using DefenderChronicles.Presentation;

namespace DefenderChronicles.Core
{
    /// <summary>
    /// Minimal production entry point. The scene must explicitly provide the
    /// validated presentation catalog before the gameplay session is initialized.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private DefenderPresentationCatalog presentationCatalog;
        public static bool RuntimeSceneReady { get; private set; }

        private void Awake()
        {
            RuntimeSceneReady = false;

            var session = FindAnyObjectByType<GameSessionController>();
            if (session == null)
            {
                var root = new GameObject("DefenderChroniclesSession");
                session = root.AddComponent<GameSessionController>();
            }

            if (presentationCatalog == null)
                throw new System.InvalidOperationException("Production presentation catalog is missing from GameBootstrap.");
            session.ConfigurePresentation(presentationCatalog);
            session.Initialize();
            RuntimeSceneReady = true;
        }
    }
}
