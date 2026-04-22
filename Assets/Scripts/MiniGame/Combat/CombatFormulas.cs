using UnityEngine;

/// <summary>
/// Static utility class for all combat calculations.
/// Centralized formulas allow easy balancing and modification.
/// </summary>
public static class CombatFormulas
{
    /// <summary>
    /// Calculates damage dealt from attacker to defender.
    /// Formula: max(minDamage, ATK - DEF + random(-range, range))
    /// Applies crit chance and accuracy checks.
    /// </summary>
    public static DamageResult CalculateDamage(
        CombatStats attacker,
        CombatStats defender,
        int randomRange,
        int minimumDamage,
        float defenseMultiplier = 1f,
        float skillMultiplier = 1f)
    {
        DamageResult result = new DamageResult();

        // Accuracy check
        if (Random.value > attacker.Accuracy)
        {
            result.Missed = true;
            result.Damage = 0;
            return result;
        }

        // Base damage
        int baseDamage = attacker.ATK - Mathf.RoundToInt(defender.DEF * defenseMultiplier);
        int randomBonus = Random.Range(-randomRange, randomRange + 1);
        int totalDamage = baseDamage + randomBonus;

        // Skill multiplier
        totalDamage = Mathf.RoundToInt(totalDamage * skillMultiplier);

        // Critical hit
        if (Random.value < attacker.CritChance)
        {
            totalDamage = Mathf.RoundToInt(totalDamage * attacker.CritMultiplier);
            result.IsCritical = true;
        }

        // Floor
        result.Damage = Mathf.Max(minimumDamage, totalDamage);
        return result;
    }

    /// <summary>
    /// Determines which fighter goes first based on SPD stats.
    /// Returns true if fighter A goes first.
    /// On ties, adds a small random factor.
    /// </summary>
    public static bool DetermineFirstTurn(int speedA, int speedB)
    {
        if (speedA == speedB)
            return Random.value >= 0.5f;

        return speedA > speedB;
    }

    /// <summary>
    /// Calculates flee success chance based on player and enemy speed.
    /// Faster players have higher flee chance.
    /// </summary>
    public static bool TryFlee(int playerSpeed, int enemySpeed, float baseFleeChance)
    {
        float speedFactor = (float)playerSpeed / Mathf.Max(1, enemySpeed);
        float finalChance = Mathf.Clamp01(baseFleeChance * speedFactor);
        return Random.value < finalChance;
    }

    /// <summary>
    /// Calculates effective defense multiplier when a fighter is defending.
    /// Returns a value to multiply against the defender's DEF (> 1 means stronger defense).
    /// </summary>
    public static float GetDefenseBoostMultiplier(float defenseReduction)
    {
        // defenseReduction of 0.5 means incoming damage is halved
        // We invert it to boost DEF: 1 / 0.5 = 2x DEF
        return 1f / Mathf.Max(0.1f, defenseReduction);
    }
}

/// <summary>
/// Result of a damage calculation.
/// </summary>
public struct DamageResult
{
    public int Damage;
    public bool IsCritical;
    public bool Missed;
}
