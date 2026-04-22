using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Relic Slash mini-game (Fruit Ninja-like).
/// Objects are launched in arcs — the player slashes them for points.
/// Implements IMiniGame for integration with MiniGameManager.
///
/// TILE: Relic
///
/// VICTORY CONDITIONS:
///   Win  = Reach the target score before the timer runs out    -> +gold, +XP (from RewardTable or fallback)
///   Lose = Timer runs out with insufficient score              -> -gold penalty
///
/// SCORING:
///   Each slashed object = base points * combo multiplier
///   Traps (bombs) = score penalty + time penalty + combo reset
///
/// PAUSE: MiniGameManager pauses PlayerLoopController before this starts.
///        MiniGameManager resumes it after OnMiniGameEnded fires.
/// </summary>
public class RelicSlashGame : MonoBehaviour, IMiniGame
{
    // ── IMiniGame ─────────────────────────────────────────────────────────────
    public event Action<MiniGameResult> OnMiniGameEnded;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Configuration")]
    [SerializeField] private RelicSlashConfig config;

    [Header("Components")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private RelicSlashHUD hud;
    [SerializeField] private RelicSlashSpawner spawner;
    [SerializeField] private SlashDetector slashDetector;

    [Header("Camera")]
    [SerializeField] private Camera slashCamera;

    // ── Runtime State ─────────────────────────────────────────────────────────
    private int currentScore;
    private float remainingTime;
    private int consecutiveHits;
    private float lastHitTime;
    private bool gameActive;
    private bool gameEnded;

    // ── IMiniGame Implementation ──────────────────────────────────────────────

    /// <summary>
    /// Called by MiniGameManager to initialize and start the slash game.
    /// </summary>
    public void StartMiniGame(BoardTile sourceTile)
    {
        if (config == null)
        {
            Debug.LogError("[RelicSlashGame] Config is null. Cannot start.");
            OnMiniGameEnded?.Invoke(MiniGameResult.Lose());
            return;
        }

        // Reset state
        currentScore = 0;
        remainingTime = config.gameDuration;
        consecutiveHits = 0;
        lastHitTime = 0f;
        gameActive = true;
        gameEnded = false;

        // Activate UI
        if (uiRoot != null) uiRoot.SetActive(true);

        // Initialize components
        hud?.Initialize(config.gameDuration, config.scoreToWin);
        hud?.ShowFeedback($"Score {config.scoreToWin} pts pour gagner !", Color.white);
        spawner?.Initialize(config, slashCamera);
        slashDetector?.Initialize(slashCamera);

        // Subscribe to spawner events
        if (spawner != null)
            spawner.OnObjectSpawned += HandleObjectSpawned;

        // Subscribe to HUD close
        if (hud != null)
            hud.OnResultClosed += HandleResultClosed;

        Debug.Log("[RelicSlashGame] Started!");
    }

    private void Update()
    {
        if (!gameActive) return;

        // Update timer
        remainingTime -= Time.deltaTime;
        hud?.UpdateTimer(remainingTime);

        // Check combo timeout
        if (consecutiveHits > 0 && Time.time - lastHitTime > config.comboTimeWindow)
        {
            consecutiveHits = 0;
            hud?.HideCombo();
        }

        // Time's up
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            EndGame();
        }
    }

    // ── Object Events ─────────────────────────────────────────────────────────

    private void HandleObjectSpawned(SlashableObject obj)
    {
        obj.OnSlashed += HandleObjectSlashed;
    }

    private void HandleObjectSlashed(SlashableObject obj, bool isTrap)
    {
        obj.OnSlashed -= HandleObjectSlashed;

        if (!gameActive) return;

        if (isTrap)
        {
            HandleTrapHit();
        }
        else
        {
            HandleSuccessfulSlash(obj);
        }
    }

    private void HandleSuccessfulSlash(SlashableObject obj)
    {
        // Update combo
        consecutiveHits++;
        lastHitTime = Time.time;

        // Calculate points
        float comboMultiplier = config.GetComboMultiplier(consecutiveHits);
        int points = Mathf.RoundToInt((config.pointsPerSlash + obj.BonusPoints) * comboMultiplier);

        currentScore += points;
        hud?.UpdateScore(currentScore);

        // Show combo text
        string comboText = config.GetComboText(consecutiveHits);
        if (!string.IsNullOrEmpty(comboText))
            hud?.ShowCombo(comboText);

        // Show feedback
        string feedbackMsg = comboMultiplier > 1f ? $"+{points} {comboText}" : $"+{points}";
        hud?.ShowFeedback(feedbackMsg, Color.green);
    }

    private void HandleTrapHit()
    {
        // Score penalty
        currentScore = Mathf.Max(0, currentScore - config.trapScorePenalty);
        hud?.UpdateScore(currentScore);

        // Time penalty
        remainingTime = Mathf.Max(0f, remainingTime - config.trapTimePenalty);

        // Combo reset
        if (config.trapResetsCombo)
        {
            consecutiveHits = 0;
            hud?.HideCombo();
        }

        hud?.ShowFeedback("PIEGE !", Color.red);
    }

    // ── End Game ──────────────────────────────────────────────────────────────

    private void EndGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        gameActive = false;

        // Stop spawning
        spawner?.StopSpawning();
        slashDetector?.SetActive(false);

        // Determine result
        bool victory = currentScore >= config.scoreToWin;
        string rewardsText = BuildRewardsText(victory);

        hud?.ShowResult(victory, currentScore, config.scoreToWin, rewardsText);

        // Auto-close after delay
        StartCoroutine(AutoCloseResult());
    }

    private IEnumerator AutoCloseResult()
    {
        yield return new WaitForSeconds(config.resultDisplayDuration);
        FireResult();
    }

    private void HandleResultClosed()
    {
        StopAllCoroutines();
        FireResult();
    }

    private void FireResult()
    {
        if (uiRoot != null) uiRoot.SetActive(false);

        // Unsubscribe
        if (spawner != null) spawner.OnObjectSpawned -= HandleObjectSpawned;
        if (hud != null) hud.OnResultClosed -= HandleResultClosed;

        bool victory = currentScore >= config.scoreToWin;
        MiniGameResult result = BuildMiniGameResult(victory);

        OnMiniGameEnded?.Invoke(result);
    }

    // ── Reward Building ───────────────────────────────────────────────────────

    private MiniGameResult BuildMiniGameResult(bool victory)
    {
        if (config.rewardTable != null && victory)
        {
            RewardRollResult rollResult = config.rewardTable.Roll();
            return rollResult.ToMiniGameResult(true, NarrativeFlags.RelicMiniGameWon);
        }

        if (victory)
            return MiniGameResult.Win(config.fallbackGoldReward, config.fallbackXPReward, NarrativeFlags.RelicMiniGameWon);

        return MiniGameResult.Lose(-config.defeatGoldPenalty);
    }

    private string BuildRewardsText(bool victory)
    {
        if (!victory)
            return $"Score insuffisant.\n-{config.defeatGoldPenalty} Gold";

        if (config.rewardTable != null)
        {
            RewardRollResult rollResult = config.rewardTable.Roll();
            return $"+{rollResult.GoldAmount} Gold\n+{rollResult.XPAmount} XP";
        }

        return $"+{config.fallbackGoldReward} Gold\n+{config.fallbackXPReward} XP";
    }
}
