using UnityEngine;

/// <summary>
/// AI decision-making logic for enemy turns in combat.
/// Selects actions based on the enemy's AI type and current state.
/// </summary>
public static class CombatAI
{
    /// <summary>
    /// Decides what action the enemy should take this turn.
    /// </summary>
    public static CombatAIDecision DecideAction(
        CombatFighter enemy,
        CombatFighter player,
        EnemyData enemyData)
    {
        return enemyData.aiType switch
        {
            EnemyAIType.Aggressive => DecideAggressive(enemy, player, enemyData),
            EnemyAIType.Defensive => DecideDefensive(enemy, player, enemyData),
            EnemyAIType.Boss => DecideBoss(enemy, player, enemyData),
            _ => DecideBalanced(enemy, player, enemyData)
        };
    }

    /// <summary>
    /// Aggressive AI: attacks most of the time, uses skills when available.
    /// Rarely defends (only at very low HP).
    /// </summary>
    private static CombatAIDecision DecideAggressive(
        CombatFighter enemy,
        CombatFighter player,
        EnemyData data)
    {
        // Use skill if available and randomly triggered
        if (data.hasSpecialSkill && enemy.CanUseSkill() && Random.value < data.skillUseChance * 1.5f)
            return CombatAIDecision.Skill;

        // Defend only at very low HP
        if (enemy.HPRatio < data.defensiveHPThreshold * 0.5f && Random.value < 0.3f)
            return CombatAIDecision.Defend;

        return CombatAIDecision.Attack;
    }

    /// <summary>
    /// Defensive AI: defends frequently when HP is low, attacks otherwise.
    /// </summary>
    private static CombatAIDecision DecideDefensive(
        CombatFighter enemy,
        CombatFighter player,
        EnemyData data)
    {
        // Defend when HP is below threshold
        if (enemy.HPRatio < data.defensiveHPThreshold && Random.value < 0.6f)
            return CombatAIDecision.Defend;

        // Use skill to finish off low-HP player
        if (data.hasSpecialSkill && enemy.CanUseSkill() && player.HPRatio < 0.3f)
            return CombatAIDecision.Skill;

        // Sometimes defend even when healthy
        if (Random.value < 0.2f)
            return CombatAIDecision.Defend;

        return CombatAIDecision.Attack;
    }

    /// <summary>
    /// Balanced AI: mix of all actions based on situation.
    /// </summary>
    private static CombatAIDecision DecideBalanced(
        CombatFighter enemy,
        CombatFighter player,
        EnemyData data)
    {
        // Defend when low
        if (enemy.HPRatio < data.defensiveHPThreshold && Random.value < 0.4f)
            return CombatAIDecision.Defend;

        // Use skill occasionally
        if (data.hasSpecialSkill && enemy.CanUseSkill() && Random.value < data.skillUseChance)
            return CombatAIDecision.Skill;

        // Small chance to defend even when healthy
        if (Random.value < 0.15f)
            return CombatAIDecision.Defend;

        return CombatAIDecision.Attack;
    }

    /// <summary>
    /// Boss AI: smart and aggressive. Uses skills frequently, defends strategically.
    /// </summary>
    private static CombatAIDecision DecideBoss(
        CombatFighter enemy,
        CombatFighter player,
        EnemyData data)
    {
        // If player is low, go for the kill with skill
        if (data.hasSpecialSkill && enemy.CanUseSkill() && player.HPRatio < 0.25f)
            return CombatAIDecision.Skill;

        // Defend strategically when low
        if (enemy.HPRatio < data.defensiveHPThreshold)
        {
            // 50/50 defend or skill (to heal/recover in future extensions)
            if (Random.value < 0.5f)
                return CombatAIDecision.Defend;
        }

        // Use skill frequently
        if (data.hasSpecialSkill && enemy.CanUseSkill() && Random.value < data.skillUseChance * 2f)
            return CombatAIDecision.Skill;

        // Boss attacks aggressively
        return CombatAIDecision.Attack;
    }
}

/// <summary>
/// Result of an AI combat decision.
/// </summary>
public enum CombatAIDecision
{
    Attack,
    Defend,
    Skill
}
