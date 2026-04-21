using System;
using UnityEngine;

/// <summary>
/// ScriptableObject holding all tunable parameters for the Hide & Seek mini-game.
/// Create via: Create > MiniGame > Hide And Seek Config
/// </summary>
[CreateAssetMenu(fileName = "HideAndSeekConfig", menuName = "MiniGame/Hide And Seek Config")]
public class HideAndSeekConfig : ScriptableObject
{
    // ── Timer ─────────────────────────────────────────────────────────────────
    [Header("Timer")]
    [Tooltip("Duration of the mini-game in seconds.")]
    [Range(10f, 120f)]
    public float gameDuration = 30f;

    // ── Player ────────────────────────────────────────────────────────────────
    [Header("Player")]
    [Tooltip("Player movement speed (units/s).")]
    [Range(1f, 20f)]
    public float playerSpeed = 5f;

    [Tooltip("How long it takes the player to enter or exit a hiding spot (seconds).")]
    [Range(0f, 1f)]
    public float hideTransitionDuration = 0.3f;

    // ── AI Enemies ────────────────────────────────────────────────────────────
    [Header("AI Enemies")]
    [Tooltip("Array of AI profiles. Each spawned enemy picks one profile.")]
    public AIProfile[] aiProfiles;

    // ── Rewards ───────────────────────────────────────────────────────────────
    [Header("Rewards — Victory (survived the timer)")]
    [Tooltip("Gold gained on victory.")]
    public int goldRewardOnWin = 40;

    [Tooltip("XP gained on victory.")]
    public int xpRewardOnWin = 25;

    [Header("Rewards — Defeat (caught by AI)")]
    [Tooltip("Gold lost on defeat (positive value = loss).")]
    public int goldPenaltyOnLose = 15;

    [Tooltip("XP gained on defeat (always positive — the player learns from failure).")]
    public int xpRewardOnLose = 10;

    // ── HUD ───────────────────────────────────────────────────────────────────
    [Header("HUD")]
    [Tooltip("How long the controls hint stays visible at the start (seconds).")]
    [Range(1f, 10f)]
    public float controlsHintDuration = 4f;

    [Tooltip("How long the result screen stays visible before auto-closing (seconds).")]
    [Range(1f, 5f)]
    public float resultDisplayDuration = 2.5f;
}

/// <summary>
/// Defines one AI enemy behavior profile.
/// Assign multiple profiles to create variety.
/// </summary>
[Serializable]
public class AIProfile
{
    [Tooltip("Display name for debug purposes.")]
    public string profileName = "Patrol Guard";

    [Header("Patrol")]
    [Tooltip("How the AI moves between patrol points.")]
    public PatrolPattern patrolPattern = PatrolPattern.Loop;

    [Tooltip("Movement speed while patrolling (units/s).")]
    [Range(1f, 10f)]
    public float patrolSpeed = 3f;

    [Tooltip("Time the AI waits at each patrol point (seconds).")]
    [Range(0f, 5f)]
    public float waitTimeAtPoint = 1f;

    [Header("Vision")]
    [Tooltip("Maximum detection distance (units).")]
    [Range(1f, 30f)]
    public float visionRange = 8f;

    [Tooltip("Half-angle of the vision cone (degrees). 45 = 90 degree total cone.")]
    [Range(5f, 90f)]
    public float visionHalfAngle = 40f;

    [Header("Pursuit")]
    [Tooltip("Movement speed while pursuing the player (units/s).")]
    [Range(2f, 15f)]
    public float pursuitSpeed = 6f;

    [Tooltip("How long the AI keeps pursuing after losing sight (seconds).")]
    [Range(0f, 10f)]
    public float pursuitMemoryDuration = 2f;

    [Header("Attack")]
    [Tooltip("Distance at which the AI catches the player (units).")]
    [Range(0.3f, 3f)]
    public float attackRange = 1f;
}

/// <summary>
/// Determines how the AI traverses its patrol waypoints.
/// </summary>
public enum PatrolPattern
{
    /// <summary>Visits waypoints in order, then loops back to the first.</summary>
    Loop,
    /// <summary>Visits waypoints in order, then reverses (ping-pong).</summary>
    PingPong,
    /// <summary>Picks a random next waypoint each time.</summary>
    Random
}
