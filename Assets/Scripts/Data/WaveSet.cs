using UnityEngine;

namespace DefenderChronicles.Data
{
    [CreateAssetMenu(fileName = "WaveSet", menuName = "DefenderChronicles/Wave Set")]
    public class WaveSet : ScriptableObject
    {
        [SerializeField] private WaveData[] waves;
        public WaveData[] Waves => waves;
    }
}
