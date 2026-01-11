using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    [TextArea(2, 4)]
    public string choiceText;

    [Header("Flag Requirements")]
    public List<string> requiredFlags = new List<string>();
    public List<string> forbiddenFlags = new List<string>();

    [Header("Flag Effects")]
    public List<string> flagsToAdd = new List<string>();
    public List<string> flagsToRemove = new List<string>();

    [Header("Next Node")]
    public DialogueNode nextNode;

    public bool IsAvailable()
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

    public void ExecuteChoice()
    {
        if (GameManager.Instance == null)
            return;

        Debug.Log($"<color=magenta>[CHOICE EXECUTED]</color> '{choiceText}' - Adding {flagsToAdd.Count} flags, Removing {flagsToRemove.Count} flags");

        foreach (string flag in flagsToAdd)
        {
            Debug.Log($"<color=magenta>[CHOICE FLAG ADD]</color> Adding flag: '{flag}'");
            GameManager.Instance.AddFlag(flag);
        }

        foreach (string flag in flagsToRemove)
        {
            Debug.Log($"<color=magenta>[CHOICE FLAG REMOVE]</color> Removing flag: '{flag}'");
            GameManager.Instance.RemoveFlag(flag);
        }
    }
}
