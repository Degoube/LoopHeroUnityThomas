using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility for rolling rewards from a RewardTable.
/// Handles weighted selection, rarity multipliers, and difficulty scaling.
/// </summary>
public static class RewardRoller
{
    private const float UncommonMultiplier = 1.25f;
    private const float RareMultiplier = 1.5f;
    private const float EpicMultiplier = 2f;
    private const float LegendaryMultiplier = 3f;

    /// <summary>
    /// Rolls all entries from a reward table using weighted random selection.
    /// Each entry is independently checked against its weight as a percentage.
    /// </summary>
    public static RewardRollResult RollFromTable(RewardTable table)
    {
        RewardRollResult result = new RewardRollResult
        {
            GoldAmount = table.guaranteedMinGold,
            XPAmount = table.guaranteedMinXP,
            HighestRarity = RewardRarity.Common
        };

        if (table.entries == null || table.entries.Count == 0)
            return result;

        float totalWeight = 0f;
        foreach (RewardEntry entry in table.entries)
            totalWeight += entry.weight;

        if (totalWeight <= 0f)
            return result;

        // Roll a weighted random entry
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (RewardEntry entry in table.entries)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                ApplyEntry(ref result, entry, table.difficultyMultiplier);
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Rolls multiple rewards from a table (for bonus/jackpot scenarios).
    /// </summary>
    public static RewardRollResult RollMultiple(RewardTable table, int rollCount)
    {
        RewardRollResult combined = new RewardRollResult
        {
            GoldAmount = table.guaranteedMinGold,
            XPAmount = table.guaranteedMinXP,
            HighestRarity = RewardRarity.Common
        };

        for (int i = 0; i < rollCount; i++)
        {
            RewardRollResult single = RollFromTable(table);
            combined.GoldAmount += single.GoldAmount - table.guaranteedMinGold;
            combined.XPAmount += single.XPAmount - table.guaranteedMinXP;
            combined.HealAmount += single.HealAmount;

            if (!combined.HasBuff && single.HasBuff)
            {
                combined.HasBuff = true;
                combined.BuffName = single.BuffName;
            }

            if (!combined.HasRelic && single.HasRelic)
            {
                combined.HasRelic = true;
                combined.RelicName = single.RelicName;
            }

            if (!combined.HasRareItem && single.HasRareItem)
            {
                combined.HasRareItem = true;
                combined.RareItemName = single.RareItemName;
            }

            if (single.HighestRarity > combined.HighestRarity)
                combined.HighestRarity = single.HighestRarity;
        }

        return combined;
    }

    private static void ApplyEntry(ref RewardRollResult result, RewardEntry entry, float difficultyMultiplier)
    {
        float rarityMult = GetRarityMultiplier(entry.rarity);
        int baseQuantity = Random.Range(entry.quantityMin, entry.quantityMax + 1);
        int scaledQuantity = Mathf.RoundToInt(baseQuantity * rarityMult * difficultyMultiplier);

        if (entry.rarity > result.HighestRarity)
            result.HighestRarity = entry.rarity;

        switch (entry.rewardType)
        {
            case RewardType.Gold:
                result.GoldAmount += scaledQuantity;
                break;

            case RewardType.XP:
                result.XPAmount += scaledQuantity;
                break;

            case RewardType.Heal:
                result.HealAmount += scaledQuantity;
                break;

            case RewardType.TemporaryBuff:
                result.HasBuff = true;
                result.BuffName = string.IsNullOrEmpty(entry.displayName) ? "Buff" : entry.displayName;
                break;

            case RewardType.Relic:
                result.HasRelic = true;
                result.RelicName = string.IsNullOrEmpty(entry.displayName) ? "Relique" : entry.displayName;
                result.GoldAmount += scaledQuantity;
                break;

            case RewardType.RareItem:
                result.HasRareItem = true;
                result.RareItemName = string.IsNullOrEmpty(entry.displayName) ? "Objet Rare" : entry.displayName;
                result.GoldAmount += scaledQuantity;
                break;
        }
    }

    private static float GetRarityMultiplier(RewardRarity rarity)
    {
        return rarity switch
        {
            RewardRarity.Uncommon => UncommonMultiplier,
            RewardRarity.Rare => RareMultiplier,
            RewardRarity.Epic => EpicMultiplier,
            RewardRarity.Legendary => LegendaryMultiplier,
            _ => 1f
        };
    }
}
