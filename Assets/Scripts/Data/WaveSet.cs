using UnityEngine;

namespace AllinDefense.Data
{
    [CreateAssetMenu(fileName = "WaveSet", menuName = "AllinDefense/Wave Set")]
    public class WaveSet : ScriptableObject
    {
        [SerializeField] private WaveData[] waves;
        public WaveData[] Waves => waves;
    }
}
