using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles reading and writing the JSON save file.
/// Pure I/O — no game logic, no coupling to other managers.
/// Other systems call SaveManager.Instance.Save(data) or Load().
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "savegame.json";

    /// <summary>Full path to the save file on disk.</summary>
    public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    /// <summary>True if a save file exists on disk.</summary>
    public bool HasSave => File.Exists(SaveFilePath);

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

    /// <summary>
    /// Serializes data to JSON and writes it to disk.
    /// Stamps the current UTC time and increments saveVersion.
    /// </summary>
    public void Save(SaveData data)
    {
        try
        {
            data.savedAt    = DateTime.UtcNow.ToString("o"); // ISO 8601
            data.saveVersion++;

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"<color=cyan>[SAVE]</color> Game saved → {SaveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to write save: {e.Message}");
        }
    }

    /// <summary>
    /// Reads and deserializes the save file.
    /// Returns null if the file does not exist or is corrupted.
    /// </summary>
    public SaveData Load()
    {
        if (!HasSave)
        {
            Debug.Log("[SaveManager] No save file found.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"<color=cyan>[LOAD]</color> Save loaded (version {data.saveVersion}, saved {data.savedAt})");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read save: {e.Message}");
            return null;
        }
    }

    /// <summary>Deletes the save file from disk.</summary>
    public void DeleteSave()
    {
        if (!HasSave)
            return;

        try
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete save: {e.Message}");
        }
    }
}
