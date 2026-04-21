using System;
using UnityEngine;

/// <summary>
/// Singleton that tracks the player's experience points.
/// Integrated with the save system via GameSaveController.
/// </summary>
public class XPManager : MonoBehaviour
{
    public static XPManager Instance { get; private set; }

    [Header("Starting XP")]
    [SerializeField] private int startingXP = 0;

    /// <summary>Current total XP.</summary>
    public int CurrentXP { get; private set; }

    /// <summary>Fired every time XP changes. Payload = new total.</summary>
    public event Action<int> OnXPChanged;

    /// <summary>Fired when XP is gained. Payload = amount gained.</summary>
    public event Action<int> OnXPGained;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CurrentXP = startingXP;
        OnXPChanged?.Invoke(CurrentXP);
    }

    /// <summary>Adds experience points and fires events.</summary>
    public void AddXP(int amount)
    {
        if (amount <= 0)
            return;

        CurrentXP += amount;
        OnXPGained?.Invoke(amount);
        OnXPChanged?.Invoke(CurrentXP);

        Debug.Log($"[XP] +{amount} XP. Total: {CurrentXP}");
    }

    /// <summary>
    /// Sets XP to an exact value. Used exclusively by the save system on restore.
    /// </summary>
    public void SetXP(int amount)
    {
        CurrentXP = Mathf.Max(0, amount);
        OnXPChanged?.Invoke(CurrentXP);
    }

    /// <summary>Resets XP to starting value.</summary>
    public void ResetXP()
    {
        CurrentXP = startingXP;
        OnXPChanged?.Invoke(CurrentXP);
    }
}
