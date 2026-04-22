using UnityEngine;

/// <summary>
/// ScriptableObject defining an enemy's identity, stats, and AI behavior.
/// Create via: Create > MiniGame > Combat > Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "MiniGame/Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown during combat.")]
    public string enemyName = "Ennemi";

    [Tooltip("Optional sprite for UI display.")]
    public Sprite portrait;

    [Tooltip("Color tint for the enemy in combat UI.")]
    public Color enemyColor = Color.red;

    [Header("Base Stats")]
    public CombatStats baseStats = CombatStats.DefaultEnemy();

    [Header("AI Behavior")]
    [Tooltip("AI personality determining combat decisions.")]
    public EnemyAIType aiType = EnemyAIType.Balanced;

    [Header("AI Tuning")]
    [Tooltip("HP threshold (0-1) below which the AI considers defensive actions.")]
    [Range(0f, 1f)]
    public float defensiveHPThreshold = 0.3f;

    [Tooltip("Chance (0-1) the AI will use a skill when available.")]
    [Range(0f, 1f)]
    public float skillUseChance = 0.3f;

    [Header("Special Skill")]
    [Tooltip("If true, this enemy has a special skill.")]
    public bool hasSpecialSkill = false;

    [Tooltip("Name of the special skill for UI display.")]
    public string skillName = "Coup Puissant";

    [Tooltip("Damage multiplier for the special skill.")]
    [Range(1f, 5f)]
    public float skillDamageMultiplier = 1.8f;

    [Tooltip("Cooldown in turns between skill uses.")]
    [Range(0, 10)]
    public int skillCooldown = 3;

    [Header("Status Effect (future)")]
    [Tooltip("Status effect this enemy can inflict.")]
    public StatusEffectType inflictableStatus = StatusEffectType.None;

    [Tooltip("Chance to inflict the status effect.")]
    [Range(0f, 1f)]
    public float statusInflictChance = 0.2f;

    [Tooltip("Duration in turns of inflicted status.")]
    [Range(1, 5)]
    public int statusDuration = 2;

    [Header("Rewards")]
    [Tooltip("Optional reward table. If null, CombatConfig defaults apply.")]
    public RewardTable rewardTable;

    [Header("Scaling")]
    [Tooltip("Stat multiplier per game loop completed.")]
    [Range(1f, 2f)]
    public float loopScalingFactor = 1.1f;

    /// <summary>
    /// Returns scaled stats based on the current loop number.
    /// </summary>
    public CombatStats GetScaledStats(int loopCount)
    {
        float scale = 1f + (loopScalingFactor - 1f) * loopCount;
        CombatStats scaled = baseStats;
        scaled.MaxHP = Mathf.RoundToInt(scaled.MaxHP * scale);
        scaled.ATK = Mathf.RoundToInt(scaled.ATK * scale);
        scaled.DEF = Mathf.RoundToInt(scaled.DEF * scale);
        scaled.SPD = Mathf.RoundToInt(scaled.SPD * scale);
        return scaled;
    }
}

/// <summary>
/// AI behavior profiles for enemy combat decisions.
/// </summary>
public enum EnemyAIType
{
    /// <summary>Focuses on dealing damage. Rarely defends.</summary>
    Aggressive,

    /// <summary>Defends when HP is low. Balanced approach.</summary>
    Defensive,

    /// <summary>Mix of attack, defense, and skills.</summary>
    Balanced,

    /// <summary>Smart decisions, frequent skill use. For boss encounters.</summary>
    Boss
}
