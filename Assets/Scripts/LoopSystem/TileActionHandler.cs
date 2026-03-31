using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TileActionEvent : UnityEvent<GameObject, BoardTile> { }

public class TileActionHandler : MonoBehaviour
{
    [Header("Tile Type Events")]
    public TileActionEvent onWitnessTile;
    public TileActionEvent onRuinsTile;
    public TileActionEvent onCombatTile;
    public TileActionEvent onAltarTile;
    public TileActionEvent onRelicTile;
    public TileActionEvent onEmptyTile;

    [Header("Witness Tile")]
    public DialogueData witnessFirstDialogue;
    public DialogueData witnessReturningDialogue;
    public DialogueData witnessIncompleteDialogue;
    public DialogueData witnessFinalDialogue;

    [Header("Ruins Tile")]
    public int ruinsGoldMin = 10;
    public int ruinsGoldMax = 50;
    public DialogueData ruinsDialogue;

    [Header("Combat Tile")]
    public GameObject[] enemyPrefabs;
    private const int CombatResourceLoss = 15;

    [Header("Altar Tile")]
    public int altarHealAmount = 50;
    public DialogueData altarDialogue;

    [Header("Relic Tile")]
    public string[] possibleRelics;
    public DialogueData relicDialogue;
    private const int RelicResourceValue = 50;

    private void Start()
    {
        // Subscribe via BoardManager to avoid missing tiles created after this Start
        if (BoardManager.Instance == null)
        {
            Debug.LogWarning("TileActionHandler: BoardManager not found.");
            return;
        }

        int total = BoardManager.Instance.TotalTiles;
        for (int i = 0; i < total; i++)
        {
            BoardTile tile = BoardManager.Instance.GetTileByPathIndex(i);
            if (tile != null)
                tile.OnTileActivated += HandleTileActivation;
        }
    }

    private void HandleTileActivation(BoardTile tile)
    {
        GameObject activator = GameObject.FindGameObjectWithTag("Player");

        switch (tile.tileData.tileType)
        {
            case TileType.Witness: ExecuteWitnessAction(activator, tile); break;
            case TileType.Ruins:   ExecuteRuinsAction(activator, tile);   break;
            case TileType.Combat:  ExecuteCombatAction(activator, tile);  break;
            case TileType.Altar:   ExecuteAltarAction(activator, tile);   break;
            case TileType.Relic:   ExecuteRelicAction(activator, tile);   break;
            case TileType.Empty:   ExecuteEmptyAction(activator, tile);   break;
        }

        // After the tile's own action, launch mini-game if configured
        TryLaunchMiniGame(tile);
    }

    /// <summary>
    /// Launches the mini-game assigned to this tile if triggersMiniGame is true.
    /// MiniGameManager handles pause and resume of the loop.
    /// </summary>
    private static void TryLaunchMiniGame(BoardTile tile)
    {
        if (tile.tileData == null
            || !tile.tileData.triggersMiniGame
            || tile.tileData.miniGamePrefab == null)
            return;

        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("TileActionHandler: TryLaunchMiniGame called but MiniGameManager is missing in the scene.");
            return;
        }

        MiniGameManager.Instance.LaunchMiniGame(tile.tileData.miniGamePrefab, tile);
    }

    // ── Witness ──────────────────────────────────────────────────────────────

    private void ExecuteWitnessAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Le Témoin");

        DialogueData dialogue = GetAppropriateWitnessDialogue();

        if (DialogueManager.Instance != null && dialogue != null)
            DialogueManager.Instance.StartDialogue(dialogue);
        else
            Debug.LogWarning("ExecuteWitnessAction: DialogueManager or dialogue is null.");

        onWitnessTile?.Invoke(activator, tile);
    }

    private DialogueData GetAppropriateWitnessDialogue()
    {
        if (GameManager.Instance == null)
            return witnessFirstDialogue;

        if (GameManager.Instance.HasFlag(NarrativeFlags.LoopAware) && witnessFinalDialogue != null)
            return witnessFinalDialogue;

        bool hasAllQuestFlags = GameManager.Instance.HasFlag(NarrativeFlags.VisitedRuins)
                             && GameManager.Instance.HasFlag(NarrativeFlags.ActivatedAltar)
                             && GameManager.Instance.HasFlag(NarrativeFlags.FoundRelic);

        if (hasAllQuestFlags && witnessReturningDialogue != null)
            return witnessReturningDialogue;

        if (GameManager.Instance.HasFlag(NarrativeFlags.MetWitness) && witnessIncompleteDialogue != null)
            return witnessIncompleteDialogue;

        return witnessFirstDialogue;
    }

    // ── Ruins ─────────────────────────────────────────────────────────────────

    private void ExecuteRuinsAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Ruines Anciennes");

        int goldFound = UnityEngine.Random.Range(ruinsGoldMin, ruinsGoldMax + 1);
        ResourceManager.Instance?.AddResources(goldFound);

        TryFirstVisit(NarrativeFlags.VisitedRuins, ruinsDialogue);
        tile.MarkAsVisited();

        onRuinsTile?.Invoke(activator, tile);
    }

    // ── Combat ────────────────────────────────────────────────────────────────

    private void ExecuteCombatAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Combat !");
        ResourceManager.Instance?.RemoveResources(CombatResourceLoss);

        if (GameManager.Instance != null)
        {
            int combatCount = 0;
            while (GameManager.Instance.HasFlag($"combat_{combatCount}"))
                combatCount++;

            GameManager.Instance.AddFlag($"combat_{combatCount}");
        }

        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            if (enemyPrefabs[idx] != null)
                Instantiate(enemyPrefabs[idx], tile.transform.position + Vector3.up, Quaternion.identity);
        }

        onCombatTile?.Invoke(activator, tile);
    }

    // ── Altar ─────────────────────────────────────────────────────────────────

    private void ExecuteAltarAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Autel Sacré");
        ResourceManager.Instance?.AddResources(altarHealAmount);

        TryFirstVisit(NarrativeFlags.ActivatedAltar, altarDialogue);

        onAltarTile?.Invoke(activator, tile);
    }

    // ── Relic ─────────────────────────────────────────────────────────────────

    private void ExecuteRelicAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Relique Ancienne");

        string relicName = possibleRelics != null && possibleRelics.Length > 0
            ? possibleRelics[UnityEngine.Random.Range(0, possibleRelics.Length)]
            : "Ancient Relic";

        ResourceManager.Instance?.AddResources(RelicResourceValue);
        GameManager.Instance?.AddFlag($"relic_{relicName}");

        TryFirstVisit(NarrativeFlags.FoundRelic, relicDialogue);
        tile.MarkAsVisited();

        onRelicTile?.Invoke(activator, tile);
    }

    // ── Empty ─────────────────────────────────────────────────────────────────

    private void ExecuteEmptyAction(GameObject activator, BoardTile tile)
    {
        onEmptyTile?.Invoke(activator, tile);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the given flag on first visit and starts the associated dialogue.
    /// Does nothing on subsequent visits.
    /// </summary>
    private void TryFirstVisit(string flag, DialogueData dialogue)
    {
        if (GameManager.Instance == null || GameManager.Instance.HasFlag(flag))
            return;

        GameManager.Instance.AddFlag(flag);

        if (dialogue != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnDestroy()
    {
        if (BoardManager.Instance == null)
            return;

        int total = BoardManager.Instance.TotalTiles;
        for (int i = 0; i < total; i++)
        {
            BoardTile tile = BoardManager.Instance.GetTileByPathIndex(i);
            if (tile != null)
                tile.OnTileActivated -= HandleTileActivation;
        }
    }
}
