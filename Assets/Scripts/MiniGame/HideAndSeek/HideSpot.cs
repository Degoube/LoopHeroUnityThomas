using UnityEngine;

/// <summary>
/// A hiding spot (box/crate) the player can enter with E.
/// Detection is done by HideAndSeekPlayer via OverlapSphere — no trigger needed.
/// The collider on this object should be non-trigger so it blocks movement.
/// </summary>
public class HideSpot : MonoBehaviour
{
    [Tooltip("Visual indicator shown when the player is close enough to interact.")]
    [SerializeField] private GameObject interactPrompt;

    /// <summary>True if a player is currently hidden inside this spot.</summary>
    public bool IsOccupied { get; private set; }

    /// <summary>World position the player teleports to when hiding.</summary>
    public Vector3 HidePosition => transform.position;

    private void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    /// <summary>Returns true if the spot is available.</summary>
    public bool CanHide()
    {
        return !IsOccupied;
    }

    /// <summary>Shows the interaction prompt (called by player when in range).</summary>
    public void ShowPrompt(bool show)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(show);
    }

    /// <summary>Marks this spot as occupied.</summary>
    public void Enter()
    {
        IsOccupied = true;
        ShowPrompt(false);
    }

    /// <summary>Frees this spot.</summary>
    public void Exit()
    {
        IsOccupied = false;
    }
}
