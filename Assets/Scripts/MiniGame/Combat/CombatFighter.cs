using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime representation of a combat participant (player or enemy).
/// Manages current HP, status effects, cooldowns, and defense state.
/// </summary>
public class CombatFighter
{
    /// <summary>Display name for UI.</summary>
    public string Name { get; private set; }

    /// <summary>Base stats before any modifiers.</summary>
    public CombatStats BaseStats { get; private set; }

    /// <summary>Current HP.</summary>
    public int CurrentHP { get; private set; }

    /// <summary>True if this fighter is defending this turn.</summary>
    public bool IsDefending { get; private set; }

    /// <summary>Remaining cooldown turns before the special skill is available.</summary>
    public int SkillCooldownRemaining { get; private set; }

    /// <summary>True if the fighter is alive.</summary>
    public bool IsAlive => CurrentHP > 0;

    /// <summary>HP as a 0-1 ratio for UI bars.</summary>
    public float HPRatio => BaseStats.MaxHP > 0 ? (float)CurrentHP / BaseStats.MaxHP : 0f;

    /// <summary>Active status effects on this fighter.</summary>
    public List<StatusEffect> ActiveEffects { get; private set; } = new List<StatusEffect>();

    /// <summary>Fired when HP changes. Args: (currentHP, maxHP, damageResult).</summary>
    public event Action<int, int, DamageResult> OnHPChanged;

    /// <summary>Fired when this fighter dies.</summary>
    public event Action OnDeath;

    /// <summary>Fired when a status effect is applied or removed.</summary>
    public event Action<StatusEffect, bool> OnStatusEffectChanged;

    private int defenseBoostTurnsRemaining;

    public CombatFighter(string name, CombatStats stats)
    {
        Name = name;
        BaseStats = stats;
        CurrentHP = stats.MaxHP;
        SkillCooldownRemaining = 0;
        IsDefending = false;
        defenseBoostTurnsRemaining = 0;
    }

    /// <summary>
    /// Applies damage to this fighter. Returns actual damage dealt.
    /// </summary>
    public int TakeDamage(DamageResult damageResult)
    {
        if (!IsAlive || damageResult.Missed)
        {
            OnHPChanged?.Invoke(CurrentHP, BaseStats.MaxHP, damageResult);
            return 0;
        }

        int actualDamage = damageResult.Damage;
        CurrentHP = Mathf.Max(0, CurrentHP - actualDamage);

        OnHPChanged?.Invoke(CurrentHP, BaseStats.MaxHP, damageResult);

        if (!IsAlive)
            OnDeath?.Invoke();

        return actualDamage;
    }

    /// <summary>
    /// Heals this fighter by the specified amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        CurrentHP = Mathf.Min(BaseStats.MaxHP, CurrentHP + amount);
        DamageResult healResult = new DamageResult { Damage = -amount };
        OnHPChanged?.Invoke(CurrentHP, BaseStats.MaxHP, healResult);
    }

    /// <summary>
    /// Sets the fighter into defense stance for the specified number of turns.
    /// </summary>
    public void SetDefending(int turns)
    {
        IsDefending = true;
        defenseBoostTurnsRemaining = turns;
    }

    /// <summary>
    /// Returns the effective defense multiplier (boosted if defending).
    /// </summary>
    public float GetDefenseMultiplier(float defenseReduction)
    {
        return IsDefending ? CombatFormulas.GetDefenseBoostMultiplier(defenseReduction) : 1f;
    }

    /// <summary>
    /// Uses the special skill. Sets cooldown.
    /// </summary>
    public void UseSkill(int cooldown)
    {
        SkillCooldownRemaining = cooldown;
    }

    /// <summary>
    /// Returns true if the special skill is available (cooldown expired).
    /// </summary>
    public bool CanUseSkill()
    {
        return SkillCooldownRemaining <= 0;
    }

    /// <summary>
    /// Called at the start of each turn to tick cooldowns and status effects.
    /// </summary>
    public void OnTurnStart()
    {
        // Tick skill cooldown
        if (SkillCooldownRemaining > 0)
            SkillCooldownRemaining--;

        // Tick defense
        if (IsDefending)
        {
            defenseBoostTurnsRemaining--;
            if (defenseBoostTurnsRemaining <= 0)
                IsDefending = false;
        }

        // Tick status effects
        TickStatusEffects();
    }

    /// <summary>
    /// Applies a new status effect to this fighter.
    /// </summary>
    public void ApplyStatusEffect(StatusEffect effect)
    {
        // Check if same type exists — refresh duration
        for (int i = 0; i < ActiveEffects.Count; i++)
        {
            if (ActiveEffects[i].Type == effect.Type)
            {
                ActiveEffects[i].RemainingTurns = effect.RemainingTurns;
                return;
            }
        }

        ActiveEffects.Add(effect);
        OnStatusEffectChanged?.Invoke(effect, true);
    }

    /// <summary>
    /// Returns true if the fighter has the specified status effect.
    /// </summary>
    public bool HasStatus(StatusEffectType type)
    {
        foreach (StatusEffect effect in ActiveEffects)
        {
            if (effect.Type == type && !effect.IsExpired)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets effective stats considering active status effects.
    /// </summary>
    public CombatStats GetEffectiveStats()
    {
        CombatStats effective = BaseStats;

        foreach (StatusEffect effect in ActiveEffects)
        {
            if (effect.IsExpired) continue;

            switch (effect.Type)
            {
                case StatusEffectType.AttackUp:
                    effective.ATK += effect.ValuePerTurn;
                    break;
                case StatusEffectType.AttackDown:
                    effective.ATK = Mathf.Max(1, effective.ATK - effect.ValuePerTurn);
                    break;
                case StatusEffectType.DefenseUp:
                    effective.DEF += effect.ValuePerTurn;
                    break;
                case StatusEffectType.DefenseDown:
                    effective.DEF = Mathf.Max(0, effective.DEF - effect.ValuePerTurn);
                    break;
                case StatusEffectType.SpeedUp:
                    effective.SPD += effect.ValuePerTurn;
                    break;
                case StatusEffectType.SpeedDown:
                    effective.SPD = Mathf.Max(1, effective.SPD - effect.ValuePerTurn);
                    break;
            }
        }

        return effective;
    }

    private void TickStatusEffects()
    {
        for (int i = ActiveEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = ActiveEffects[i];

            // Apply per-turn effect (poison damage, etc.)
            if (effect.Type == StatusEffectType.Poison && IsAlive)
            {
                CurrentHP = Mathf.Max(0, CurrentHP - effect.ValuePerTurn);
                DamageResult poisonResult = new DamageResult { Damage = effect.ValuePerTurn };
                OnHPChanged?.Invoke(CurrentHP, BaseStats.MaxHP, poisonResult);

                if (!IsAlive)
                {
                    OnDeath?.Invoke();
                    return;
                }
            }

            if (!effect.Tick())
            {
                OnStatusEffectChanged?.Invoke(effect, false);
                ActiveEffects.RemoveAt(i);
            }
        }
    }
}
