using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject loot table defining weighted reward entries.
/// Create via: Create > MiniGame > Reward Table
/// </summary>
[CreateAssetMenu(fileName = "NewRewardTable", menuName = "MiniGame/Reward Table")]
public class RewardTable : ScriptableObject
{
    [Header("Reward Entries")]
    [Tooltip("List of possible rewards with weights and ranges.")]
    public List<RewardEntry> entries = new List<RewardEntry>();

    [Header("Difficulty Scaling")]
    [Tooltip("Multiplier applied to reward quantities based on difficulty level.")]
    [Range(0.5f, 3f)]
    public float difficultyMultiplier = 1f;

    [Header("Guaranteed Minimums")]
    [Tooltip("Minimum gold always granted regardless of rolls.")]
    public int guaranteedMinGold = 5;

    [Tooltip("Minimum XP always granted regardless of rolls.")]
    public int guaranteedMinXP = 5;

    /// <summary>
    /// Rolls rewards from this table and returns a consolidated result.
    /// </summary>
    public RewardRollResult Roll()
    {
        return RewardRoller.RollFromTable(this);
    }
}

/// <summary>
/// Single entry in a RewardTable defining one possible reward outcome.
/// </summary>
[Serializable]
public class RewardEntry
{
    [Tooltip("Type of reward.")]
    public RewardType rewardType = RewardType.Gold;

    [Tooltip("Probability weight (higher = more likely relative to other entries).")]
    [Range(0f, 100f)]
    public float weight = 50f;

    [Tooltip("Rarity tier affecting quantity bonus.")]
    public RewardRarity rarity = RewardRarity.Common;

    [Tooltip("Minimum quantity granted when this entry is selected.")]
    public int quantityMin = 10;

    [Tooltip("Maximum quantity granted when this entry is selected.")]
    public int quantityMax = 30;

    [Tooltip("Optional display name for UI feedback.")]
    public string displayName;
}

/// <summary>
/// Rarity tiers affecting reward multipliers.
/// </summary>
public enum RewardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Consolidated result from rolling a reward table.
/// </summary>
public struct RewardRollResult
{
    public int GoldAmount;
    public int XPAmount;
    public int HealAmount;
    public bool HasBuff;
    public string BuffName;
    public bool HasRelic;
    public string RelicName;
    public bool HasRareItem;
    public string RareItemName;
    public RewardRarity HighestRarity;

    /// <summary>
    /// Converts this result into a MiniGameResult for integration with MiniGameManager.
    /// </summary>
    public MiniGameResult ToMiniGameResult(bool success, string flagToAdd = null)
    {
        return new MiniGameResult
        {
            Success = success,
            ResourceDelta = success ? GoldAmount : -GoldAmount / 2,
            XPDelta = XPAmount,
            FlagToAdd = flagToAdd
        };
    }
}
