using System;
using UnityEngine;

/// <summary>
/// ScriptableObject holding all tunable parameters for the Relic Slash (Fruit Ninja) mini-game.
/// Create via: Create > MiniGame > Relic Slash Config
/// </summary>
[CreateAssetMenu(fileName = "RelicSlashConfig", menuName = "MiniGame/Relic Slash Config")]
public class RelicSlashConfig : ScriptableObject
{
    // ── Timer ─────────────────────────────────────────────────────────────────
    [Header("Timer")]
    [Tooltip("Duration of the mini-game in seconds.")]
    [Range(10f, 120f)]
    public float gameDuration = 30f;

    // ── Scoring ───────────────────────────────────────────────────────────────
    [Header("Scoring")]
    [Tooltip("Points per slashed object.")]
    public int pointsPerSlash = 10;

    [Tooltip("Minimum score required to win.")]
    public int scoreToWin = 100;

    // ── Combo ─────────────────────────────────────────────────────────────────
    [Header("Combo System")]
    [Tooltip("Time window (seconds) to chain combos.")]
    [Range(0.3f, 2f)]
    public float comboTimeWindow = 0.8f;

    [Tooltip("Combo multiplier thresholds. Index = combo level, value = required consecutive hits.")]
    public ComboThreshold[] comboThresholds = new ComboThreshold[]
    {
        new ComboThreshold { requiredHits = 3, multiplier = 2f, displayText = "x2" },
        new ComboThreshold { requiredHits = 5, multiplier = 3f, displayText = "x3" },
        new ComboThreshold { requiredHits = 8, multiplier = 5f, displayText = "x5" }
    };

    // ── Spawning ──────────────────────────────────────────────────────────────
    [Header("Object Spawning")]
    [Tooltip("Initial spawn interval (seconds between launches).")]
    [Range(0.3f, 3f)]
    public float initialSpawnInterval = 1.2f;

    [Tooltip("Minimum spawn interval at max difficulty.")]
    [Range(0.1f, 1f)]
    public float minSpawnInterval = 0.3f;

    [Tooltip("How fast the spawn rate increases over time.")]
    [Range(0f, 1f)]
    public float spawnAcceleration = 0.02f;

    [Tooltip("Maximum number of objects on screen at once.")]
    [Range(1, 20)]
    public int maxObjectsOnScreen = 10;

    // ── Object Physics ────────────────────────────────────────────────────────
    [Header("Object Physics")]
    [Tooltip("Minimum upward launch speed.")]
    [Range(3f, 15f)]
    public float minLaunchSpeed = 6f;

    [Tooltip("Maximum upward launch speed.")]
    [Range(5f, 25f)]
    public float maxLaunchSpeed = 12f;

    [Tooltip("Horizontal spread range for launch direction.")]
    [Range(0f, 10f)]
    public float horizontalSpread = 3f;

    [Tooltip("Gravity affecting launched objects.")]
    [Range(5f, 30f)]
    public float gravity = 15f;

    [Tooltip("Y position below which objects are destroyed.")]
    public float destroyBelowY = -2f;

    // ── Traps ─────────────────────────────────────────────────────────────────
    [Header("Traps")]
    [Tooltip("Chance (0-1) that a spawned object is a trap (bomb).")]
    [Range(0f, 0.5f)]
    public float trapChance = 0.15f;

    [Tooltip("Score penalty for hitting a trap.")]
    public int trapScorePenalty = 30;

    [Tooltip("Time penalty (seconds) for hitting a trap.")]
    [Range(0f, 10f)]
    public float trapTimePenalty = 3f;

    [Tooltip("Whether hitting a trap resets the combo.")]
    public bool trapResetsCombo = true;

    // ── Object Prefabs ────────────────────────────────────────────────────────
    [Header("Prefabs")]
    [Tooltip("Slashable object prefabs (relics, gems, etc.).")]
    public SlashableObjectData[] objectTypes;

    [Tooltip("Trap/bomb prefab data.")]
    public SlashableObjectData trapData;

    // ── Rewards ───────────────────────────────────────────────────────────────
    [Header("Rewards")]
    [Tooltip("Reward table for this mini-game. If null, fallback values are used.")]
    public RewardTable rewardTable;

    [Tooltip("Fallback gold reward on win.")]
    public int fallbackGoldReward = 30;

    [Tooltip("Fallback XP reward on win.")]
    public int fallbackXPReward = 20;

    [Tooltip("Gold penalty on loss.")]
    public int defeatGoldPenalty = 10;

    // ── HUD ───────────────────────────────────────────────────────────────────
    [Header("HUD")]
    [Tooltip("Duration of the result screen (seconds).")]
    [Range(1f, 5f)]
    public float resultDisplayDuration = 2.5f;

    /// <summary>
    /// Returns the current combo multiplier based on consecutive hits.
    /// </summary>
    public float GetComboMultiplier(int consecutiveHits)
    {
        float multiplier = 1f;

        if (comboThresholds == null) return multiplier;

        for (int i = comboThresholds.Length - 1; i >= 0; i--)
        {
            if (consecutiveHits >= comboThresholds[i].requiredHits)
            {
                multiplier = comboThresholds[i].multiplier;
                break;
            }
        }

        return multiplier;
    }

    /// <summary>
    /// Returns the combo display text for the current hit count.
    /// </summary>
    public string GetComboText(int consecutiveHits)
    {
        if (comboThresholds == null) return "";

        for (int i = comboThresholds.Length - 1; i >= 0; i--)
        {
            if (consecutiveHits >= comboThresholds[i].requiredHits)
                return comboThresholds[i].displayText;
        }

        return "";
    }
}

/// <summary>
/// Defines a combo threshold level.
/// </summary>
[Serializable]
public class ComboThreshold
{
    public int requiredHits;
    public float multiplier;
    public string displayText;
}

/// <summary>
/// Data defining a type of slashable object.
/// </summary>
[Serializable]
public class SlashableObjectData
{
    [Tooltip("Display name.")]
    public string objectName = "Relique";

    [Tooltip("Color for the object.")]
    public Color objectColor = Color.yellow;

    [Tooltip("Size scale.")]
    [Range(0.5f, 3f)]
    public float scale = 1f;

    [Tooltip("Bonus points for this specific type.")]
    public int bonusPoints = 0;
}
