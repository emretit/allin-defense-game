using UnityEngine;

namespace DefenderChronicles.Core
{
    /// <summary>Owns profile and network work across battle/front-end scene reloads.</summary>
    public sealed class CampaignApplication : MonoBehaviour
    {
        private static CampaignApplication instance;
        public CampaignProgressStore Progress { get; private set; }
        public UnityGameServicesController Services { get; private set; }
        public static CampaignApplication GetOrCreate()
        {
            if (instance != null) return instance;
            return new GameObject("Campaign Application").AddComponent<CampaignApplication>();
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Progress = new CampaignProgressStore();
            Services = gameObject.AddComponent<UnityGameServicesController>();
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
