using UnityEngine;

namespace AllinDefense.UI
{
    public sealed class FrontendOrientationWait
    {
        public const float TimeoutSeconds = 3f;
        private readonly double started;
        private Rect previousSafeArea;
        private Vector2Int previousSize;
        private int stableFrames;
        public bool TimedOut { get; private set; }
        public bool Ready { get; private set; }

        public FrontendOrientationWait(double now) => started = now;

        public bool Tick(double now, bool matches, Vector2Int size, Rect safeArea)
        {
            bool valid = matches && safeArea.width > 0 && safeArea.height > 0 &&
                         safeArea.xMin >= 0 && safeArea.yMin >= 0 &&
                         safeArea.xMax <= size.x && safeArea.yMax <= size.y;
            stableFrames = valid && size == previousSize && safeArea == previousSafeArea ? stableFrames + 1 : 0;
            previousSize = size; previousSafeArea = safeArea;
            Ready = stableFrames >= 2;
            TimedOut = !Ready && now - started >= TimeoutSeconds;
            return Ready || TimedOut;
        }
    }
}
