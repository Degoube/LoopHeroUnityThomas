using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates save and load operations.
/// Knows how to collect data from all managers → SaveData,
/// and how to push a SaveData back into each manager.
/// This is the ONLY class that depends on every manager simultaneously.
/// </summary>
public class GameSaveController : MonoBehaviour
{
    public static GameSaveController Instance { get; private set; }

    [Header("Auto-save")]
    [Tooltip("If true, the game is saved automatically at the end of each turn.")]
    public bool autoSaveOnTurnEnd = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (PlayerLoopController.Instance != null && autoSaveOnTurnEnd)
            PlayerLoopController.Instance.OnTurnEnded += HandleTurnEnded;

        // Delete save when the game ends (victory or defeat = fresh start next time)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVictory += HandleGameEnded;
            GameStateManager.Instance.OnDefeat  += HandleGameEnded;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Collects state from all managers and writes it to disk.
    /// Safe to call at any time after the board has been initialised.
    /// </summary>
    public void SaveGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[GameSaveController] SaveManager not found.");
            return;
        }

        SaveData data = CollectSaveData();
        SaveManager.Instance.Save(data);
    }

    /// <summary>
    /// Loads the save file from disk and applies it to all managers.
    /// Returns true if a valid save was found and applied.
    /// </summary>
    public bool LoadGame()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSave)
            return false;

        SaveData data = SaveManager.Instance.Load();
        if (data == null)
            return false;

        StartCoroutine(ApplySaveDataWhenReady(data));
        return true;
    }

    /// <summary>Deletes the save file. Call on New Game.</summary>
    public void DeleteSave()
    {
        SaveManager.Instance?.DeleteSave();
    }

    // ── Save collection ───────────────────────────────────────────────────────

    private SaveData CollectSaveData()
    {
        SaveData data = new SaveData();

        // Player loop
        if (PlayerLoopController.Instance != null)
        {
            data.currentPathIndex = PlayerLoopController.Instance.CurrentPathIndex;
            data.totalLoops       = PlayerLoopController.Instance.TotalLoops;
            data.currentTurn      = PlayerLoopController.Instance.CurrentTurn;
        }

        // Resources
        if (ResourceManager.Instance != null)
            data.currentResources = ResourceManager.Instance.CurrentResources;

        // Narrative flags
        if (GameManager.Instance != null)
            data.narrativeFlags = new List<string>(GameManager.Instance.GetAllFlags());

        // Board seed
        if (BoardManager.Instance != null)
            data.boardSeed = BoardManager.Instance.seed;

        // Tile visited states
        data.tileStates = CollectTileStates();

        return data;
    }

    private List<TileSaveData> CollectTileStates()
    {
        List<TileSaveData> states = new List<TileSaveData>();

        if (BoardManager.Instance == null)
            return states;

        int total = BoardManager.Instance.TotalTiles;
        for (int i = 0; i < total; i++)
        {
            BoardTile tile = BoardManager.Instance.GetTileByPathIndex(i);
            if (tile == null)
                continue;

            states.Add(new TileSaveData
            {
                pathIndex = i,
                isVisited = tile.isVisited
            });
        }

        return states;
    }

    // ── Load / restore ────────────────────────────────────────────────────────

    /// <summary>
    /// Waits for all singletons to be ready before applying save data.
    /// BoardManager generates the board in Start(), so we must wait one frame.
    /// </summary>
    private IEnumerator ApplySaveDataWhenReady(SaveData data)
    {
        // Wait for BoardManager to finish generating
        while (BoardManager.Instance == null || BoardManager.Instance.TotalTiles == 0)
            yield return null;

        // Wait for PlayerLoopController to initialise
        while (PlayerLoopController.Instance == null)
            yield return null;

        yield return new WaitForSeconds(0.2f);

        ApplySaveData(data);
    }

    private void ApplySaveData(SaveData data)
    {
        // 1. Narrative flags
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearFlags();
            foreach (string flag in data.narrativeFlags)
                GameManager.Instance.AddFlag(flag);
        }

        // 2. Resources
        ResourceManager.Instance?.SetResources(data.currentResources);

        // 3. Player position & loop state
        PlayerLoopController.Instance?.RestoreState(data.currentPathIndex, data.currentTurn, data.totalLoops);

        // 4. Tile visited states
        RestoreTileStates(data.tileStates);

        Debug.Log($"<color=cyan>[LOAD]</color> State restored — Turn {data.currentTurn}, Loop {data.totalLoops}, PathIndex {data.currentPathIndex}, Resources {data.currentResources}");
    }

    private void RestoreTileStates(List<TileSaveData> tileStates)
    {
        if (BoardManager.Instance == null || tileStates == null)
            return;

        foreach (TileSaveData ts in tileStates)
        {
            BoardTile tile = BoardManager.Instance.GetTileByPathIndex(ts.pathIndex);
            if (tile != null)
                tile.isVisited = ts.isVisited;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandleTurnEnded(int turn)
    {
        if (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentGameState != GameState.Playing)
            return;

        SaveGame();
    }

    private void HandleGameEnded()
    {
        // Game over (win or lose) → delete save so the next session starts fresh
        SaveManager.Instance?.DeleteSave();
    }

    private void OnDestroy()
    {
        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.OnTurnEnded -= HandleTurnEnded;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVictory -= HandleGameEnded;
            GameStateManager.Instance.OnDefeat  -= HandleGameEnded;
        }
    }
}
