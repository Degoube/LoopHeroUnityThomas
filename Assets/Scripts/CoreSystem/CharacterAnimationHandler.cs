using UnityEngine;

/// <summary>
/// Standardized animation handler for all characters (player, NPCs, enemies).
/// Wraps Animator with consistent parameter names and helper methods.
/// Designed for easy duplication: new character = mesh + anims + override controller + this component.
/// 
/// Expected Animator Controller states:
///   Idle, Walk, Run, Attack, Hit, Cast, Death, Victory, Interact
/// 
/// Expected parameters:
///   Speed (float), IsMoving (bool), Attack (trigger), Hit (trigger),
///   Dead (bool), Victory (trigger), Interact (trigger), Cast (trigger)
///   
/// Blend Tree: use "Speed" parameter for Idle -> Walk -> Run transitions.
/// </summary>
public class CharacterAnimationHandler : MonoBehaviour
{
    // ── Standard Parameter Names ──────────────────────────────────────────────
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int VictoryHash = Animator.StringToHash("Victory");
    private static readonly int InteractHash = Animator.StringToHash("Interact");
    private static readonly int CastHash = Animator.StringToHash("Cast");

    [Header("References")]
    [Tooltip("Animator component. Auto-detected if left empty.")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [Tooltip("Speed smoothing factor for blend tree transitions.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float speedSmoothFactor = 0.1f;

    [Tooltip("Threshold below which speed is considered zero (for IsMoving).")]
    [Range(0f, 0.1f)]
    [SerializeField] private float movingThreshold = 0.01f;

    [Tooltip("If true, uses root motion from animation clips.")]
    [SerializeField] private bool useRootMotion = false;

    private float currentSpeed;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning($"[CharacterAnimationHandler] No Animator found on {gameObject.name}.");

        if (animator != null)
            animator.applyRootMotion = useRootMotion;
    }

    /// <summary>
    /// Sets the movement speed for the blend tree (Idle -> Walk -> Run).
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (animator == null) return;

        currentSpeed = Mathf.Lerp(currentSpeed, speed, speedSmoothFactor);
        animator.SetFloat(SpeedHash, currentSpeed);
        animator.SetBool(IsMovingHash, currentSpeed > movingThreshold);
    }

    /// <summary>
    /// Triggers the attack animation.
    /// </summary>
    public void PlayAttack()
    {
        if (animator == null) return;
        animator.SetTrigger(AttackHash);
    }

    /// <summary>
    /// Triggers the hit/damage received animation.
    /// </summary>
    public void PlayHit()
    {
        if (animator == null) return;
        animator.SetTrigger(HitHash);
    }

    /// <summary>
    /// Sets the death state. Transitions to Death animation.
    /// </summary>
    public void SetDead(bool isDead)
    {
        if (animator == null) return;
        animator.SetBool(DeadHash, isDead);
    }

    /// <summary>
    /// Triggers the victory animation.
    /// </summary>
    public void PlayVictory()
    {
        if (animator == null) return;
        animator.SetTrigger(VictoryHash);
    }

    /// <summary>
    /// Triggers the interact animation (opening chest, picking up item, etc.).
    /// </summary>
    public void PlayInteract()
    {
        if (animator == null) return;
        animator.SetTrigger(InteractHash);
    }

    /// <summary>
    /// Triggers the cast/skill animation.
    /// </summary>
    public void PlayCast()
    {
        if (animator == null) return;
        animator.SetTrigger(CastHash);
    }

    /// <summary>
    /// Immediately stops all movement animation and returns to Idle.
    /// </summary>
    public void StopMovement()
    {
        SetSpeed(0f);
        currentSpeed = 0f;
    }

    /// <summary>
    /// Resets all triggers. Useful when re-initializing the character.
    /// </summary>
    public void ResetAllTriggers()
    {
        if (animator == null) return;

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(HitHash);
        animator.ResetTrigger(VictoryHash);
        animator.ResetTrigger(InteractHash);
        animator.ResetTrigger(CastHash);
    }

    /// <summary>
    /// Applies an AnimatorOverrideController to swap animation clips at runtime.
    /// Used for character variants sharing the same state machine.
    /// </summary>
    public void ApplyOverrideController(AnimatorOverrideController overrideController)
    {
        if (animator == null || overrideController == null) return;
        animator.runtimeAnimatorController = overrideController;
    }

    /// <summary>
    /// Returns the Animator for advanced usage (animation events, etc.).
    /// </summary>
    public Animator GetAnimator() => animator;
}
