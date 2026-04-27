using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Turn-based combat mini-game (Pokemon-like).
/// Implements IMiniGame for integration with MiniGameManager.
/// Manages the combat loop: turn order, player actions, AI decisions, win/lose conditions.
///
/// TILE: Combat
///
/// VICTORY CONDITIONS:
///   Win    = Reduce enemy HP to 0                    -> +gold, +XP (from RewardTable or fallback)
///   Lose   = Player HP reaches 0                     -> -gold penalty
///   Flee   = Escape based on SPD comparison          -> -gold (flee penalty), no XP
///
/// PAUSE: MiniGameManager pauses PlayerLoopController before this starts.
///        MiniGameManager resumes it after OnMiniGameEnded fires.
/// </summary>
public class CombatGame : MonoBehaviour, IMiniGame
{
    // ── IMiniGame ─────────────────────────────────────────────────────────────
    public event Action<MiniGameResult> OnMiniGameEnded;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Configuration")]
    [SerializeField] private CombatConfig config;

    [Header("UI")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private CombatHUD hud;

    [Header("Camera")]
    [SerializeField] private Camera combatCamera;

    // ── Runtime State ─────────────────────────────────────────────────────────
    private CombatFighter playerFighter;
    private CombatFighter enemyFighter;
    private EnemyData currentEnemyData;
    private bool isPlayerTurn;
    private bool combatOver;
    private int turnCount;
    private CombatOutcome outcome;
    private RewardRollResult? cachedVictoryReward;

    // ── IMiniGame Implementation ──────────────────────────────────────────────

    /// <summary>
    /// Called by MiniGameManager to initialize and start combat.
    /// </summary>
    public void StartMiniGame(BoardTile sourceTile)
    {
        if (config == null)
        {
            Debug.LogError("[CombatGame] CombatConfig is null. Cannot start combat.");
            OnMiniGameEnded?.Invoke(MiniGameResult.Lose());
            return;
        }

        combatOver = false;
        turnCount = 0;
        outcome = CombatOutcome.None;
        cachedVictoryReward = null;

        // Activate UI
        if (uiRoot != null) uiRoot.SetActive(true);

        // Select enemy
        currentEnemyData = SelectEnemy();
        if (currentEnemyData == null)
        {
            Debug.LogError("[CombatGame] No enemy data available.");
            OnMiniGameEnded?.Invoke(MiniGameResult.Lose());
            return;
        }

        // Get loop count for scaling
        int loopCount = GetCurrentLoopCount();

        // Create fighters
        playerFighter = new CombatFighter("Heros", config.playerBaseStats);
        CombatStats enemyStats = currentEnemyData.GetScaledStats(loopCount);
        enemyFighter = new CombatFighter(currentEnemyData.enemyName, enemyStats);

        // Wire fighter events
        WireFighterEvents();

        // Initialize HUD
        if (hud != null)
        {
            hud.Initialize(playerFighter, enemyFighter, config.playerSkillName, config.fleeAllowed);
            hud.OnActionSelected += HandlePlayerAction;
            hud.OnResultClosed += HandleResultClosed;
            hud.LogMessage($"Un {currentEnemyData.enemyName} apparait !");
            hud.LogMessage("VICTOIRE : Reduire les HP ennemis a 0");
            hud.LogMessage("DEFAITE : Tes HP tombent a 0");
            if (config.fleeAllowed)
                hud.LogMessage("FUITE : Possible mais coute du gold");
        }

        // Determine first turn
        isPlayerTurn = CombatFormulas.DetermineFirstTurn(
            playerFighter.BaseStats.SPD,
            enemyFighter.BaseStats.SPD);

        string firstMsg = isPlayerTurn
            ? "Tu es plus rapide ! A toi de jouer."
            : $"{enemyFighter.Name} est plus rapide !";

        hud?.LogMessage(firstMsg);

        // Start combat loop
        StartCoroutine(CombatLoop());
    }

    // ── Combat Loop ───────────────────────────────────────────────────────────

    private IEnumerator CombatLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (!combatOver)
        {
            turnCount++;

            if (isPlayerTurn)
            {
                // Player turn
                playerFighter.OnTurnStart();
                hud?.SetTurnIndicator($"Tour {turnCount} - Ton tour");
                hud?.UpdateSkillCooldown(playerFighter.SkillCooldownRemaining);
                hud?.SetActionsVisible(true);

                // Wait for player action (handled via button callback)
                yield return new WaitUntil(() => !isPlayerTurn || combatOver);
            }
            else
            {
                // Enemy turn
                enemyFighter.OnTurnStart();
                hud?.SetTurnIndicator($"Tour {turnCount} - {enemyFighter.Name}");
                hud?.SetActionsVisible(false);

                yield return new WaitForSeconds(config.aiTurnDelay);

                if (!combatOver)
                    ExecuteEnemyTurn();

                yield return new WaitForSeconds(config.attackFeedbackDuration);

                // Check win/lose
                if (CheckCombatEnd())
                    yield break;

                isPlayerTurn = true;
            }
        }
    }

