using UnityEngine;
using UnityEngine.InputSystem;
using AllinDefense.Core;
using AllinDefense.Grid;
using AllinDefense.Data;
using AllinDefense.Utils;

namespace AllinDefense.Towers
{
    public class TowerPlacer : MonoBehaviour
    {
        public static TowerPlacer Instance { get; private set; }

        [Header("Tower Prefabs")]
        [SerializeField] private GameObject archerPrefab;
        [SerializeField] private GameObject magePrefab;
        [SerializeField] private GameObject warriorPrefab;
        [SerializeField] private GameObject catapultPrefab;

        [Header("Tower Costs")]
        [SerializeField] private int archerCost = 75;
        [SerializeField] private int mageCost = 125;
        [SerializeField] private int warriorCost = 100;
        [SerializeField] private int catapultCost = 150;

        private TowerType? selectedTowerType = null;
        private bool isPlacingTower = false;

        public bool IsPlacingTower => isPlacingTower;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>Loads tower prefabs from Resources when Inspector slots are empty.</summary>
        public void ApplyRuntimeDefaultsFromResources()
        {
            if (archerPrefab == null)
                archerPrefab = Resources.Load<GameObject>("Prefabs/Towers/ArcherTower");
            if (magePrefab == null)
                magePrefab = Resources.Load<GameObject>("Prefabs/Towers/MageTower");
            if (warriorPrefab == null)
                warriorPrefab = Resources.Load<GameObject>("Prefabs/Towers/WarriorTower");
            if (catapultPrefab == null)
                catapultPrefab = Resources.Load<GameObject>("Prefabs/Towers/CatapultTower");
        }

        private void Update()
        {
            if (!isPlacingTower) return;
            HandlePlacementPreview();
            HandlePlacementInput();
        }

        public void SelectTower(TowerType type)
        {
            selectedTowerType = type;
            isPlacingTower = true;
        }

        public void CancelPlacement()
        {
            selectedTowerType = null;
            isPlacingTower = false;
            GridManager.Instance?.HideHighlight();
        }

        private static Vector3 GetPointerWorldPosition()
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector3.zero;

            Vector2 screen;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null)
                screen = Mouse.current.position.ReadValue();
            else
                screen = Vector2.zero;

            Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, cam.nearClipPlane + 1f));
            w.z = 0f;
            return w;
        }

        private static bool GetPrimaryPointerDownThisFrame()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            return false;
        }

        private void HandlePlacementPreview()
        {
            Vector3 worldPos = GetPointerWorldPosition();

            var slots = OutpostSlotManager.Instance;
            if (slots != null && slots.SlotCount > 0)
            {
                bool canPlaceInSlot = slots.TryGetNearestAvailableSlot(worldPos, out var slot);
                GridManager.Instance?.ShowHighlight(canPlaceInSlot ? slot.Position : worldPos, canPlaceInSlot);
                return;
            }

            var grid = GridManager.Instance;
            if (grid == null) return;

            var gridPos = grid.GetGridPosition(worldPos);
            var cell = grid.GetCell(gridPos);
            bool canPlace = cell != null && cell.IsAvailable;

            grid.ShowHighlight(worldPos, canPlace);
        }

        private void HandlePlacementInput()
        {
            if (GetPrimaryPointerDownThisFrame())
            {
                Vector3 worldPos = GetPointerWorldPosition();
                TryPlaceTower(worldPos);
            }

            bool cancel =
                (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
            if (cancel)
                CancelPlacement();
        }

        private void TryPlaceTower(Vector3 worldPos)
        {
            if (!selectedTowerType.HasValue) return;

            int cost = GetCost(selectedTowerType.Value);
            var prefab = GetPrefab(selectedTowerType.Value);
            if (prefab == null) return;

            if (!GameManager.Instance.SpendGold(cost)) return;

            var tower = Instantiate(prefab);
            bool placed = false;

            var slots = OutpostSlotManager.Instance;
            if (slots != null && slots.SlotCount > 0)
            {
                placed = slots.TryPlaceOutpost(worldPos, tower);
            }
            else
            {
                var grid = GridManager.Instance;
                if (grid != null)
                {
                    var gridPos = grid.GetGridPosition(worldPos);
                    placed = grid.PlaceTower(gridPos.x, gridPos.y, tower);
                }
            }

            if (!placed)
            {
                GameManager.Instance.EarnGold(cost);
                Destroy(tower);
            }
            else
            {
                AudioManager.Instance?.PlayTowerPlace();
                CancelPlacement();
            }
        }

        private GameObject GetPrefab(TowerType type) => type switch
        {
            TowerType.Archer   => archerPrefab,
            TowerType.Mage     => magePrefab,
            TowerType.Warrior  => warriorPrefab,
            TowerType.Catapult => catapultPrefab,
            _                  => null
        };

        private int GetCost(TowerType type) => type switch
        {
            TowerType.Archer   => archerCost,
            TowerType.Mage     => mageCost,
            TowerType.Warrior  => warriorCost,
            TowerType.Catapult => catapultCost,
            _                  => 0
        };
    }
}
