using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Speaker")]
    public string speakerName;

    [Header("Dialogue Content")]
    [TextArea(3, 10)]
    public string dialogueText;

    [Header("Flag Requirements")]
    public List<string> requiredFlags = new List<string>();
    public List<string> forbiddenFlags = new List<string>();

    [Header("Flag Effects")]
    public List<string> flagsToAddOnVisit = new List<string>();
    public List<string> flagsToRemoveOnVisit = new List<string>();

    [Header("Choices")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Auto-Continue")]
    public DialogueNode nextNode;

    public bool CanDisplay()
    {
        if (GameManager.Instance == null)
            return true;

        foreach (string flag in requiredFlags)
        {
            if (!GameManager.Instance.HasFlag(flag))
                return false;
        }

        foreach (string flag in forbiddenFlags)
        {
            if (GameManager.Instance.HasFlag(flag))
                return false;
        }

        return true;
    }

    public void OnNodeVisit()
    {
        if (GameManager.Instance == null)
            return;

        foreach (string flag in flagsToAddOnVisit)
        {
            GameManager.Instance.AddFlag(flag);
        }

        foreach (string flag in flagsToRemoveOnVisit)
        {
            GameManager.Instance.RemoveFlag(flag);
        }
    }

    public List<DialogueChoice> GetAvailableChoices()
    {
        List<DialogueChoice> availableChoices = new List<DialogueChoice>();

        foreach (DialogueChoice choice in choices)
        {
            if (choice.IsAvailable())
            {
                availableChoices.Add(choice);
            }
        }

        return availableChoices;
    }
}
