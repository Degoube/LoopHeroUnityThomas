using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Main controller for the Hide & Seek mini-game.
/// Implements IMiniGame — MiniGameManager owns the lifecycle and camera management.
///
/// TILE: Ruins
///
/// VICTORY CONDITIONS:
///   Win  = Survive until the timer runs out without being caught  -> +gold, +XP, flag set
///   Lose = Caught by an AI enemy before time expires              -> -gold penalty
///
/// PAUSE: MiniGameManager pauses PlayerLoopController before this starts.
///        MiniGameManager resumes it after OnMiniGameEnded fires.
/// </summary>
public class HideAndSeekGame : MonoBehaviour, IMiniGame
{
    // ── IMiniGame ─────────────────────────────────────────────────────────────
    public event Action<MiniGameResult> OnMiniGameEnded;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Configuration")]
    [Tooltip("ScriptableObject containing all tunable parameters.")]
    [SerializeField] private HideAndSeekConfig config;

    [Header("Scene References")]
    [Tooltip("The player character in this mini-game arena.")]
    [SerializeField] private HideAndSeekPlayer player;

    [Tooltip("All AI enemies in this arena.")]
    [SerializeField] private HideAndSeekAI[] enemies;

    [Tooltip("The HUD manager for this mini-game.")]
    [SerializeField] private HideAndSeekHUD hud;

    [Tooltip("The top-down camera used during this mini-game.")]
    [SerializeField] private Camera miniGameCamera;

    // ── Internal state ────────────────────────────────────────────────────────
    private float elapsedTime;
    private bool isRunning;
    private bool playerCaught;
    private bool countdownActive;

    // ── IMiniGame ─────────────────────────────────────────────────────────────

    /// <summary>Called by MiniGameManager to start the mini-game.</summary>
    public void StartMiniGame(BoardTile tile)
    {
        if (config == null)
        {
            Debug.LogError("[HideAndSeek] No HideAndSeekConfig assigned!");
            OnMiniGameEnded?.Invoke(MiniGameResult.Lose());
            return;
        }

        // Activate camera (main camera already disabled by MiniGameManager)
        if (miniGameCamera != null)
            miniGameCamera.gameObject.SetActive(true);

        // Initialize player
        if (player != null)
        {
            player.Initialize(config.playerSpeed, config.hideTransitionDuration);
            player.OnPlayerCaught += HandlePlayerCaught;
        }
        else
        {
            Debug.LogError("[HideAndSeek] Player reference is null!");
        }

        // Initialize AI (paused until countdown finishes)
        InitializeEnemies();
        PauseAllEnemies();

        // Initialize HUD
        if (hud != null)
            hud.Initialize();

        // Disable player input during countdown
        if (player != null)
            player.SetInputEnabled(false);

        // Start
        elapsedTime = 0f;
        isRunning = false;
        playerCaught = false;
        countdownActive = true;

        StartCoroutine(CountdownThenStart());

        Debug.Log($"[HideAndSeek] Countdown started — Duration: {config.countdownDuration}s, Game: {config.gameDuration}s, Enemies: {enemies?.Length ?? 0}");
    }

    // ── Countdown ───────────────────────────────────────────────────────────

    private IEnumerator CountdownThenStart()
    {
        float countdown = config.countdownDuration;

        if (countdown > 0f)
        {
            int lastShown = -1;
            while (countdown > 0f)
            {
                int display = Mathf.CeilToInt(countdown);
                if (display != lastShown)
                {
                    hud?.ShowCountdown(display.ToString());
                    lastShown = display;
                }
                countdown -= Time.deltaTime;
                yield return null;
            }

            hud?.ShowCountdown("GO !");
            yield return new WaitForSeconds(0.5f);
            hud?.HideCountdown();
        }

        // Start gameplay
        countdownActive = false;
        isRunning = true;

        if (player != null)
            player.SetInputEnabled(true);

        ResumeAllEnemies();

        StartCoroutine(HideControlsHintAfterDelay());

        Debug.Log("[HideAndSeek] Countdown finished — Game started!");
    }

