using UnityEngine;

/// <summary>
/// ScriptableObject holding all tunable parameters for the turn-based combat mini-game.
/// Create via: Create > MiniGame > Combat > Combat Config
/// </summary>
[CreateAssetMenu(fileName = "CombatConfig", menuName = "MiniGame/Combat/Combat Config")]
public class CombatConfig : ScriptableObject
{
    // ── Player ────────────────────────────────────────────────────────────────
    [Header("Player Base Stats")]
    [Tooltip("Default player stats. Can be overridden by a character system later.")]
    public CombatStats playerBaseStats = CombatStats.DefaultPlayer();

    // ── Enemies ───────────────────────────────────────────────────────────────
    [Header("Enemy Pool")]
    [Tooltip("Possible enemies for random encounters. One is picked at random.")]
    public EnemyData[] enemyPool;

    [Tooltip("Boss enemy data. Used when NarrativeFlags indicate a boss fight.")]
    public EnemyData bossEnemy;

    // ── Combat Formulas ───────────────────────────────────────────────────────
    [Header("Damage Formula")]
    [Tooltip("Random bonus range added/subtracted to base damage.")]
    [Range(0, 20)]
    public int randomDamageRange = 5;

    [Tooltip("Minimum damage dealt per hit (prevents zero damage).")]
    [Range(1, 10)]
    public int minimumDamage = 1;

    // ── Defense ───────────────────────────────────────────────────────────────
    [Header("Defense Action")]
    [Tooltip("Damage reduction multiplier when defending (0.5 = 50% reduction).")]
    [Range(0.1f, 0.9f)]
    public float defenseReductionMultiplier = 0.5f;

    [Tooltip("Number of turns the defense buff lasts.")]
    [Range(1, 3)]
    public int defenseDuration = 1;

    // ── Flee ──────────────────────────────────────────────────────────────────
    [Header("Flee")]
    [Tooltip("Base chance to flee successfully (0-1).")]
    [Range(0f, 1f)]
    public float fleeBaseChance = 0.4f;

    [Tooltip("If true, fleeing is allowed in this combat.")]
    public bool fleeAllowed = true;

    [Tooltip("Resource penalty when fleeing successfully.")]
    public int fleePenalty = 10;

    // ── Rewards ───────────────────────────────────────────────────────────────
    [Header("Default Rewards")]
    [Tooltip("Default reward table used when the enemy has no specific table.")]
    public RewardTable defaultRewardTable;

    [Tooltip("Gold reward on victory if no reward table is assigned.")]
    public int fallbackGoldReward = 25;

    [Tooltip("XP reward on victory if no reward table is assigned.")]
    public int fallbackXPReward = 15;

    [Tooltip("Gold penalty on defeat.")]
    public int defeatGoldPenalty = 15;

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Delay before AI plays its turn (seconds).")]
    [Range(0.3f, 2f)]
    public float aiTurnDelay = 0.8f;

    [Tooltip("Duration of attack animation/feedback (seconds).")]
    [Range(0.2f, 1.5f)]
    public float attackFeedbackDuration = 0.4f;

    [Tooltip("Duration of the result screen before auto-closing (seconds).")]
    [Range(1f, 5f)]
    public float resultDisplayDuration = 2.5f;

    // ── Special Skill (Player) ────────────────────────────────────────────────
    [Header("Player Special Skill")]
    [Tooltip("Name of the player's special skill.")]
    public string playerSkillName = "Frappe Heroique";

    [Tooltip("Damage multiplier for the player's special skill.")]
    [Range(1f, 5f)]
    public float playerSkillMultiplier = 2f;

    [Tooltip("Cooldown in turns for the player's special skill.")]
    [Range(1, 10)]
    public int playerSkillCooldown = 3;
}
