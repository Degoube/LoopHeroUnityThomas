using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller for the Hide & Seek mini-game.
/// ZQSD movement (top-down 3D) and E interaction with HideSpots.
/// Detection uses OverlapSphere — HideSpot colliders stay non-trigger.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class HideAndSeekPlayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Visual mesh/model child. Hidden when inside a box.")]
    [SerializeField] private GameObject playerVisual;

    [Header("Detection")]
    [Tooltip("Radius to search for nearby HideSpots.")]
    [SerializeField] private float hideSpotSearchRadius = 2.5f;

    /// <summary>True when the player is hidden inside a box.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>Fired when the player is caught by an AI.</summary>
    public event Action OnPlayerCaught;

    private CharacterController characterController;
    private CharacterAnimationHandler animHandler;
    private float moveSpeed;
    private bool inputEnabled;

    private HideSpot currentHideSpot;
    private HideSpot nearestAvailableSpot;

    private readonly Collider[] overlapBuffer = new Collider[16];
    private const float Gravity = -9.81f;

    /// <summary>Initializes the player with config values.</summary>
    public void Initialize(float speed, float transitionDuration)
    {
        moveSpeed = speed;
        characterController = GetComponent<CharacterController>();
        animHandler = GetComponent<CharacterAnimationHandler>();
        inputEnabled = true;
        IsHidden = false;

        if (playerVisual != null)
            playerVisual.SetActive(true);

        Debug.Log($"[HideAndSeekPlayer] Initialized — Speed: {speed}");
    }

    /// <summary>Enables or disables player input processing.</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void Update()
    {
        if (!inputEnabled)
            return;

        if (IsHidden)
        {
            HandleHiddenState();
            return;
        }

        HandleMovement();
        HandleHideSpotDetection();
        HandleInteraction();
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        if (Keyboard.current == null || characterController == null)
            return;

        Vector3 direction = Vector3.zero;

        // ZQSD (AZERTY) + WASD (QWERTY) support
        if (Keyboard.current.wKey.isPressed || Keyboard.current.zKey.isPressed)
            direction.z += 1f;
        if (Keyboard.current.sKey.isPressed)
            direction.z -= 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.qKey.isPressed)
            direction.x -= 1f;
        if (Keyboard.current.dKey.isPressed)
            direction.x += 1f;

        Vector3 move = Vector3.zero;

        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
            move = direction * (moveSpeed * Time.deltaTime);
            transform.forward = direction;
        }

        // Drive animation based on movement
        if (animHandler != null)
            animHandler.SetSpeed(direction.sqrMagnitude > 0.01f ? 1f : 0f);

        // Apply gravity to keep grounded
        if (!characterController.isGrounded)
            move.y += Gravity * Time.deltaTime;

        if (move.sqrMagnitude > 0f)
            characterController.Move(move);
    }

    // ── HideSpot detection via OverlapSphere ──────────────────────────────────

    private void HandleHideSpotDetection()
    {
        HideSpot previousNearest = nearestAvailableSpot;
        nearestAvailableSpot = FindNearestHideSpot();

        // Show/hide prompts
        if (previousNearest != null && previousNearest != nearestAvailableSpot)
            previousNearest.ShowPrompt(false);

        if (nearestAvailableSpot != null)
            nearestAvailableSpot.ShowPrompt(true);
    }

    private void HandleInteraction()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame && nearestAvailableSpot != null && nearestAvailableSpot.CanHide())
            EnterHideSpot(nearestAvailableSpot);
    }

    private void HandleHiddenState()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            ExitHideSpot();
    }

    // ── Hide / Unhide ─────────────────────────────────────────────────────────

    private void EnterHideSpot(HideSpot spot)
    {
        IsHidden = true;
        currentHideSpot = spot;
        spot.Enter();

        if (characterController != null)
            characterController.enabled = false;

        transform.position = spot.HidePosition;

        if (playerVisual != null)
            playerVisual.SetActive(false);

        Debug.Log("[HideAndSeekPlayer] Hidden.");
    }

    private void ExitHideSpot()
    {
        if (currentHideSpot == null)
            return;

        IsHidden = false;
        currentHideSpot.Exit();

        Vector3 exitPosition = currentHideSpot.transform.position + currentHideSpot.transform.forward * 1.5f;
        transform.position = exitPosition;

        if (characterController != null)
            characterController.enabled = true;

        if (playerVisual != null)
            playerVisual.SetActive(true);

        currentHideSpot = null;

        Debug.Log("[HideAndSeekPlayer] Exited hiding spot.");
    }

    private HideSpot FindNearestHideSpot()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, hideSpotSearchRadius, overlapBuffer,
            Physics.AllLayers, QueryTriggerInteraction.Collide);

        HideSpot best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            HideSpot spot = overlapBuffer[i].GetComponent<HideSpot>();
            if (spot == null || !spot.CanHide())
                continue;

            float dist = Vector3.Distance(transform.position, spot.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = spot;
            }
        }

        return best;
    }

    /// <summary>Called by the AI when it catches the player.</summary>
    public void Caught()
    {
        inputEnabled = false;

        if (IsHidden && currentHideSpot != null)
            ExitHideSpot();

        OnPlayerCaught?.Invoke();
        Debug.Log("[HideAndSeekPlayer] Caught!");
    }
}