    private void PauseAllEnemies()
    {
        if (enemies == null) return;
        foreach (HideAndSeekAI enemy in enemies)
        {
            if (enemy != null)
                enemy.StopAI();
        }
    }

    private void ResumeAllEnemies()
    {
        if (enemies == null) return;
        foreach (HideAndSeekAI enemy in enemies)
        {
            if (enemy != null)
                enemy.ResumeAI();
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isRunning)
            return;

        elapsedTime += Time.deltaTime;
        float remaining = Mathf.Max(0f, config.gameDuration - elapsedTime);

        if (hud != null)
            hud.UpdateTimer(remaining);

        if (elapsedTime >= config.gameDuration)
            EndGame(true);
    }

    // ── AI Init ───────────────────────────────────────────────────────────────

    private void InitializeEnemies()
    {
        if (enemies == null || config.aiProfiles == null || config.aiProfiles.Length == 0)
        {
            Debug.LogWarning("[HideAndSeek] No enemies or AI profiles configured.");
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            AIProfile assignedProfile = config.aiProfiles[i % config.aiProfiles.Length];
            enemies[i].Initialize(assignedProfile, player);
            enemies[i].OnPlayerCaught += HandlePlayerCaught;
        }
    }

    // ── Events ────────────────────────────────────────────────────────────────

    private void HandlePlayerCaught()
    {
        if (!isRunning || playerCaught)
            return;

        playerCaught = true;
        EndGame(false);
    }

    // ── End Game ──────────────────────────────────────────────────────────────

    private void EndGame(bool victory)
    {
        if (!isRunning)
            return;

        isRunning = false;

        // Stop AI
        if (enemies != null)
        {
            foreach (HideAndSeekAI enemy in enemies)
            {
                if (enemy != null)
                    enemy.StopAI();
            }
        }

        // Stop player
        if (player != null)
            player.SetInputEnabled(false);

        // Calculate rewards
        int goldDelta = victory ? config.goldRewardOnWin : -config.goldPenaltyOnLose;
        int xpDelta = victory ? config.xpRewardOnWin : config.xpRewardOnLose;

        // Show result
        if (hud != null)
            hud.ShowResult(victory, goldDelta, xpDelta);

        Debug.Log($"[HideAndSeek] {(victory ? "VICTORY" : "DEFEAT")} — Gold: {goldDelta}, XP: {xpDelta}");

        StartCoroutine(FireResultAfterDelay(config.resultDisplayDuration, victory, goldDelta, xpDelta));
    }

    private IEnumerator FireResultAfterDelay(float delay, bool victory, int goldDelta, int xpDelta)
    {
        yield return new WaitForSeconds(delay);

        // Unsubscribe
        if (player != null)
            player.OnPlayerCaught -= HandlePlayerCaught;

        if (enemies != null)
        {
            foreach (HideAndSeekAI enemy in enemies)
            {
                if (enemy != null)
                    enemy.OnPlayerCaught -= HandlePlayerCaught;
            }
        }

        // Disable mini-game camera (MiniGameManager will restore main)
        if (miniGameCamera != null)
            miniGameCamera.gameObject.SetActive(false);

        // Build result and fire — MiniGameManager handles Destroy, camera restore, loop resume
        MiniGameResult result = victory
            ? MiniGameResult.Win(goldDelta, xpDelta, NarrativeFlags.RuinsMiniGameWon)
            : MiniGameResult.Lose(goldDelta, xpDelta);

        OnMiniGameEnded?.Invoke(result);
    }

    private IEnumerator HideControlsHintAfterDelay()
    {
        yield return new WaitForSeconds(config.controlsHintDuration);

        if (hud != null)
            hud.HideControlsHint();
    }
}
