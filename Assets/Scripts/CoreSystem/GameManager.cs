using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Initial Dialogue")]
    public DialogueData firstDialogue;
    public float delayBeforeFirstDialogue = 1f;

    private HashSet<string> narrativeFlags = new HashSet<string>();
    private string saveFilePath;
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

        saveFilePath = Path.Combine(Application.persistentDataPath, "narrative_flags.json");
        narrativeFlags.Clear();
        Debug.Log("GameManager initialized with fresh flags (no persistence)");
    }

    private void Start()
    {
        if (firstDialogue != null && !HasFlag("game_started"))
        {
            StartCoroutine(ShowFirstDialogue());
        }
    }

    private IEnumerator ShowFirstDialogue()
    {
        yield return new WaitForSeconds(delayBeforeFirstDialogue);
        
        while (DialogueManager.Instance == null)
        {
            yield return null;
        }
        
        if (!hasShownFirstDialogue)
        {
            hasShownFirstDialogue = true;
            AddFlag("game_started");
            DialogueManager.Instance.StartDialogue(firstDialogue);
            Debug.Log("First dialogue started");
        }
    }

    public void AddFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag))
        {
            Debug.LogWarning("Attempted to add null or empty flag");
            return;
        }

        bool isNew = narrativeFlags.Add(flag);
        if (isNew)
        {
            Debug.Log($"<color=green>[FLAG ADDED]</color> {flag}");
        }
    }

    public void RemoveFlag(string flag)
    {
        narrativeFlags.Remove(flag);
    }

    public bool HasFlag(string flag)
    {
        return narrativeFlags.Contains(flag);
    }

    public int GetFlagCount()
    {
        return narrativeFlags.Count;
    }

    public void SaveFlags()
    {
        NarrativeFlagsData data = new NarrativeFlagsData
        {
            flags = new List<string>(narrativeFlags)
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"Flags saved to {saveFilePath}");
    }

    public void LoadFlags()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("No save file found, starting with empty flags");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        NarrativeFlagsData data = JsonUtility.FromJson<NarrativeFlagsData>(json);
        narrativeFlags = new HashSet<string>(data.flags);
        Debug.Log($"Loaded {narrativeFlags.Count} flags from {saveFilePath}");
    }

    public void ClearAllFlags()
    {
        narrativeFlags.Clear();
        hasShownFirstDialogue = false;
        SaveFlags();
        Debug.Log("All flags cleared and saved");
    }
    
    public void ResetGame()
    {
        ClearAllFlags();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitting - flags will NOT be saved (fresh start on next launch)");
    }
}

[System.Serializable]
public class NarrativeFlagsData
{
    public List<string> flags;
}
