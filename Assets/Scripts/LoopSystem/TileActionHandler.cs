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

    [Header("Altar Tile")]
    public int altarHealAmount = 50;
    public int altarCost = 20;
    public DialogueData altarDialogue;

    [Header("Relic Tile")]
    public string[] possibleRelics;
    public DialogueData relicDialogue;

    private void Start()
    {
        BoardTile[] tiles = FindObjectsByType<BoardTile>(FindObjectsSortMode.None);
        foreach (BoardTile tile in tiles)
        {
            tile.OnTileActivated += HandleTileActivation;
        }
    }

    private void HandleTileActivation(BoardTile tile)
    {
        GameObject activator = GameObject.FindGameObjectWithTag("Player");

        switch (tile.tileData.tileType)
        {
            case TileType.Witness:
                ExecuteWitnessAction(activator, tile);
                break;
            case TileType.Ruins:
                ExecuteRuinsAction(activator, tile);
                break;
            case TileType.Combat:
                ExecuteCombatAction(activator, tile);
                break;
            case TileType.Altar:
                ExecuteAltarAction(activator, tile);
                break;
            case TileType.Relic:
                ExecuteRelicAction(activator, tile);
                break;
            case TileType.Empty:
                ExecuteEmptyAction(activator, tile);
                break;
        }
    }

    private void ExecuteWitnessAction(GameObject activator, BoardTile tile)
    {
        Debug.Log("Witness Tile: Starting witness dialogue");

        if (TileNameDisplay.Instance != null)
        {
            TileNameDisplay.Instance.ShowTileName("Le Témoin");
        }

        DialogueData dialogueToUse = GetAppropriateWitnessDialogue();

        if (DialogueManager.Instance != null && dialogueToUse != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToUse);
        }
        else
        {
            if (DialogueManager.Instance == null)
                Debug.LogWarning("DialogueManager.Instance is null");
            if (dialogueToUse == null)
                Debug.LogWarning("No appropriate witness dialogue found");
        }

        onWitnessTile?.Invoke(activator, tile);
    }

    private DialogueData GetAppropriateWitnessDialogue()
    {
        if (GameManager.Instance == null)
            return witnessFirstDialogue;

        bool hasAllQuestFlags = GameManager.Instance.HasFlag("visited_ruins") 
                              && GameManager.Instance.HasFlag("activated_altar") 
                              && GameManager.Instance.HasFlag("found_relic");
        
        bool hasMetWitness = GameManager.Instance.HasFlag("met_witness");

        if (GameManager.Instance.HasFlag("loop_aware") && witnessFinalDialogue != null)
        {
            Debug.Log("Witness dialogue: FINAL (loop_aware flag present)");
            return witnessFinalDialogue;
        }
        else if (hasAllQuestFlags && witnessReturningDialogue != null)
        {
            Debug.Log("Witness dialogue: RETURNING (all 3 quest flags obtained)");
            return witnessReturningDialogue;
        }
        else if (hasMetWitness && witnessIncompleteDialogue != null)
        {
            Debug.Log("Witness dialogue: INCOMPLETE (met witness but missing quest flags)");
            return witnessIncompleteDialogue;
        }
        else
        {
            Debug.Log("Witness dialogue: FIRST");
            return witnessFirstDialogue;
        }
    }

    private void ExecuteRuinsAction(GameObject activator, BoardTile tile)
    {
        if (TileNameDisplay.Instance != null)
        {
            TileNameDisplay.Instance.ShowTileName("Ruines Anciennes");
        }

        int goldFound = UnityEngine.Random.Range(ruinsGoldMin, ruinsGoldMax + 1);
        Debug.Log($"Ruins Tile: Found {goldFound} gold");

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(goldFound);
        }

        bool isFirstVisit = GameManager.Instance != null && !GameManager.Instance.HasFlag("visited_ruins");
        
        if (GameManager.Instance != null && isFirstVisit)
        {
            GameManager.Instance.AddFlag("visited_ruins");
            Debug.Log("Flag set: visited_ruins");
            CheckForDialogueTrigger();
        }

        if (isFirstVisit && ruinsDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log("Starting Ruins dialogue (first visit only)");
            DialogueManager.Instance.StartDialogue(ruinsDialogue);
        }
        else if (!isFirstVisit)
        {
            Debug.Log("Ruins already visited - dialogue skipped");
        }

        tile.MarkAsVisited();

        onRuinsTile?.Invoke(activator, tile);
    }

    private void ExecuteCombatAction(GameObject activator, BoardTile tile)
    {
        Debug.Log("Combat Tile: Enemy encounter");

        if (TileNameDisplay.Instance != null)
        {
            TileNameDisplay.Instance.ShowTileName("Combat !");
        }

        int resourceLoss = 15;
        
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.RemoveResources(resourceLoss);
        }

        if (GameManager.Instance != null)
        {
            int combatCount = 0;
            while (GameManager.Instance.HasFlag($"combat_{combatCount}"))
            {
                combatCount++;
            }
            
            GameManager.Instance.AddFlag($"combat_{combatCount}");
            Debug.Log($"Flag set: combat_{combatCount}");
        }

        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            GameObject enemy = enemyPrefabs[randomIndex];

            if (enemy != null)
            {
                Instantiate(enemy, tile.transform.position + Vector3.up, Quaternion.identity);
            }
        }

        onCombatTile?.Invoke(activator, tile);
    }

    private void ExecuteAltarAction(GameObject activator, BoardTile tile)
    {
        Debug.Log("Altar Tile: Sacred power activated");

        if (TileNameDisplay.Instance != null)
        {
            TileNameDisplay.Instance.ShowTileName("Autel Sacré");
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(altarHealAmount);
        }

        bool isFirstVisit = GameManager.Instance != null && !GameManager.Instance.HasFlag("activated_altar");
        
        if (GameManager.Instance != null && isFirstVisit)
        {
            GameManager.Instance.AddFlag("activated_altar");
            Debug.Log("Flag set: activated_altar");
            CheckForDialogueTrigger();
        }

        if (isFirstVisit && altarDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log("Starting Altar dialogue (first visit only)");
            DialogueManager.Instance.StartDialogue(altarDialogue);
        }
        else if (!isFirstVisit)
        {
            Debug.Log("Altar already visited - dialogue skipped");
        }

        onAltarTile?.Invoke(activator, tile);
    }

    private void ExecuteRelicAction(GameObject activator, BoardTile tile)
    {
        if (TileNameDisplay.Instance != null)
        {
            TileNameDisplay.Instance.ShowTileName("Relique Ancienne");
        }

        string relicName = "Ancient Relic";
        
        if (possibleRelics != null && possibleRelics.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, possibleRelics.Length);
            relicName = possibleRelics[randomIndex];
        }
        
        Debug.Log($"Relic Tile: Obtained {relicName}");

        int relicValue = 50;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(relicValue);
        }

        bool isFirstVisit = GameManager.Instance != null && !GameManager.Instance.HasFlag("found_relic");
        
        if (GameManager.Instance != null)
        {
            if (isFirstVisit)
            {
                GameManager.Instance.AddFlag("found_relic");
                Debug.Log("Flag set: found_relic");
                CheckForDialogueTrigger();
            }
            
            GameManager.Instance.AddFlag($"relic_{relicName}");
            Debug.Log($"Flag set: relic_{relicName}");
        }

        if (isFirstVisit && relicDialogue != null && DialogueManager.Instance != null)
        {
            Debug.Log("Starting Relic dialogue (first visit only)");
            DialogueManager.Instance.StartDialogue(relicDialogue);
        }
        else if (!isFirstVisit)
        {
            Debug.Log("Relic already found - dialogue skipped");
        }

        tile.MarkAsVisited();

        onRelicTile?.Invoke(activator, tile);
    }

    private void CheckForDialogueTrigger()
    {
        if (GameManager.Instance == null || DialogueManager.Instance == null)
            return;

        bool hasRuins = GameManager.Instance.HasFlag("visited_ruins");
        bool hasAltar = GameManager.Instance.HasFlag("activated_altar");
        bool hasRelic = GameManager.Instance.HasFlag("found_relic");

        if (hasRuins && hasAltar && hasRelic && !GameManager.Instance.HasFlag("loop_aware"))
        {
            Debug.Log("All quest flags obtained! Witness dialogue will progress on next visit.");
        }
    }

    private void ExecuteEmptyAction(GameObject activator, BoardTile tile)
    {
        Debug.Log("Empty Tile: Nothing happens");
        onEmptyTile?.Invoke(activator, tile);
    }

    private void OnDestroy()
    {
        BoardTile[] tiles = FindObjectsByType<BoardTile>(FindObjectsSortMode.None);
        foreach (BoardTile tile in tiles)
        {
            tile.OnTileActivated -= HandleTileActivation;
        }
    }
}
