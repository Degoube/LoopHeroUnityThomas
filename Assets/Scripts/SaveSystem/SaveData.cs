using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable snapshot of the game state.
/// No Unity types — safe to serialize with JsonUtility.
/// </summary>
[Serializable]
public class SaveData
{
    // ── Metadata ──────────────────────────────────────────────────────────────
    /// <summary>ISO 8601 timestamp of when the save was written.</summary>
    public string savedAt;

    /// <summary>Incremented on every save — useful to detect the most recent slot.</summary>
    public int saveVersion;

    // ── Player progression ────────────────────────────────────────────────────
    /// <summary>Index of the tile on the loop path where the player currently stands.</summary>
    public int currentPathIndex;

    /// <summary>Number of full loops completed.</summary>
    public int totalLoops;

    /// <summary>Number of turns played in the current loop.</summary>
    public int currentTurn;

    // ── Resources ─────────────────────────────────────────────────────────────
    public int currentResources;

    // ── Experience ────────────────────────────────────────────────────────────
    public int currentXP;

    // ── Narrative flags ───────────────────────────────────────────────────────
    /// <summary>All flags that have been set via GameManager.AddFlag().</summary>
    public List<string> narrativeFlags = new List<string>();

    // ── Board ─────────────────────────────────────────────────────────────────
    /// <summary>Random seed used to regenerate the same board layout.</summary>
    public int boardSeed;

    /// <summary>
    /// Per-tile state indexed by path index.
    /// Allows restoring visited / occupied state after board regeneration.
    /// </summary>
    public List<TileSaveData> tileStates = new List<TileSaveData>();
}

/// <summary>Lightweight snapshot of one BoardTile's runtime state.</summary>
[Serializable]
public class TileSaveData
{
    public int pathIndex;
    public bool isVisited;
}
