using UnityEngine;
using UnityEngine.EventSystems;
using DefenderChronicles.Towers;
using DefenderChronicles.Utils;

namespace DefenderChronicles.UI
{
    /// <summary>Selects towers in the world via <see cref="TouchInputHandler.OnTap"/>.</summary>
    public class TowerWorldTapHandler : MonoBehaviour
    {
        [SerializeField] private float pickRadius = 0.55f;

        private void Start()
        {
            var touch = GetComponent<TouchInputHandler>();
            if (touch != null)
                touch.OnTap.AddListener(OnWorldTap);
        }

        private void OnDestroy()
        {
            var touch = GetComponent<TouchInputHandler>();
            if (touch != null)
                touch.OnTap.RemoveListener(OnWorldTap);
        }

        private void OnWorldTap(Vector3 world)
        {
            if (TowerPlacer.Instance != null && TowerPlacer.Instance.IsPlacingTower)
                return;
            if (IsPointerOverUi())
                return;

            TowerBase best = null;
            float bestSqr = pickRadius * pickRadius;
            foreach (var t in FindObjectsByType<TowerBase>(FindObjectsSortMode.None))
            {
                float s = (t.transform.position - world).sqrMagnitude;
                if (s <= bestSqr)
                {
                    bestSqr = s;
                    best = t;
                }
            }

            if (best != null)
                TowerInfoPanel.Instance?.ShowTowerInfo(best);
            else
                TowerInfoPanel.Instance?.HidePanel();
        }

        private static bool IsPointerOverUi()
        {
            var es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }
    }
}
