using System;
using UnityEngine;

/// <summary>
/// Singleton that handles the full mini-game lifecycle:
/// cache camera -> pause loop -> spawn mini-game -> wait for result -> apply result -> save -> restore -> resume.
///
/// PAUSE BEHAVIOR:
///   - PlayerLoopController.enabled is set to false: this stops Update() (no dice rolls during mini-game).
///   - Coroutines on PlayerLoopController keep running (they belong to the GameObject, not the component).
///   - PlayerLoopController.WaitForTileAction() waits on IsMiniGameActive and resumes automatically.
///   - On cleanup, PlayerLoopController.enabled is restored to true and EndTurn() is called by the coroutine.
///
/// TILE -> MINI-GAME MAPPING:
///   Ruins   -> Hide & Seek       (HideAndSeekGame)
///   Combat  -> Turn-Based RPG    (CombatGame)
///   Altar   -> Tic-Tac-Toe       (TicTacToeGame)
///   Relic   -> Fruit Ninja       (RelicSlashGame)
///
/// Each mini-game prefab is assigned via TileData.miniGamePrefab on its corresponding TileData ScriptableObject.
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    /// <summary>Fired when a mini-game starts. Subscribers can use this to hide board UI.</summary>
    public event Action<BoardTile> OnMiniGameStarted;

    /// <summary>Fired when a mini-game ends, before the loop resumes. Subscribers can use this to show rewards UI.</summary>
    public event Action<MiniGameResult> OnMiniGameEnded;

    /// <summary>True while a mini-game is running. PlayerLoopController checks this to block input.</summary>
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
    /// Flow: validate -> pause loop -> disable main camera -> instantiate -> start.
    /// The mini-game fires OnMiniGameEnded when done; this manager handles cleanup.
    /// </summary>
    public void LaunchMiniGame(GameObject miniGamePrefab, BoardTile sourceTile)
    {
        if (IsMiniGameActive)
        {
            Debug.LogWarning("[MiniGameManager] A mini-game is already active — ignoring.");
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

        // 1. Pause the game loop (disable Update, keep coroutines alive for WaitForTileAction)
        PauseLoop();

        // 2. Cache & disable main camera BEFORE instantiation so the mini-game's camera takes over
        CacheAndDisableMainCamera();

        // 3. Instantiate & force-activate
        Transform parent = miniGameRoot != null ? miniGameRoot : null;
        currentMiniGameInstance = Instantiate(miniGamePrefab, parent);
        currentMiniGameInstance.SetActive(true);

        // 4. Subscribe and start
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

        string tileName = sourceTile?.tileData?.tileName ?? "unknown";
        Debug.Log($"[MiniGameManager] Mini-game started on tile '{tileName}' — Loop PAUSED.");
    }

    private void HandleMiniGameEnded(MiniGameResult result)
    {
        if (!IsMiniGameActive)
            return;

        Debug.Log($"[MiniGameManager] Mini-game ended — Success: {result.Success}, " +
                  $"Gold: {result.ResourceDelta:+#;-#;0}, XP: {result.XPDelta:+#;-#;0}");

        ApplyResult(result);
        FullCleanup();

        if (autoSaveAfterMiniGame)
            GameSaveController.Instance?.SaveGame();

        OnMiniGameEnded?.Invoke(result);
    }

    /// <summary>
    /// Applies the mini-game result: gold, XP, narrative flags.
    /// This is the ONLY place rewards are applied — tile actions do NOT duplicate them.
    /// </summary>
    private void ApplyResult(MiniGameResult result)
    {
        // Gold
        if (result.ResourceDelta != 0 && ResourceManager.Instance != null)
        {
            if (result.ResourceDelta > 0)
                ResourceManager.Instance.AddResources(result.ResourceDelta);
            else
                ResourceManager.Instance.RemoveResources(-result.ResourceDelta);
        }

        // XP
        if (result.XPDelta > 0 && XPManager.Instance != null)
            XPManager.Instance.AddXP(result.XPDelta);

        // Narrative flag
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

        Debug.Log("[MiniGameManager] Cleanup complete — Loop RESUMED.");
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void CacheAndDisableMainCamera()
    {
        cachedMainCamera = Camera.main;

        if (cachedMainCamera != null)
            cachedMainCamera.gameObject.SetActive(false);
        else
            Debug.LogWarning("[MiniGameManager] No MainCamera found to cache.");
    }

    private void RestoreMainCamera()
    {
        if (cachedMainCamera != null)
            cachedMainCamera.gameObject.SetActive(true);

        cachedMainCamera = null;
    }

    // ── Loop Pause/Resume ─────────────────────────────────────────────────────

    /// <summary>
    /// Pauses the game loop by disabling PlayerLoopController.
    /// This stops Update() (no dice rolls) but coroutines keep running.
    /// The WaitForTileAction coroutine waits on IsMiniGameActive.
    /// </summary>
    private static void PauseLoop()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.enabled = false;
            Debug.Log("[MiniGameManager] PlayerLoopController.enabled = false (paused).");
        }
    }

    /// <summary>
    /// Resumes the game loop by re-enabling PlayerLoopController.
    /// The WaitForTileAction coroutine detects IsMiniGameActive = false and calls EndTurn().
    /// </summary>
    private static void ResumeLoop()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.enabled = true;
            Debug.Log("[MiniGameManager] PlayerLoopController.enabled = true (resumed).");
        }
    }
}
