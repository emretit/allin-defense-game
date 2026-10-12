using UnityEngine;

namespace AllinDefense.Core
{
    /// <summary>Optional path layouts for multi-map support (see README). Index 0 matches GameBootstrap default.</summary>
    public static class MapPathPresets
    {
        public static int Count => Paths.Length;

        public static Vector3[][] Paths { get; } =
        {
            new[]
            {
                new Vector3(-2f, 0.5f, 0f),
                new Vector3(4f, 0.5f, 0f),
                new Vector3(4f, 5f, 0f),
                new Vector3(11f, 5f, 0f),
                new Vector3(11f, 1.5f, 0f),
                new Vector3(17f, 1.5f, 0f),
                new Vector3(17f, 7f, 0f),
                new Vector3(21f, 7f, 0f)
            },
            new[]
            {
                new Vector3(-2f, 6f, 0f),
                new Vector3(6f, 6f, 0f),
                new Vector3(6f, 1f, 0f),
                new Vector3(14f, 1f, 0f),
                new Vector3(14f, 8f, 0f),
                new Vector3(22f, 8f, 0f)
            },
            new[]
            {
                new Vector3(-1f, 1f, 0f),
                new Vector3(10f, 1f, 0f),
                new Vector3(10f, 6f, 0f),
                new Vector3(3f, 6f, 0f),
                new Vector3(3f, 10f, 0f),
                new Vector3(20f, 10f, 0f),
                new Vector3(20f, 2f, 0f)
            }
        };

        public static Vector3[] Get(int index)
        {
            if (index < 0 || index >= Paths.Length)
                return Paths[0];
            return Paths[index];
        }
    }
}
