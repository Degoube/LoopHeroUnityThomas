using System;
using UnityEngine;

/// <summary>
/// Singleton that handles the full mini-game lifecycle:
/// cache camera -> pause loop -> spawn mini-game -> wait for result -> apply result -> save -> restore -> resume.
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    /// <summary>Fired when a mini-game starts.</summary>
    public event Action<BoardTile> OnMiniGameStarted;

    /// <summary>Fired when a mini-game ends, before the loop resumes.</summary>
    public event Action<MiniGameResult> OnMiniGameEnded;

    public bool IsMiniGameActive { get; private set; }

    [Header("Mini-Game Root")]
    [Tooltip("Root transform under which mini-game GameObjects are instantiated.")]
    [SerializeField] private Transform miniGameRoot;

    [Header("Auto-Save")]
    [Tooltip("If true, automatically saves the game after each mini-game ends.")]
    [SerializeField] private bool autoSaveAfterMiniGame = true;

    private GameObject currentMiniGameInstance;
    private BoardTile currentTile;
    private Camera cachedMainCamera;

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
    /// </summary>
    public void LaunchMiniGame(GameObject miniGamePrefab, BoardTile sourceTile)
    {
        if (IsMiniGameActive)
        {
            Debug.LogWarning("[MiniGameManager] A mini-game is already active.");
            return;
        }

        if (miniGamePrefab == null)
        {
            Debug.LogError("[MiniGameManager] miniGamePrefab is null.");
            return;
        }

        IMiniGame prefabCheck = miniGamePrefab.GetComponent<IMiniGame>();
        if (prefabCheck == null)
        {
            Debug.LogError($"[MiniGameManager] Prefab '{miniGamePrefab.name}' has no IMiniGame component.");
            return;
        }

        currentTile = sourceTile;
        IsMiniGameActive = true;

        // 1. Cache & disable main camera BEFORE instantiation
        CacheAndDisableMainCamera();

        // 2. Pause the game loop
        PauseLoop();

        // 3. Instantiate & force-activate
        Transform parent = miniGameRoot != null ? miniGameRoot : null;
        currentMiniGameInstance = Instantiate(miniGamePrefab, parent);
        currentMiniGameInstance.SetActive(true);

        // 4. Start the mini-game
        IMiniGame instance = currentMiniGameInstance.GetComponent<IMiniGame>();
        if (instance == null)
        {
            Debug.LogError("[MiniGameManager] Instantiated object lost its IMiniGame component.");
            FullCleanup();
            return;
        }

        instance.OnMiniGameEnded += HandleMiniGameEnded;
        instance.StartMiniGame(sourceTile);

        OnMiniGameStarted?.Invoke(sourceTile);
        Debug.Log($"[MiniGameManager] Mini-game started on tile '{sourceTile?.tileData?.tileName}'");
    }

    private void HandleMiniGameEnded(MiniGameResult result)
    {
        if (!IsMiniGameActive)
            return;

        Debug.Log($"[MiniGameManager] Ended — Success: {result.Success}, Gold: {result.ResourceDelta}, XP: {result.XPDelta}");

        ApplyResult(result);
        FullCleanup();

        if (autoSaveAfterMiniGame)
            GameSaveController.Instance?.SaveGame();

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

        if (result.XPDelta > 0 && XPManager.Instance != null)
            XPManager.Instance.AddXP(result.XPDelta);

        if (result.Success && !string.IsNullOrEmpty(result.FlagToAdd))
            GameManager.Instance?.AddFlag(result.FlagToAdd);
    }

    /// <summary>Destroys mini-game instance, restores camera, resumes loop.</summary>
    private void FullCleanup()
    {
        if (currentMiniGameInstance != null)
        {
            Destroy(currentMiniGameInstance);
            currentMiniGameInstance = null;
        }

        RestoreMainCamera();
        ResumeLoop();

        currentTile = null;
        IsMiniGameActive = false;
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void CacheAndDisableMainCamera()
    {
        cachedMainCamera = Camera.main;

        if (cachedMainCamera != null)
        {
            cachedMainCamera.gameObject.SetActive(false);
            Debug.Log("[MiniGameManager] Main camera cached and disabled.");
        }
        else
        {
            Debug.LogWarning("[MiniGameManager] No MainCamera found to cache.");
        }
    }

    private void RestoreMainCamera()
    {
        if (cachedMainCamera != null)
        {
            cachedMainCamera.gameObject.SetActive(true);
            Debug.Log("[MiniGameManager] Main camera restored.");
        }

        cachedMainCamera = null;
    }

    // ── Loop ──────────────────────────────────────────────────────────────────

    private static void PauseLoop()
    {
        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = false;

        Debug.Log("[MiniGameManager] Loop paused.");
    }

    private static void ResumeLoop()
    {
        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = true;

        Debug.Log("[MiniGameManager] Loop resumed.");
    }
}
