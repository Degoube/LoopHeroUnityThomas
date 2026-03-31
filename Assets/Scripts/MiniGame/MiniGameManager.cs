using System;
using UnityEngine;

/// <summary>
/// Singleton that handles the full mini-game lifecycle:
/// pause the loop → spawn mini-game → wait for result → apply result → resume loop.
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    /// <summary>Fired when a mini-game starts. Useful for UI transitions.</summary>
    public event Action<BoardTile> OnMiniGameStarted;

    /// <summary>Fired when a mini-game ends, before the loop resumes.</summary>
    public event Action<MiniGameResult> OnMiniGameEnded;

    public bool IsMiniGameActive { get; private set; }

    // Root under which mini-game GameObjects are instantiated
    [SerializeField] private Transform miniGameRoot;

    private GameObject currentMiniGameInstance;
    private BoardTile currentTile;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Launches a mini-game from the given prefab, triggered by sourceTile.
    /// Pauses PlayerLoopController while the mini-game runs.
    /// </summary>
    public void LaunchMiniGame(GameObject miniGamePrefab, BoardTile sourceTile)
    {
        if (IsMiniGameActive)
        {
            Debug.LogWarning("MiniGameManager: A mini-game is already active. Ignoring launch request.");
            return;
        }

        if (miniGamePrefab == null)
        {
            Debug.LogError("MiniGameManager: miniGamePrefab is null. Cannot launch mini-game.");
            return;
        }

        IMiniGame miniGame = miniGamePrefab.GetComponent<IMiniGame>();
        if (miniGame == null)
        {
            Debug.LogError($"MiniGameManager: Prefab '{miniGamePrefab.name}' has no IMiniGame component.");
            return;
        }

        currentTile = sourceTile;
        IsMiniGameActive = true;

        PauseLoop();

        Transform parent = miniGameRoot != null ? miniGameRoot : null;
        currentMiniGameInstance = Instantiate(miniGamePrefab, parent);

        IMiniGame instance = currentMiniGameInstance.GetComponent<IMiniGame>();
        instance.OnMiniGameEnded += HandleMiniGameEnded;
        instance.StartMiniGame(sourceTile);

        OnMiniGameStarted?.Invoke(sourceTile);
        Debug.Log($"[MiniGame] Started on tile '{sourceTile?.tileData?.tileName}'");
    }

    private void HandleMiniGameEnded(MiniGameResult result)
    {
        if (!IsMiniGameActive)
            return;

        Debug.Log($"[MiniGame] Ended — Success: {result.Success}, ResourceDelta: {result.ResourceDelta}");

        ApplyResult(result);
        CleanUp();
        ResumeLoop();

        OnMiniGameEnded?.Invoke(result);
    }

    private void ApplyResult(MiniGameResult result)
    {
        if (result.ResourceDelta != 0 && ResourceManager.Instance != null)
        {
            if (result.ResourceDelta > 0)
                ResourceManager.Instance.AddResources(result.ResourceDelta);
            else
                ResourceManager.Instance.RemoveResources(-result.ResourceDelta);
        }

        if (result.Success && !string.IsNullOrEmpty(result.FlagToAdd))
            GameManager.Instance?.AddFlag(result.FlagToAdd);
    }

    private void CleanUp()
    {
        if (currentMiniGameInstance != null)
        {
            Destroy(currentMiniGameInstance);
            currentMiniGameInstance = null;
        }

        currentTile = null;
        IsMiniGameActive = false;
    }

    private static void PauseLoop()
    {
        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = false;

        Debug.Log("[MiniGame] Loop paused.");
    }

    private static void ResumeLoop()
    {
        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = true;

        Debug.Log("[MiniGame] Loop resumed.");
    }
}
