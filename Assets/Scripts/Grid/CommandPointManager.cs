using UnityEngine;

namespace DefenderChronicles.Grid
{
    public class CommandPointManager : MonoBehaviour
    {
        public static CommandPointManager Instance { get; private set; }

        [SerializeField] private float selectionRadius = 0.9f;
        [SerializeField] private CommandPoint[] commandPoints;

        public int PointCount => commandPoints == null ? 0 : commandPoints.Length;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultCommandPoints();
        }

        public bool TryGetNearestPoint(Vector3 worldPosition, out CommandPoint point)
        {
            point = null;
            if (commandPoints == null || commandPoints.Length == 0)
                return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < commandPoints.Length; i++)
            {
                var candidate = commandPoints[i];
                if (candidate == null)
                    continue;

                float distance = Vector3.Distance(worldPosition, candidate.Position);
                if (distance > selectionRadius || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                point = candidate;
            }

            return point != null;
        }

        private void EnsureDefaultCommandPoints()
        {
            if (commandPoints != null && commandPoints.Length > 0)
                return;

            commandPoints = new[]
            {
                new CommandPoint("cmd_west_hold", new Vector3(4.0f, 1.4f, 0f), CommandPointRole.FrontlineHold),
                new CommandPoint("cmd_mid_hold", new Vector3(10.8f, 4.3f, 0f), CommandPointRole.FrontlineHold),
                new CommandPoint("cmd_gate_hold", new Vector3(16.9f, 2.4f, 0f), CommandPointRole.FrontlineHold),
                new CommandPoint("cmd_boss_focus", new Vector3(18.8f, 6.8f, 0f), CommandPointRole.BossFocus)
            };
        }

        private void OnDrawGizmos()
        {
            if (commandPoints == null)
                return;

            Gizmos.color = Color.green;
            for (int i = 0; i < commandPoints.Length; i++)
            {
                var point = commandPoints[i];
                if (point == null)
                    continue;

                Gizmos.DrawWireSphere(point.Position, selectionRadius);
                Gizmos.DrawSphere(point.Position, 0.18f);
            }
        }
    }

    public enum CommandPointRole
    {
        FrontlineHold,
        BossFocus
    }

    [System.Serializable]
    public class CommandPoint
    {
        [SerializeField] private string id;
        [SerializeField] private Vector3 position;
        [SerializeField] private CommandPointRole role;

        public CommandPoint(string id, Vector3 position, CommandPointRole role)
        {
            this.id = id;
            this.position = position;
            this.role = role;
        }

        public string Id => id;
        public Vector3 Position => position;
        public CommandPointRole Role => role;
    }
}
