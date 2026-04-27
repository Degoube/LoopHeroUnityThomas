using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnhancedQuestUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI questInfoText;
    public TextMeshProUGUI currentTileInfoText;
    
    [Header("Quest Definitions")]
    public List<EnhancedQuest> quests = new List<EnhancedQuest>();
    
    private BoardTile lastActiveTile;
    
    private void Start()
    {
        UpdateQuestDisplay();
        
        if (GameManager.Instance != null)
        {
            InvokeRepeating(nameof(UpdateQuestDisplay), 1f, 1f);
        }
        
        BoardTile[] tiles = FindObjectsByType<BoardTile>(FindObjectsSortMode.None);
        foreach (BoardTile tile in tiles)
        {
            tile.OnTileActivated += HandleTileActivated;
        }
    }
    
    private void HandleTileActivated(BoardTile tile)
    {
        lastActiveTile = tile;
        UpdateCurrentTileInfo(tile);
    }
    
    public void UpdateQuestDisplay()
    {
        if (questInfoText == null || GameManager.Instance == null)
            return;
        
        string questText = "<b><size=22>OBJECTIFS</size></b>\n\n";
        
        foreach (EnhancedQuest quest in quests)
        {
            bool isComplete = quest.IsComplete();
            string statusIcon = isComplete ? "✓" : "○";
            string colorTag = isComplete ? "<color=#00FF00>" : "<color=#FFFFFF>";
            
            questText += $"{statusIcon} {colorTag}{quest.questName}</color>\n";
            questText += $"   <size=14><color=#AAAAAA>→ {GetTileColorName(quest.tileType)}</color></size>\n";
        }
        
        questText += "\n<b><size=18>CONDITION DE VICTOIRE</size></b>\n";
        questText += "<color=#FFD700>Obtenir le flag 'truth_done'</color>\n";
        questText += "<size=14><color=#AAAAAA>Débloquez TOUTES les cases (Ruines, Autel, Relique)</color></size>\n";
        questText += "<size=14><color=#AAAAAA>puis retournez au Témoin (case violette)</color></size>";
        
        questInfoText.text = questText;
    }
    
    private void UpdateCurrentTileInfo(BoardTile tile)
    {
        if (currentTileInfoText == null || tile == null || tile.TileData == null)
            return;
        
        TileData data = tile.TileData;
        string info = $"<b><size=20>CASE ACTUELLE</size></b>\n\n";
        
        info += $"<b>{data.tileName}</b>\n";
        info += $"<color=#{ColorUtility.ToHtmlStringRGB(data.tileColor)}>■</color> {GetTileTypeName(data.tileType)}\n\n";
        
        info += "<b>EFFETS :</b>\n";
        
        switch (data.tileType)
        {
            case TileType.Witness:
                info += "• Dialogue avec le Témoin\n";
                break;
                
            case TileType.Ruins:
                info += $"• Or: <color=#FFD700>+{GetRuinsGoldAmount()}</color>\n";
                info += "• Mini-jeu: Cache-Cache\n";
                info += "• <color=#FF6666>Non revisitable</color>\n";
                break;
                
            case TileType.Combat:
                info += "• Ressources: <color=#FF0000>-15</color>\n";
                info += "• Mini-jeu: Combat RPG\n";
                break;
                
            case TileType.Altar:
                info += $"• Ressources: <color=#00FF00>+{GetAltarHealAmount()}</color>\n";
                info += "• Mini-jeu: Morpion\n";
                break;
                
            case TileType.Relic:
                info += $"• Ressources: <color=#00FF00>+50</color>\n";
                info += "• Mini-jeu: Relic Slash\n";
                info += "• <color=#FF6666>Non revisitable</color>\n";
                break;
                
            case TileType.Empty:
                info += "• Aucun effet\n";
                break;
        }
        
        currentTileInfoText.text = info;
    }
    
    private string GetTileColorName(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Witness:
                return "Case <color=#CC00FF>Violette</color>";
            case TileType.Ruins:
                return "Case <color=#654321>Marron</color>";
            case TileType.Combat:
                return "Case <color=#FF0000>Rouge</color>";
            case TileType.Altar:
                return "Case <color=#0052FF>Bleue</color>";
            case TileType.Relic:
                return "Case <color=#FFD700>Dorée</color>";
            case TileType.Empty:
                return "Case <color=#8B8B8B>Grise</color>";
            default:
                return "Case inconnue";
        }
    }
    
    private string GetTileTypeName(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Witness: return "Témoin";
            case TileType.Ruins: return "Ruines";
            case TileType.Combat: return "Combat";
            case TileType.Altar: return "Autel";
            case TileType.Relic: return "Relique";
            case TileType.Empty: return "Vide";
            default: return "Inconnu";
        }
    }
    
    private string GetRuinsGoldAmount()
    {
        TileActionHandler handler = FindFirstObjectByType<TileActionHandler>();
        if (handler != null)
        {
            return $"{handler.ruinsGoldMin}-{handler.ruinsGoldMax}";
        }
        return "10-50";
    }
    
    private int GetAltarHealAmount()
    {
        TileActionHandler handler = FindFirstObjectByType<TileActionHandler>();
        if (handler != null)
        {
            return handler.altarHealAmount;
        }
        return 50;
    }
    
    private void OnDestroy()
    {
        BoardTile[] tiles = FindObjectsByType<BoardTile>(FindObjectsSortMode.None);
        foreach (BoardTile tile in tiles)
        {
            tile.OnTileActivated -= HandleTileActivated;
        }
    }
}

[System.Serializable]
public class EnhancedQuest
{
    public string questName;
    public string requiredFlag;
    public TileType tileType;
    
    public bool IsComplete()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(requiredFlag))
            return false;
        
        return GameManager.Instance.HasFlag(requiredFlag);
    }
}
