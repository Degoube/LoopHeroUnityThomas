using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject questPanel;
    public TextMeshProUGUI questListText;
    
    [Header("Quest Definitions")]
    public List<Quest> quests = new List<Quest>();
    
    private void Start()
    {
        UpdateQuestDisplay();
        
        if (GameManager.Instance != null)
        {
            InvokeRepeating(nameof(UpdateQuestDisplay), 1f, 1f);
        }
    }
    
    public void UpdateQuestDisplay()
    {
        if (questListText == null || GameManager.Instance == null)
            return;
        
        string questText = "<b>Quêtes :</b>\n\n";
        
        foreach (Quest quest in quests)
        {
            bool isComplete = quest.IsComplete();
            string statusIcon = isComplete ? "✓" : "○";
            string colorTag = isComplete ? "<color=green>" : "<color=white>";
            
            questText += $"{statusIcon} {colorTag}{quest.questName}</color>\n";
        }
        
        questListText.text = questText;
    }
    
    public void ToggleQuestPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(!questPanel.activeSelf);
        }
    }
}

[System.Serializable]
public class Quest
{
    public string questName;
    public string requiredFlag;
    
    public bool IsComplete()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(requiredFlag))
            return false;
        
        return GameManager.Instance.HasFlag(requiredFlag);
    }
}
