using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameSetupHelper : MonoBehaviour
{
    [Header("Board Visualization")]
    public bool showTilePositions = true;
    public bool showSpecialTilePositions = true;
    
    [Header("Witness NPC Position")]
    public bool showWitnessPosition = true;
    
    private BoardManager boardManager;
    
    private void OnValidate()
    {
        boardManager = GetComponent<BoardManager>();
    }
    
    private void OnDrawGizmos()
    {
        if (boardManager == null)
            boardManager = GetComponent<BoardManager>();
            
        if (boardManager == null)
            return;
            
        if (showTilePositions)
        {
            DrawBoardGrid();
        }
        
        if (showSpecialTilePositions)
        {
            DrawSpecialTilePositions();
        }
        
        if (showWitnessPosition)
        {
            DrawWitnessPosition();
        }
    }
    
    private void DrawBoardGrid()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        
        int size = boardManager.boardSize;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector3 pos = new Vector3(x * boardManager.tileSpacing, 0, y * boardManager.tileSpacing);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);
                
                #if UNITY_EDITOR
                Handles.Label(pos + Vector3.up * 0.5f, $"({x},{y})", new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.white },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                });
                #endif
            }
        }
    }
    
    private void DrawSpecialTilePositions()
    {
        DrawSpecialTile(new Vector2Int(2, 2), Color.magenta, "WITNESS");
        DrawSpecialTile(new Vector2Int(0, 0), new Color(0.6f, 0.4f, 0.2f), "RUINS");
        DrawSpecialTile(new Vector2Int(4, 4), Color.yellow, "RELIC");
        DrawSpecialTile(new Vector2Int(0, 4), Color.cyan, "ALTAR");
        DrawSpecialTile(new Vector2Int(4, 0), Color.red, "COMBAT");
    }
    
    private void DrawSpecialTile(Vector2Int gridPos, Color color, string label)
    {
        Vector3 worldPos = new Vector3(gridPos.x * boardManager.tileSpacing, 0, gridPos.y * boardManager.tileSpacing);
        
        Gizmos.color = color;
        Gizmos.DrawCube(worldPos, Vector3.one * 0.8f);
        
        Gizmos.color = new Color(color.r, color.g, color.b, 0.5f);
        Gizmos.DrawWireCube(worldPos, Vector3.one * 1.2f);
        
        #if UNITY_EDITOR
        Handles.Label(worldPos + Vector3.up, label, new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = color },
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        #endif
    }
    
    private void DrawWitnessPosition()
    {
        Vector3 witnessPos = new Vector3(2 * boardManager.tileSpacing, 1, 2 * boardManager.tileSpacing);
        
        Gizmos.color = new Color(1f, 0f, 1f, 0.7f);
        Gizmos.DrawSphere(witnessPos, 0.5f);
        Gizmos.DrawWireSphere(witnessPos, 3f);
        
        #if UNITY_EDITOR
        Handles.Label(witnessPos + Vector3.up * 1.5f, "WITNESS NPC\n(Place here)", new GUIStyle()
        {
            normal = new GUIStyleState() { textColor = Color.magenta },
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        #endif
    }
}
