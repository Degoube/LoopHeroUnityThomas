using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    public int startingResources = 100;

    [Header("Critical Thresholds")]
    public int criticalThreshold = 20;
    public int warningThreshold = 50;

    public int CurrentResources { get; private set; }

    public event Action<int> OnResourcesChanged;
    public event Action OnResourcesDepleted;
    public event Action OnResourcesCritical;
    public event Action OnResourcesWarning;

    private bool hasCriticalWarningFired;
    private bool hasWarningFired;

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
        CurrentResources = startingResources;
        OnResourcesChanged?.Invoke(CurrentResources);
    }

    public void AddResources(int amount)
    {
        if (amount <= 0)
            return;

        CurrentResources += amount;
        OnResourcesChanged?.Invoke(CurrentResources);

        Debug.Log($"Resources added: +{amount}. Total: {CurrentResources}");
    }

    public void RemoveResources(int amount)
    {
        if (amount <= 0)
            return;

        CurrentResources -= amount;
        CurrentResources = Mathf.Max(0, CurrentResources);

        OnResourcesChanged?.Invoke(CurrentResources);
        CheckResourceThresholds();

        Debug.Log($"Resources removed: -{amount}. Total: {CurrentResources}");
    }

    public bool HasEnoughResources(int amount)
    {
        return CurrentResources >= amount;
    }

    public bool TrySpendResources(int amount)
    {
        if (!HasEnoughResources(amount))
            return false;

        RemoveResources(amount);
        return true;
    }

    private void CheckResourceThresholds()
    {
        if (CurrentResources <= 0)
        {
            OnResourcesDepleted?.Invoke();
            Debug.LogWarning("Resources depleted!");
        }
        else if (CurrentResources <= criticalThreshold && !hasCriticalWarningFired)
        {
            hasCriticalWarningFired = true;
            OnResourcesCritical?.Invoke();
            Debug.LogWarning($"Resources critical: {CurrentResources}");
        }
        else if (CurrentResources <= warningThreshold && !hasWarningFired)
        {
            hasWarningFired = true;
            OnResourcesWarning?.Invoke();
            Debug.LogWarning($"Resources low: {CurrentResources}");
        }

        if (CurrentResources > criticalThreshold)
        {
            hasCriticalWarningFired = false;
        }

        if (CurrentResources > warningThreshold)
        {
            hasWarningFired = false;
        }
    }

    public void ResetResources()
    {
        CurrentResources = startingResources;
        hasCriticalWarningFired = false;
        hasWarningFired = false;
        OnResourcesChanged?.Invoke(CurrentResources);
    }
}