    // ── Player Actions ────────────────────────────────────────────────────────

    private void HandlePlayerAction(CombatActionType action)
    {
        if (combatOver || !isPlayerTurn) return;

        hud?.SetActionsVisible(false);

        switch (action)
        {
            case CombatActionType.Attack:
                ExecutePlayerAttack(1f);
                break;

            case CombatActionType.Defend:
                ExecutePlayerDefend();
                break;

            case CombatActionType.Skill:
                ExecutePlayerSkill();
                break;

            case CombatActionType.Flee:
                ExecutePlayerFlee();
                return; // Flee might end combat immediately

            case CombatActionType.Item:
                hud?.LogMessage("Pas d'objets disponibles pour le moment.");
                hud?.SetActionsVisible(true);
                return; // Don't end turn
        }

        if (!CheckCombatEnd())
            isPlayerTurn = false;
    }

    private void ExecutePlayerAttack(float multiplier)
    {
        CombatStats attackerStats = playerFighter.GetEffectiveStats();
        float defMult = enemyFighter.GetDefenseMultiplier(config.defenseReductionMultiplier);

        DamageResult result = CombatFormulas.CalculateDamage(
            attackerStats,
            enemyFighter.GetEffectiveStats(),
            config.randomDamageRange,
            config.minimumDamage,
            defMult,
            multiplier);

        enemyFighter.TakeDamage(result);
        hud?.UpdateEnemyHP(enemyFighter.CurrentHP, enemyFighter.BaseStats.MaxHP);

        if (result.Missed)
        {
            hud?.LogMessage("Tu attaques... Rate !");
        }
        else
        {
            string critText = result.IsCritical ? " CRITIQUE !" : "";
            hud?.LogMessage($"Tu infliges {result.Damage} degats !{critText}");
            hud?.ShakeTarget(false);
        }
    }

    private void ExecutePlayerDefend()
    {
        playerFighter.SetDefending(config.defenseDuration);
        hud?.LogMessage("Tu te mets en garde. Defense augmentee !");
    }

    private void ExecutePlayerSkill()
    {
        if (!playerFighter.CanUseSkill())
        {
            hud?.LogMessage($"{config.playerSkillName} en recharge !");
            hud?.SetActionsVisible(true);
            return;
        }

        playerFighter.UseSkill(config.playerSkillCooldown);
        hud?.LogMessage($"{config.playerSkillName} !");

        ExecutePlayerAttack(config.playerSkillMultiplier);
    }

    private void ExecutePlayerFlee()
    {
        if (!config.fleeAllowed)
        {
            hud?.LogMessage("Impossible de fuir !");
            hud?.SetActionsVisible(true);
            return;
        }

        bool success = CombatFormulas.TryFlee(
            playerFighter.BaseStats.SPD,
            enemyFighter.BaseStats.SPD,
            config.fleeBaseChance);

        if (success)
        {
            hud?.LogMessage("Tu t'enfuis avec succes !");
            EndCombat(CombatOutcome.Fled);
        }
        else
        {
            hud?.LogMessage("Fuite echouee !");
            isPlayerTurn = false;
        }
    }

