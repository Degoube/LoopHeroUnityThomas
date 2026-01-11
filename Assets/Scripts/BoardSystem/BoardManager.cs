using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Board Configuration")]
    public int boardSize = 7;
    public float tileSpacing = 1.1f;

    [Header("Tile Prefabs")]
    public GameObject tilePrefab;
    public TileData[] availableTileTypes;

    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public int seed = 0;

    private Dictionary<Vector2Int, BoardTile> tileGrid = new Dictionary<Vector2Int, BoardTile>();
    private List<BoardTile> pathTiles = new List<BoardTile>();
    
    public int TotalTiles => pathTiles.Count;

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
        if (generateOnStart)
        {
            GenerateBoard();
        }
    }

    public void GenerateBoard()
    {
        ClearBoard();

        if (seed != 0)
        {
            UnityEngine.Random.InitState(seed);
        }

        GenerateSquareLoop();
        EnsureSpecialTiles();

        Debug.Log($"Generated loop board: {pathTiles.Count} tiles in path");
    }

    private void GenerateSquareLoop()
    {
        pathTiles.Clear();
        int pathIndex = 0;

        int size = boardSize;
        
        for (int x = 0; x < size; x++)
        {
            CreatePathTile(new Vector2Int(x, 0), pathIndex++);
        }

        for (int y = 1; y < size; y++)
        {
            CreatePathTile(new Vector2Int(size - 1, y), pathIndex++);
        }

        for (int x = size - 2; x >= 0; x--)
        {
            CreatePathTile(new Vector2Int(x, size - 1), pathIndex++);
        }

        for (int y = size - 2; y > 0; y--)
        {
            CreatePathTile(new Vector2Int(0, y), pathIndex++);
        }
    }

    private void CreatePathTile(Vector2Int gridPosition, int pathIndex)
    {
        Vector3 worldPosition = new Vector3(gridPosition.x * tileSpacing, 0, gridPosition.y * tileSpacing);
        
        GameObject tileObject = Instantiate(tilePrefab, worldPosition, Quaternion.identity, transform);
        tileObject.name = $"Tile_{pathIndex:D2}_{gridPosition.x}_{gridPosition.y}";

        BoardTile tile = tileObject.GetComponent<BoardTile>();
        if (tile != null)
        {
            tile.gridPosition = gridPosition;
            tile.pathIndex = pathIndex;
            tile.tileData = GetRandomTileData();
            tile.InitializeTile();
            
            tileGrid[gridPosition] = tile;
            pathTiles.Add(tile);
        }
    }

    private TileData GetRandomTileData()
    {
        if (availableTileTypes == null || availableTileTypes.Length == 0)
            return null;

        int randomIndex = UnityEngine.Random.Range(0, availableTileTypes.Length);
        return availableTileTypes[randomIndex];
    }

    public void ClearBoard()
    {
        foreach (var tile in tileGrid.Values)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }

        tileGrid.Clear();
        pathTiles.Clear();
    }

    public BoardTile GetTileAt(Vector2Int position)
    {
        tileGrid.TryGetValue(position, out BoardTile tile);
        return tile;
    }

    public BoardTile GetTileByPathIndex(int index)
    {
        if (index < 0 || index >= pathTiles.Count)
            return null;
        
        return pathTiles[index];
    }

    public int GetNextPathIndex(int currentIndex, int steps)
    {
        if (pathTiles.Count == 0)
            return 0;
        
        return (currentIndex + steps) % pathTiles.Count;
    }

    public List<BoardTile> GetNeighbors(Vector2Int position)
    {
        List<BoardTile> neighbors = new List<BoardTile>();

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = position + dir;
            BoardTile neighbor = GetTileAt(neighborPos);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public List<BoardTile> GetTilesByType(TileType type)
    {
        List<BoardTile> tiles = new List<BoardTile>();

        foreach (BoardTile tile in tileGrid.Values)
        {
            if (tile.tileData != null && tile.tileData.tileType == type)
            {
                tiles.Add(tile);
            }
        }

        return tiles;
    }

    public void EnsureSpecialTiles()
    {
        int totalTiles = pathTiles.Count;
        List<int> usedIndices = new List<int>();
        
        PlaceMultipleTilesWithSpacing(4, GetTileDataByType(TileType.Witness), usedIndices, 2, totalTiles);
        PlaceMultipleTilesWithSpacing(3, GetTileDataByType(TileType.Combat), usedIndices, 2, totalTiles);
        PlaceMultipleTilesWithSpacing(2, GetTileDataByType(TileType.Ruins), usedIndices, 2, totalTiles);
        PlaceMultipleTilesWithSpacing(2, GetTileDataByType(TileType.Altar), usedIndices, 2, totalTiles);
        PlaceMultipleTilesWithSpacing(3, GetTileDataByType(TileType.Relic), usedIndices, 2, totalTiles);
        
        FillRemainingWithEmpty(usedIndices);
        
        Debug.Log($"Special tiles placed: 4 Witness, 3 Combat, 2 Ruins, 2 Altar, 3 Relic, {totalTiles - usedIndices.Count} Empty");
    }
    
    private void PlaceMultipleTilesWithSpacing(int count, TileData tileData, List<int> usedIndices, int minSpacing, int totalTiles)
    {
        if (tileData == null || count <= 0) return;
        
        int spacing = totalTiles / (count + 1);
        
        for (int i = 0; i < count; i++)
        {
            int preferredIndex = spacing * (i + 1);
            int finalIndex = preferredIndex;
            
            if (IsIndexTooCloseToUsed(preferredIndex, usedIndices, minSpacing, totalTiles))
            {
                finalIndex = FindValidIndexNear(preferredIndex, usedIndices, minSpacing, totalTiles);
            }
            
            usedIndices.Add(finalIndex);
            PlaceSpecificTileAtPath(finalIndex, tileData);
        }
    }
    
    private void FillRemainingWithEmpty(List<int> usedIndices)
    {
        TileData emptyTileData = GetTileDataByType(TileType.Empty);
        if (emptyTileData == null) return;
        
        for (int i = 0; i < pathTiles.Count; i++)
        {
            if (!usedIndices.Contains(i))
            {
                PlaceSpecificTileAtPath(i, emptyTileData);
            }
        }
    }
    
    private bool IsIndexTooCloseToUsed(int index, List<int> usedIndices, int minSpacing, int totalTiles)
    {
        foreach (int used in usedIndices)
        {
            int distance = Mathf.Min(
                Mathf.Abs(index - used),
                totalTiles - Mathf.Abs(index - used)
            );
            
            if (distance < minSpacing)
            {
                return true;
            }
        }
        return false;
    }
    
    private int FindValidIndexNear(int preferredIndex, List<int> usedIndices, int minSpacing, int totalTiles)
    {
        for (int offset = 1; offset < totalTiles; offset++)
        {
            int candidate1 = (preferredIndex + offset) % totalTiles;
            if (!IsIndexTooCloseToUsed(candidate1, usedIndices, minSpacing, totalTiles))
            {
                return candidate1;
            }
            
            int candidate2 = (preferredIndex - offset + totalTiles) % totalTiles;
            if (!IsIndexTooCloseToUsed(candidate2, usedIndices, minSpacing, totalTiles))
            {
                return candidate2;
            }
        }
        
        return preferredIndex;
    }

    private void PlaceSpecificTileAtPath(int pathIndex, TileData tileData)
    {
        BoardTile tile = GetTileByPathIndex(pathIndex);
        if (tile != null && tileData != null)
        {
            tile.tileData = tileData;
            tile.InitializeTile();
            
            Debug.Log($"Placed {tileData.tileType} at path index {pathIndex}");
        }
    }

    private TileData GetTileDataByType(TileType type)
    {
        if (availableTileTypes == null)
            return null;

        foreach (TileData data in availableTileTypes)
        {
            if (data != null && data.tileType == type)
                return data;
        }
        
        Debug.LogWarning($"No TileData found for type: {type}");
        return null;
    }
}
