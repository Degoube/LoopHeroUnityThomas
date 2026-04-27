using System;
using UnityEngine;

/// <summary>
/// AI enemy for the Hide & Seek mini-game.
/// Patrols waypoints, detects the player via a vision cone, pursues and attacks.
/// Uses CharacterController for collision-aware movement (cannot walk through walls).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class HideAndSeekAI : MonoBehaviour
{
    [Header("Patrol Waypoints")]
    [Tooltip("Assign the patrol waypoints. The AI visits them according to its profile pattern.")]
    public Transform[] waypoints;

    [Header("Debug Visualization")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color pursuitColor = Color.red;
    [SerializeField] private Color visionConeColor = new Color(1f, 1f, 0f, 0.3f);

    /// <summary>Fired when this AI catches the player.</summary>
    public event Action OnPlayerCaught;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private AIProfile profile;
    private HideAndSeekPlayer targetPlayer;
    private CharacterController controller;
    private CharacterAnimationHandler animHandler;
    private AIState currentState;
    private bool isActive;

    // Patrol
    private int currentWaypointIndex;
    private int waypointDirection = 1;
    private float waitTimer;

    // Pursuit
    private Vector3 lastKnownPlayerPosition;
    private float pursuitLostTimer;

    private const float ArrivalThreshold = 0.5f;
    private const float Gravity = -9.81f;

    private enum AIState { Patrolling, Waiting, Pursuing, Attacking }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Configures this AI with a profile and target player reference.</summary>
    public void Initialize(AIProfile aiProfile, HideAndSeekPlayer player)
    {
        profile = aiProfile;
        targetPlayer = player;
        controller = GetComponent<CharacterController>();
        animHandler = GetComponent<CharacterAnimationHandler>();
        currentState = AIState.Patrolling;
        currentWaypointIndex = 0;
        waypointDirection = 1;
        isActive = true;

        Debug.Log($"[AI] '{profile.profileName}' initialized — Pattern: {profile.patrolPattern}, Speed: {profile.patrolSpeed}/{profile.pursuitSpeed}");
    }

    /// <summary>Stops all AI behavior.</summary>
    public void StopAI()
    {
        isActive = false;
        if (animHandler != null)
            animHandler.StopMovement();
    }

    /// <summary>Resumes AI behavior after a pause (e.g., countdown).</summary>
    public void ResumeAI()
    {
        if (profile != null && targetPlayer != null)
            isActive = true;
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isActive || profile == null || targetPlayer == null || controller == null)
            return;

        switch (currentState)
        {
            case AIState.Patrolling:
                UpdatePatrol();
                break;
            case AIState.Waiting:
                UpdateWaiting();
                break;
            case AIState.Pursuing:
                UpdatePursuit();
                break;
            case AIState.Attacking:
                break;
        }

        // Always check vision (except mid-attack)
        if (currentState != AIState.Attacking)
            CheckVision();
    }

    // ── Movement via CharacterController ──────────────────────────────────────

    private void MoveTo(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            if (animHandler != null)
                animHandler.SetSpeed(0f);
            return;
        }

        Vector3 move = direction.normalized * (speed * Time.deltaTime);

        // Apply gravity
        if (!controller.isGrounded)
            move.y += Gravity * Time.deltaTime;

        controller.Move(move);

        // Face movement direction
        Vector3 faceDir = direction.normalized;
        if (faceDir.sqrMagnitude > 0.01f)
            transform.forward = faceDir;

        // Drive animation — normalize speed between patrol (0.5) and pursuit (1.0)
        if (animHandler != null)
        {
            float normalizedSpeed = Mathf.InverseLerp(0f, profile.pursuitSpeed, speed);
            animHandler.SetSpeed(normalizedSpeed);
        }
    }

    // ── Patrol ────────────────────────────────────────────────────────────────

    private void UpdatePatrol()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform wp = waypoints[currentWaypointIndex];
        if (wp == null)
            return;

        Vector3 destination = wp.position;
        MoveTo(destination, profile.patrolSpeed);

        float dist = FlatDistance(transform.position, destination);
        if (dist < ArrivalThreshold)
        {
            waitTimer = profile.waitTimeAtPoint;
            currentState = AIState.Waiting;
            if (animHandler != null)
                animHandler.StopMovement();
        }
    }

    private void UpdateWaiting()
    {
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f)
            return;

        AdvanceWaypoint();
        currentState = AIState.Patrolling;
    }

    private void AdvanceWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        switch (profile.patrolPattern)
        {
            case PatrolPattern.Loop:
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                break;

            case PatrolPattern.PingPong:
                currentWaypointIndex += waypointDirection;
                if (currentWaypointIndex >= waypoints.Length - 1)
                    waypointDirection = -1;
                else if (currentWaypointIndex <= 0)
                    waypointDirection = 1;
                currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
                break;

            case PatrolPattern.Random:
                int next;
                do
                {
                    next = UnityEngine.Random.Range(0, waypoints.Length);
                } while (next == currentWaypointIndex && waypoints.Length > 1);
                currentWaypointIndex = next;
                break;
        }
    }

    // ── Vision ────────────────────────────────────────────────────────────────

    private void CheckVision()
    {
        // If player is hidden, tick pursuit memory
        if (targetPlayer == null || targetPlayer.IsHidden)
        {
            if (currentState == AIState.Pursuing)
                TickPursuitMemory();

            return;
        }

        Vector3 toPlayer = targetPlayer.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // Range check
        if (distance > profile.visionRange)
        {
            if (currentState == AIState.Pursuing)
                TickPursuitMemory();
            return;
        }

        // Cone angle check
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > profile.visionHalfAngle)
        {
            if (currentState == AIState.Pursuing)
                TickPursuitMemory();
            return;
        }

        // Line-of-sight raycast (ignore triggers so crates don't block vision)
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (targetPlayer.transform.position + Vector3.up * 0.5f) - origin;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                if (currentState == AIState.Pursuing)
                    TickPursuitMemory();
                return;
            }
        }

        // Player spotted
        lastKnownPlayerPosition = targetPlayer.transform.position;
        pursuitLostTimer = profile.pursuitMemoryDuration;

        if (currentState != AIState.Pursuing)
        {
            currentState = AIState.Pursuing;
            Debug.Log($"[AI] '{profile.profileName}' spotted the player!");
        }
    }

    private void TickPursuitMemory()
    {
        pursuitLostTimer -= Time.deltaTime;
        if (pursuitLostTimer <= 0f)
        {
            currentState = AIState.Patrolling;
            Debug.Log($"[AI] '{profile.profileName}' lost the player, returning to patrol.");
        }
    }

    // ── Pursuit ───────────────────────────────────────────────────────────────

    private void UpdatePursuit()
    {
        if (targetPlayer == null)
            return;

        Vector3 targetPos = targetPlayer.IsHidden ? lastKnownPlayerPosition : targetPlayer.transform.position;
        MoveTo(targetPos, profile.pursuitSpeed);

        // Attack range check
        if (!targetPlayer.IsHidden)
        {
            float dist = FlatDistance(transform.position, targetPlayer.transform.position);
            if (dist <= profile.attackRange)
            {
                currentState = AIState.Attacking;
                isActive = false;

                if (animHandler != null)
                {
                    animHandler.StopMovement();
                    animHandler.PlayAttack();
                }

                targetPlayer.Caught();
                OnPlayerCaught?.Invoke();
                Debug.Log($"[AI] '{profile.profileName}' caught the player!");
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!drawGizmos || profile == null)
            return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 forward = transform.forward;

        // Vision cone
        Gizmos.color = visionConeColor;
        Vector3 leftDir = Quaternion.Euler(0f, -profile.visionHalfAngle, 0f) * forward;
        Vector3 rightDir = Quaternion.Euler(0f, profile.visionHalfAngle, 0f) * forward;
        Gizmos.DrawRay(origin, leftDir * profile.visionRange);
        Gizmos.DrawRay(origin, rightDir * profile.visionRange);
        Gizmos.DrawRay(origin, forward * profile.visionRange);

        // State indicator
        Gizmos.color = currentState == AIState.Pursuing ? pursuitColor : patrolColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, profile.attackRange);

        // Patrol path
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = patrolColor;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            if (profile.patrolPattern == PatrolPattern.Loop && waypoints[0] != null && waypoints[^1] != null)
                Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
        }
    }
}
