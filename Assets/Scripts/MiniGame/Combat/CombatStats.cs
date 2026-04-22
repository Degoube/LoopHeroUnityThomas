using System;
using UnityEngine;

/// <summary>
/// Core combat statistics shared by player and enemies.
/// Serializable for ScriptableObject and save system compatibility.
/// </summary>
[Serializable]
public struct CombatStats
{
    [Tooltip("Maximum hit points.")]
    public int MaxHP;

    [Tooltip("Attack power — base damage dealt.")]
    public int ATK;

    [Tooltip("Defense — reduces incoming damage.")]
    public int DEF;

    [Tooltip("Speed — determines turn order (higher goes first).")]
    public int SPD;

    [Tooltip("Critical hit chance (0 to 1).")]
    [Range(0f, 1f)]
    public float CritChance;

    [Tooltip("Critical hit damage multiplier.")]
    [Range(1f, 4f)]
    public float CritMultiplier;

    [Tooltip("Accuracy — chance to land an attack (0 to 1).")]
    [Range(0f, 1f)]
    public float Accuracy;

    /// <summary>
    /// Returns default balanced stats for a player character.
    /// </summary>
    public static CombatStats DefaultPlayer()
    {
        return new CombatStats
        {
            MaxHP = 100,
            ATK = 20,
            DEF = 10,
            SPD = 15,
            CritChance = 0.1f,
            CritMultiplier = 1.5f,
            Accuracy = 0.9f
        };
    }

    /// <summary>
    /// Returns default stats for a basic enemy.
    /// </summary>
    public static CombatStats DefaultEnemy()
    {
        return new CombatStats
        {
            MaxHP = 80,
            ATK = 15,
            DEF = 8,
            SPD = 10,
            CritChance = 0.05f,
            CritMultiplier = 1.5f,
            Accuracy = 0.85f
        };
    }
}

/// <summary>
/// Types of actions available during combat.
/// </summary>
public enum CombatActionType
{
    Attack,
    Defend,
    Skill,
    Item,
    Flee
}

/// <summary>
/// Status effects that can be applied during combat.
/// Prepared for future extension.
/// </summary>
public enum StatusEffectType
{
    None,
    Poison,
    Stun,
    AttackUp,
    AttackDown,
    DefenseUp,
    DefenseDown,
    SpeedUp,
    SpeedDown
}

/// <summary>
/// Runtime data for an active status effect on a fighter.
/// </summary>
[Serializable]
public class StatusEffect
{
    public StatusEffectType Type;
    public int RemainingTurns;
    public int ValuePerTurn;

    public StatusEffect(StatusEffectType type, int duration, int value = 0)
    {
        Type = type;
        RemainingTurns = duration;
        ValuePerTurn = value;
    }

    /// <summary>
    /// Returns true if this effect has expired.
    /// </summary>
    public bool IsExpired => RemainingTurns <= 0;

    /// <summary>
    /// Ticks down one turn. Returns true if still active.
    /// </summary>
    public bool Tick()
    {
        RemainingTurns--;
        return RemainingTurns > 0;
    }
}
