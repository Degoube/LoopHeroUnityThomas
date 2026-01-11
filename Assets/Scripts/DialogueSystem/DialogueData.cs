using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Dialogue Information")]
    public string dialogueName;

    [TextArea(2, 4)]
    public string description;

    [Header("Starting Node")]
    public DialogueNode startNode;

    [Header("Flag Requirements")]
    public string requiredFlagToStart;
    public string flagToSetOnComplete;

    public bool CanStart()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(requiredFlagToStart))
            return true;

        return GameManager.Instance.HasFlag(requiredFlagToStart);
    }

    public void OnDialogueComplete()
    {
        Debug.Log($"<color=lime>OnDialogueComplete called for dialogue: '{dialogueName}'</color>");
        
        if (GameManager.Instance != null && !string.IsNullOrEmpty(flagToSetOnComplete))
        {
            GameManager.Instance.AddFlag(flagToSetOnComplete);
            Debug.Log($"<color=lime>Dialogue '{dialogueName}' complete - Flag set: {flagToSetOnComplete}</color>");
        }
        else if (string.IsNullOrEmpty(flagToSetOnComplete))
        {
            Debug.Log($"<color=orange>Dialogue '{dialogueName}' has no flagToSetOnComplete</color>");
        }
    }
}
