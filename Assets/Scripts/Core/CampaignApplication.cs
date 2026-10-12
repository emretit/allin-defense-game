using UnityEngine;

namespace AllinDefense.Core
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
            Progress.Changed += RecordLocalSaveFailure;
            Services = gameObject.AddComponent<UnityGameServicesController>();
        }
        private float nextLocalRetry;
        private void Update()
        {
            if (Progress == null || !Progress.HasWriteError || Time.realtimeSinceStartup < nextLocalRetry) return;
            nextLocalRetry = Time.realtimeSinceStartup + 10;
            Progress.Save();
        }
        private bool reportedWriteFailure;
        private void RecordLocalSaveFailure()
        {
            if (Progress.HasWriteError && !reportedWriteFailure)
                GameSessionController.Instance?.RecordSaveFailure();
            reportedWriteFailure = Progress.HasWriteError;
        }
        private void OnDestroy()
        {
            if (Progress != null) Progress.Changed -= RecordLocalSaveFailure;
            if (instance == this) instance = null;
        }
    }
}