    // ── Enemy Turn ────────────────────────────────────────────────────────────

    private void ExecuteEnemyTurn()
    {
        // Check stun
        if (enemyFighter.HasStatus(StatusEffectType.Stun))
        {
            hud?.LogMessage($"{enemyFighter.Name} est etourdi et ne peut pas agir !");
            return;
        }

        CombatAIDecision decision = CombatAI.DecideAction(enemyFighter, playerFighter, currentEnemyData);

        switch (decision)
        {
            case CombatAIDecision.Attack:
                ExecuteEnemyAttack(1f);
                break;

            case CombatAIDecision.Defend:
                enemyFighter.SetDefending(config.defenseDuration);
                hud?.LogMessage($"{enemyFighter.Name} se met en garde !");
                break;

            case CombatAIDecision.Skill:
                ExecuteEnemySkill();
                break;
        }

        // Try to inflict status effect
        TryInflictEnemyStatus();
    }

    private void ExecuteEnemyAttack(float multiplier)
    {
        CombatStats attackerStats = enemyFighter.GetEffectiveStats();
        float defMult = playerFighter.GetDefenseMultiplier(config.defenseReductionMultiplier);

        DamageResult result = CombatFormulas.CalculateDamage(
            attackerStats,
            playerFighter.GetEffectiveStats(),
            config.randomDamageRange,
            config.minimumDamage,
            defMult,
            multiplier);

        playerFighter.TakeDamage(result);
        hud?.UpdatePlayerHP(playerFighter.CurrentHP, playerFighter.BaseStats.MaxHP);

        if (result.Missed)
        {
            hud?.LogMessage($"{enemyFighter.Name} attaque... Rate !");
        }
        else
        {
            string critText = result.IsCritical ? " CRITIQUE !" : "";
            hud?.LogMessage($"{enemyFighter.Name} inflige {result.Damage} degats !{critText}");
            hud?.ShakeTarget(true);
        }
    }

    private void ExecuteEnemySkill()
    {
        if (!currentEnemyData.hasSpecialSkill || !enemyFighter.CanUseSkill())
        {
            ExecuteEnemyAttack(1f);
            return;
        }

        enemyFighter.UseSkill(currentEnemyData.skillCooldown);
        hud?.LogMessage($"{enemyFighter.Name} utilise {currentEnemyData.skillName} !");
        ExecuteEnemyAttack(currentEnemyData.skillDamageMultiplier);
    }

    private void TryInflictEnemyStatus()
    {
        if (currentEnemyData.inflictableStatus == StatusEffectType.None) return;
        if (UnityEngine.Random.value > currentEnemyData.statusInflictChance) return;

        StatusEffect effect = new StatusEffect(
            currentEnemyData.inflictableStatus,
            currentEnemyData.statusDuration,
            GetStatusValue(currentEnemyData.inflictableStatus));

        playerFighter.ApplyStatusEffect(effect);
        hud?.LogMessage($"Tu subis l'effet : {GetStatusName(currentEnemyData.inflictableStatus)} !");
    }

    // ── Win/Lose Check ────────────────────────────────────────────────────────

    private bool CheckCombatEnd()
    {
        if (!enemyFighter.IsAlive)
        {
            EndCombat(CombatOutcome.Victory);
            return true;
        }

        if (!playerFighter.IsAlive)
        {
            EndCombat(CombatOutcome.Defeat);
            return true;
        }

        return false;
    }

