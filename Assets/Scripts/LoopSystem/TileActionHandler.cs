using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TileActionEvent : UnityEvent<GameObject, BoardTile> { }

/// <summary>
/// Routes tile activations to their correct handlers.
/// When a tile has a mini-game configured, all rewards are delegated to the mini-game result.
/// Hardcoded rewards only apply as fallbacks when no mini-game prefab is assigned.
/// </summary>
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

    [Header("Ruins Tile (fallback when no mini-game)")]
    public int ruinsGoldMin = 10;
    public int ruinsGoldMax = 50;
    public DialogueData ruinsDialogue;

    [Header("Combat Tile (fallback when no mini-game)")]
    private const int CombatResourceLoss = 15;

    [Header("Altar Tile (fallback when no mini-game)")]
    public int altarHealAmount = 50;
    public DialogueData altarDialogue;

    [Header("Relic Tile (fallback when no mini-game)")]
    public string[] possibleRelics;
    public DialogueData relicDialogue;
    private const int RelicResourceValue = 50;

    private GameObject cachedPlayer;

    private void Start()
    {
        cachedPlayer = GameObject.FindGameObjectWithTag("Player");

        if (BoardManager.Instance == null)
        {
            Debug.LogWarning("[TileActionHandler] BoardManager not found.");
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
        GameObject activator = cachedPlayer;
        bool hasMiniGame = HasMiniGameConfigured(tile);

        switch (tile.tileData.tileType)
        {
            case TileType.Witness:
                ExecuteWitnessAction(activator, tile);
                break;

            case TileType.Ruins:
                ExecuteRuinsAction(activator, tile, hasMiniGame);
                break;

            case TileType.Combat:
                ExecuteCombatAction(activator, tile, hasMiniGame);
                break;

            case TileType.Altar:
                ExecuteAltarAction(activator, tile, hasMiniGame);
                break;

            case TileType.Relic:
                ExecuteRelicAction(activator, tile, hasMiniGame);
                break;

            case TileType.Empty:
                ExecuteEmptyAction(activator, tile);
                break;
        }

        // Launch mini-game AFTER tile setup (name display, first-visit flags, dialogue).
        // All rewards are handled by the mini-game result via MiniGameManager.
        if (hasMiniGame)
            LaunchMiniGame(tile);
    }

    /// <summary>
    /// Returns true if this tile has a valid mini-game prefab configured.
    /// </summary>
    private static bool HasMiniGameConfigured(BoardTile tile)
    {
        return tile.tileData != null
            && tile.tileData.triggersMiniGame
            && tile.tileData.miniGamePrefab != null;
    }

    /// <summary>
    /// Launches the mini-game assigned to this tile.
    /// MiniGameManager handles: pause loop, cache camera, instantiate, wait for result, apply result, resume.
    /// </summary>
    private static void LaunchMiniGame(BoardTile tile)
    {
        if (MiniGameManager.Instance == null)
        {
            Debug.LogWarning("[TileActionHandler] MiniGameManager is missing in the scene.");
            return;
        }

        MiniGameManager.Instance.LaunchMiniGame(tile.tileData.miniGamePrefab, tile);
    }

    // ── Witness (no mini-game, dialogue only) ────────────────────────────────

    private void ExecuteWitnessAction(GameObject activator, BoardTile tile)
    {
        TileNameDisplay.Instance?.ShowTileName("Le Temoin");

        DialogueData dialogue = GetAppropriateWitnessDialogue();

        if (DialogueManager.Instance != null && dialogue != null)
            DialogueManager.Instance.StartDialogue(dialogue);
        else
            Debug.LogWarning("[TileActionHandler] ExecuteWitnessAction: DialogueManager or dialogue is null.");

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

    // ── Ruins = Hide & Seek ──────────────────────────────────────────────────

    private void ExecuteRuinsAction(GameObject activator, BoardTile tile, bool hasMiniGame)
    {
        TileNameDisplay.Instance?.ShowTileName("Ruines Anciennes");

        TryFirstVisit(NarrativeFlags.VisitedRuins, ruinsDialogue);
        tile.MarkAsVisited();

        onRuinsTile?.Invoke(activator, tile);

        // Fallback rewards only when no mini-game is configured
        if (!hasMiniGame)
        {
            int goldFound = UnityEngine.Random.Range(ruinsGoldMin, ruinsGoldMax + 1);
            ResourceManager.Instance?.AddResources(goldFound);
            Debug.Log($"[Ruins] No mini-game — fallback gold: +{goldFound}");
        }
    }

    // ── Combat = Turn-Based Pokemon-Like ─────────────────────────────────────

    private void ExecuteCombatAction(GameObject activator, BoardTile tile, bool hasMiniGame)
    {
        TileNameDisplay.Instance?.ShowTileName("Combat !");

        // Track combat count for narrative
        if (GameManager.Instance != null)
        {
            int combatCount = 0;
            while (GameManager.Instance.HasFlag($"combat_{combatCount}"))
                combatCount++;

            GameManager.Instance.AddFlag($"combat_{combatCount}");
        }

        onCombatTile?.Invoke(activator, tile);

        // Fallback: direct resource loss only when no mini-game handles it
        if (!hasMiniGame)
        {
            ResourceManager.Instance?.RemoveResources(CombatResourceLoss);
            Debug.Log($"[Combat] No mini-game — fallback penalty: -{CombatResourceLoss}");
        }
    }

    // ── Altar = Tic-Tac-Toe (Morpion) ────────────────────────────────────────

    private void ExecuteAltarAction(GameObject activator, BoardTile tile, bool hasMiniGame)
    {
        TileNameDisplay.Instance?.ShowTileName("Autel Sacre");

        TryFirstVisit(NarrativeFlags.ActivatedAltar, altarDialogue);

        onAltarTile?.Invoke(activator, tile);

        // Fallback: direct heal only when no mini-game handles it
        if (!hasMiniGame)
        {
            ResourceManager.Instance?.AddResources(altarHealAmount);
            Debug.Log($"[Altar] No mini-game — fallback heal: +{altarHealAmount}");
        }
    }

    // ── Relic = Fruit Ninja (Relic Slash) ────────────────────────────────────

    private void ExecuteRelicAction(GameObject activator, BoardTile tile, bool hasMiniGame)
    {
        TileNameDisplay.Instance?.ShowTileName("Relique Ancienne");

        TryFirstVisit(NarrativeFlags.FoundRelic, relicDialogue);
        tile.MarkAsVisited();

        onRelicTile?.Invoke(activator, tile);

        // Fallback: direct reward only when no mini-game handles it
        if (!hasMiniGame)
        {
            string relicName = possibleRelics != null && possibleRelics.Length > 0
                ? possibleRelics[UnityEngine.Random.Range(0, possibleRelics.Length)]
                : "Ancient Relic";

            ResourceManager.Instance?.AddResources(RelicResourceValue);
            GameManager.Instance?.AddFlag($"relic_{relicName}");
            Debug.Log($"[Relic] No mini-game — fallback reward: +{RelicResourceValue}");
        }
    }

    // ── Empty ────────────────────────────────────────────────────────────────

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
