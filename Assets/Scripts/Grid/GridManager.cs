using System.Collections.Generic;
using UnityEngine;

namespace DefenderChronicles.Grid
{
    public enum CellType
    {
        Empty,
        Path,
        Tower,
        Blocked
    }

    [System.Serializable]
    public class GridCell
    {
        public Vector2Int position;
        public CellType cellType;
        public GameObject occupant;

        public GridCell(Vector2Int pos, CellType type = CellType.Empty)
        {
            position = pos;
            cellType = type;
            occupant = null;
        }

        public bool IsAvailable => cellType == CellType.Empty && occupant == null;
    }

    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 12;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Visual")]
        [SerializeField] private GameObject cellHighlightPrefab;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.3f);

        private GridCell[,] grid;
        private GameObject currentHighlight;

        public int Width => gridWidth;
        public int Height => gridHeight;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            grid = new GridCell[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    grid[x, y] = new GridCell(new Vector2Int(x, y));
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return gridOrigin + new Vector3(x * cellSize + cellSize * 0.5f, y * cellSize + cellSize * 0.5f, 0);
        }

        public Vector2Int GetGridPosition(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
            return new Vector2Int(x, y);
        }

        public bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
        }

        public GridCell GetCell(int x, int y)
        {
            if (!IsValidCell(x, y)) return null;
            return grid[x, y];
        }

        public GridCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public bool PlaceTower(int x, int y, GameObject tower)
        {
            var cell = GetCell(x, y);
            if (cell == null || !cell.IsAvailable) return false;

            cell.cellType = CellType.Tower;
            cell.occupant = tower;
            tower.transform.position = GetWorldPosition(x, y);
            return true;
        }

        public bool RemoveTower(int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell == null || cell.cellType != CellType.Tower) return false;

            cell.cellType = CellType.Empty;
            cell.occupant = null;
            return true;
        }

        /// <summary>Finds the cell that holds this occupant (e.g. sold tower) and clears it.</summary>
        public bool TryReleaseOccupant(GameObject occupant)
        {
            if (occupant == null || grid == null) return false;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    var c = grid[x, y];
                    if (c.occupant != occupant) continue;
                    c.cellType = CellType.Empty;
                    c.occupant = null;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Creates a default highlight template when no prefab is assigned (runtime bootstrap).</summary>
        public void EnsureRuntimeHighlightTemplate()
        {
            if (cellHighlightPrefab != null) return;

            var template = new GameObject("_CellHighlightTemplate");
            template.hideFlags = HideFlags.HideAndDontSave;
            template.SetActive(false);
            var sr = template.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            float ppu = tex.width / Mathf.Max(0.01f, cellSize);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
            sr.color = validColor;
            cellHighlightPrefab = template;
        }

        public void SetCellType(int x, int y, CellType type)
        {
            var cell = GetCell(x, y);
            if (cell != null) cell.cellType = type;
        }

        public void ShowHighlight(Vector3 worldPos, bool isValid)
        {
            if (cellHighlightPrefab == null) return;

            var gridPos = GetGridPosition(worldPos);
            if (!IsValidCell(gridPos.x, gridPos.y)) return;

            if (currentHighlight == null)
                currentHighlight = Instantiate(cellHighlightPrefab);

            currentHighlight.SetActive(true);
            currentHighlight.transform.position = GetWorldPosition(gridPos.x, gridPos.y);

            var sr = currentHighlight.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = isValid ? validColor : invalidColor;
        }

        public void HideHighlight()
        {
            if (currentHighlight != null)
                currentHighlight.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 1, 1, 0.2f);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 pos = gridOrigin + new Vector3(x * cellSize, y * cellSize, 0);
                    Gizmos.DrawWireCube(pos + new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0),
                                        new Vector3(cellSize, cellSize, 0));
                }
            }
        }
    }
}