    private void EndCombat(CombatOutcome result)
    {
        if (combatOver) return;

        combatOver = true;
        outcome = result;

        hud?.SetActionsVisible(false);

        string details;
        bool victory;

        switch (result)
        {
            case CombatOutcome.Victory:
                victory = true;
                details = BuildVictoryDetails();
                hud?.LogMessage("VICTOIRE !");
                break;

            case CombatOutcome.Defeat:
                victory = false;
                details = $"Perdu {config.defeatGoldPenalty} gold.";
                hud?.LogMessage("Tu as ete vaincu...");
                break;

            case CombatOutcome.Fled:
                victory = false;
                details = $"Fuite ! Perdu {config.fleePenalty} gold.";
                break;

            default:
                victory = false;
                details = "";
                break;
        }

        hud?.ShowResult(victory, details);

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
        MiniGameResult result;

        switch (outcome)
        {
            case CombatOutcome.Victory:
                result = BuildVictoryResult();
                break;

            case CombatOutcome.Defeat:
                result = MiniGameResult.Lose(-config.defeatGoldPenalty);
                break;

            case CombatOutcome.Fled:
                result = MiniGameResult.Lose(-config.fleePenalty);
                break;

            default:
                result = MiniGameResult.Lose();
                break;
        }

        if (uiRoot != null) uiRoot.SetActive(false);

        // Unsubscribe
        if (hud != null)
        {
            hud.OnActionSelected -= HandlePlayerAction;
            hud.OnResultClosed -= HandleResultClosed;
        }

        OnMiniGameEnded?.Invoke(result);
    }

    // ── Reward Building ───────────────────────────────────────────────────────

    /// <summary>
    /// Rolls the reward once and caches it so display and result use the same values.
    /// </summary>
    private RewardRollResult RollVictoryReward()
    {
        if (cachedVictoryReward.HasValue)
            return cachedVictoryReward.Value;

        RewardTable table = currentEnemyData != null ? currentEnemyData.rewardTable : null;

        if (table == null)
            table = config.defaultRewardTable;

        if (table != null)
        {
            cachedVictoryReward = table.Roll();
        }
        else
        {
            cachedVictoryReward = new RewardRollResult
            {
                GoldAmount = config.fallbackGoldReward,
                XPAmount = config.fallbackXPReward
            };
        }

        return cachedVictoryReward.Value;
    }

    private MiniGameResult BuildVictoryResult()
    {
        RewardRollResult reward = RollVictoryReward();
        return reward.ToMiniGameResult(true, NarrativeFlags.CombatMiniGameWon);
    }

    private string BuildVictoryDetails()
    {
        RewardRollResult reward = RollVictoryReward();
        return $"+{reward.GoldAmount} Gold\n+{reward.XPAmount} XP";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private EnemyData SelectEnemy()
    {
        // Check for boss fight flag
        if (GameManager.Instance != null && GameManager.Instance.HasFlag(NarrativeFlags.LoopAware))
        {
            if (config.bossEnemy != null)
                return config.bossEnemy;
        }

        if (config.enemyPool == null || config.enemyPool.Length == 0)
        {
            Debug.LogError("[CombatGame] No enemies in pool.");
            return null;
        }

        return config.enemyPool[UnityEngine.Random.Range(0, config.enemyPool.Length)];
    }

    private int GetCurrentLoopCount()
    {
        if (PlayerLoopController.Instance != null)
            return PlayerLoopController.Instance.TotalLoops;
        return 0;
    }

    private void WireFighterEvents()
    {
        playerFighter.OnDeath += () => Debug.Log("[CombatGame] Player died.");
        enemyFighter.OnDeath += () => Debug.Log("[CombatGame] Enemy died.");
    }

    private static int GetStatusValue(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.Poison => 5,
            StatusEffectType.AttackDown => 3,
            StatusEffectType.DefenseDown => 3,
            StatusEffectType.SpeedDown => 3,
            _ => 0
        };
    }

    private static string GetStatusName(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.Poison => "Poison",
            StatusEffectType.Stun => "Etourdissement",
            StatusEffectType.AttackUp => "ATK+",
            StatusEffectType.AttackDown => "ATK-",
            StatusEffectType.DefenseUp => "DEF+",
            StatusEffectType.DefenseDown => "DEF-",
            StatusEffectType.SpeedUp => "SPD+",
            StatusEffectType.SpeedDown => "SPD-",
            _ => "Inconnu"
        };
    }
}

/// <summary>
/// Possible combat outcomes.
/// </summary>
public enum CombatOutcome
{
    None,
    Victory,
    Defeat,
    Fled
}
