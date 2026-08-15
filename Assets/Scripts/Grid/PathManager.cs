using System.Collections.Generic;
using UnityEngine;
using DefenderChronicles.Grid;

namespace DefenderChronicles.Core
{
    public class PathManager : MonoBehaviour
    {
        public static PathManager Instance { get; private set; }

        [Header("Path Waypoints")]
        [SerializeField] private Transform[] waypoints;

        private List<Vector3> pathPoints = new List<Vector3>();
        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> cumulativeDistances = new List<float>();
        private float totalPathLength = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildPath();
        }

        /// <summary>Creates waypoint transforms as children and rebuilds the path (used by GameBootstrap).</summary>
        public void ConfigureRuntimeWaypoints(Vector3[] points)
        {
            if (points == null || points.Length < 2) return;

            if (waypoints != null)
            {
                foreach (var w in waypoints)
                {
                    if (w != null)
                        Destroy(w.gameObject);
                }
            }

            waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var go = new GameObject($"Waypoint_{i + 1:00}");
                go.transform.SetParent(transform, false);
                go.transform.position = points[i];
                waypoints[i] = go.transform;
            }

            RebuildPath();
        }

        public int GetWaypointTransformCount() =>
            waypoints == null ? 0 : waypoints.Length;

        /// <summary>Applies a preset path from <see cref="MapPathPresets"/> (multi-map stub).</summary>
        public void ApplyMapPreset(int presetIndex)
        {
            ConfigureRuntimeWaypoints(MapPathPresets.Get(presetIndex));
        }

        public void RebuildPath()
        {
            BuildPath();
        }

        private void BuildPath()
        {
            pathPoints.Clear();
            segmentLengths.Clear();
            cumulativeDistances.Clear();
            totalPathLength = 0f;

            if (waypoints == null) return;

            foreach (var wp in waypoints)
            {
                if (wp != null)
                    pathPoints.Add(wp.position);
            }

            cumulativeDistances.Add(0f);
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                float length = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
                segmentLengths.Add(length);
                totalPathLength += length;
                cumulativeDistances.Add(totalPathLength);
            }

            // Mark path cells on grid
            if (GridManager.Instance != null)
            {
                for (int i = 0; i < pathPoints.Count - 1; i++)
                {
                    MarkPathSegment(pathPoints[i], pathPoints[i + 1]);
                }
            }
        }

        private void MarkPathSegment(Vector3 from, Vector3 to)
        {
            var grid = GridManager.Instance;
            var startCell = grid.GetGridPosition(from);
            var endCell = grid.GetGridPosition(to);

            int x = startCell.x;
            int y = startCell.y;

            while (x != endCell.x || y != endCell.y)
            {
                grid.SetCellType(x, y, CellType.Path);

                if (x < endCell.x) x++;
                else if (x > endCell.x) x--;
                else if (y < endCell.y) y++;
                else if (y > endCell.y) y--;
            }
            grid.SetCellType(endCell.x, endCell.y, CellType.Path);
        }

        public List<Vector3> GetPath() => pathPoints;

        public Vector3 GetWaypointAt(int index)
        {
            if (index < 0 || index >= pathPoints.Count)
                return Vector3.zero;
            return pathPoints[index];
        }

        public int GetWaypointCount() => pathPoints.Count;

        public float GetRouteProgress(Vector3 position, int targetWaypointIndex)
        {
            if (pathPoints.Count < 2 || totalPathLength <= 0f)
                return 0f;

            if (targetWaypointIndex <= 0)
                return 0f;

            if (targetWaypointIndex >= pathPoints.Count)
                return totalPathLength;

            int segmentIndex = targetWaypointIndex - 1;
            Vector3 from = pathPoints[segmentIndex];
            Vector3 to = pathPoints[targetWaypointIndex];
            Vector3 segment = to - from;
            float segmentLength = segmentLengths[segmentIndex];

            if (segmentLength <= 0.0001f)
                return cumulativeDistances[segmentIndex];

            float t = Vector3.Dot(position - from, segment) / (segmentLength * segmentLength);
            t = Mathf.Clamp01(t);
            return cumulativeDistances[segmentIndex] + segmentLength * t;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            Gizmos.color = Color.red;
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    Gizmos.DrawSphere(wp.position, 0.2f);
            }
        }
    }
}
