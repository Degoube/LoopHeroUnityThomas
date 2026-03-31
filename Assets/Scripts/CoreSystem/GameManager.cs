using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Initial Dialogue")]
    public DialogueData firstDialogue;
    public float delayBeforeFirstDialogue = 1f;

    private readonly HashSet<string> narrativeFlags = new HashSet<string>();
    private bool hasShownFirstDialogue = false;

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
        if (firstDialogue != null && !HasFlag(NarrativeFlags.GameStarted))
            StartCoroutine(ShowFirstDialogue());
    }

    private IEnumerator ShowFirstDialogue()
    {
        yield return new WaitForSeconds(delayBeforeFirstDialogue);

        while (DialogueManager.Instance == null)
            yield return null;

        if (!hasShownFirstDialogue)
        {
            hasShownFirstDialogue = true;
            AddFlag(NarrativeFlags.GameStarted);
            DialogueManager.Instance.StartDialogue(firstDialogue);
        }
    }

    /// <summary>Adds a narrative flag. No-op if the flag already exists or is empty.</summary>
    public void AddFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
        {
            Debug.LogWarning("Attempted to add null or empty flag");
            return;
        }

        if (narrativeFlags.Add(flag))
            Debug.Log($"<color=green>[FLAG ADDED]</color> {flag}");
    }

    /// <summary>Removes a narrative flag.</summary>
    public void RemoveFlag(string flag)
    {
        narrativeFlags.Remove(flag);
    }

    /// <summary>Returns true if the flag has been set.</summary>
    public bool HasFlag(string flag)
    {
        return narrativeFlags.Contains(flag);
    }

    public int GetFlagCount() => narrativeFlags.Count;

    /// <summary>Clears all flags and reloads the active scene.</summary>
    public void ResetGame()
    {
        narrativeFlags.Clear();
        hasShownFirstDialogue = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
