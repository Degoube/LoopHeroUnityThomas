using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example mini-game: click a target button before the timer runs out.
/// Demonstrates how to implement IMiniGame.
/// Copy this class and rename it to create a new mini-game.
/// </summary>
public class ExampleMiniGame : MonoBehaviour, IMiniGame
{
    public event Action<MiniGameResult> OnMiniGameEnded;

    [Header("Settings")]
    public float timeLimit = 5f;
    public int resourceRewardOnWin = 25;
    public int resourcePenaltyOnLose = 10;

    [Header("UI — assign in Prefab")]
    public GameObject uiRoot;
    public Button targetButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;

    private bool isRunning;
    private float elapsed;
    private BoardTile sourceTile;

    // ── IMiniGame ─────────────────────────────────────────────────────────────

    public void StartMiniGame(BoardTile tile)
    {
        sourceTile = tile;
        isRunning = true;
        elapsed = 0f;

        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (instructionText != null)
            instructionText.text = "Appuie sur le bouton !";

        if (targetButton != null)
            targetButton.onClick.AddListener(OnTargetClicked);

        Debug.Log("[ExampleMiniGame] Started.");
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Update()
    {
        if (!isRunning)
            return;

        elapsed += Time.unscaledDeltaTime;
        float remaining = Mathf.Max(0f, timeLimit - elapsed);

        if (timerText != null)
            timerText.text = $"{remaining:F1}s";

        if (elapsed >= timeLimit)
            EndMiniGame(false);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnTargetClicked()
    {
        EndMiniGame(true);
    }

    private void EndMiniGame(bool success)
    {
        if (!isRunning)
            return;

        isRunning = false;

        if (targetButton != null)
            targetButton.onClick.RemoveListener(OnTargetClicked);

        if (uiRoot != null)
            uiRoot.SetActive(false);

        MiniGameResult result = success
            ? MiniGameResult.Win(resourceRewardOnWin)
            : MiniGameResult.Lose(-resourcePenaltyOnLose);

        Debug.Log($"[ExampleMiniGame] Ended — {(success ? "WIN" : "LOSE")}");

        // Fire the event — MiniGameManager will clean up and resume the loop
        OnMiniGameEnded?.Invoke(result);
    }
}
