using UnityEngine;

namespace DefenderChronicles.Core
{
    /// <summary>
    /// Minimal scene entry point. The playable greybox owns its authored runtime
    /// hierarchy and does not depend on optional TMP resources or Inspector wiring.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameBootstrap : MonoBehaviour
    {
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

            session.Initialize();
            RuntimeSceneReady = true;
        }
    }
}
