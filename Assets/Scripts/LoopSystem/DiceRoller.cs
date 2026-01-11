using System;
using System.Collections;
using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    [Header("Dice Settings")]
    public int minRoll = 1;
    public int maxRoll = 6;

    [Header("Animation")]
    public float rollDuration = 1f;
    public int rollSteps = 10;
    public AudioClip rollSound;
    public AudioClip resultSound;

    public event Action<int> OnRollComplete;
    public event Action OnRollStarted;

    private AudioSource audioSource;
    private bool isRolling;
    private int currentResult;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void RollDice()
    {
        if (isRolling)
            return;

        StartCoroutine(RollAnimation());
    }

    private IEnumerator RollAnimation()
    {
        isRolling = true;
        OnRollStarted?.Invoke();

        if (audioSource != null && rollSound != null)
        {
            audioSource.PlayOneShot(rollSound);
        }

        float stepDuration = rollDuration / rollSteps;

        for (int i = 0; i < rollSteps; i++)
        {
            currentResult = UnityEngine.Random.Range(minRoll, maxRoll + 1);
            yield return new WaitForSeconds(stepDuration);
        }

        currentResult = UnityEngine.Random.Range(minRoll, maxRoll + 1);

        if (audioSource != null && resultSound != null)
        {
            audioSource.PlayOneShot(resultSound);
        }

        isRolling = false;
        OnRollComplete?.Invoke(currentResult);

        Debug.Log($"Dice rolled: {currentResult}");
    }

    public int GetLastRoll()
    {
        return currentResult;
    }

    public bool IsRolling()
    {
        return isRolling;
    }
}
