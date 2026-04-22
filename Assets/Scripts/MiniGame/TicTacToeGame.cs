using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tic-Tac-Toe mini-game (Morpion).
/// Player = X, AI = O. AI plays with win/block/center/random strategy.
/// Implements IMiniGame — MiniGameManager owns the lifecycle.
///
/// TILE: Altar
///
/// VICTORY CONDITIONS:
///   Win  = Player aligns 3 symbols (row, column, or diagonal)  -> +gold, +XP, flag set
///   Lose = AI aligns 3 symbols                                  -> -gold penalty
///   Draw = Board full with no winner                             -> No penalty, small XP
///
/// PAUSE: MiniGameManager pauses PlayerLoopController before this starts.
///        MiniGameManager resumes it after OnMiniGameEnded fires.
/// </summary>
public class TicTacToeGame : MonoBehaviour, IMiniGame
{
    // ── IMiniGame event ───────────────────────────────────────────────────────
    public event Action<MiniGameResult> OnMiniGameEnded;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("UI Root")]
    public GameObject uiRoot;

    [Header("Grid — 9 buttons, index 0-8, left-to-right top-to-bottom")]
    public Button[] cellButtons;   // 9 elements
    public TextMeshProUGUI[] cellTexts; // 9 elements, children of each button

    [Header("Status & Result")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI resultText;
    public GameObject resultPanel;

    [Header("Victory/Defeat Conditions Display")]
    [Tooltip("Optional text showing win/lose conditions at game start.")]
    public TextMeshProUGUI conditionsText;

    [Header("Result Buttons")]
    public Button closeButton;

    [Header("Rewards")]
    [Tooltip("Gold gained on victory.")]
    public int resourceRewardOnWin  =  30;
    [Tooltip("XP gained on victory.")]
    public int xpRewardOnWin = 15;
    [Tooltip("Gold lost on defeat.")]
    public int resourcePenaltyOnLose = 10;
    [Tooltip("XP gained on draw (consolation).")]
    public int xpRewardOnDraw = 5;

    [Header("AI")]
    public float aiPlayDelay = 0.7f;

    // ── Internal state ────────────────────────────────────────────────────────
    // 0 = empty, 1 = player (X), 2 = AI (O)
    private readonly int[] board = new int[9];
    private bool isPlayerTurn;
    private bool gameOver;

    private const string PlayerSymbol = "X";
    private const string AISymbol     = "O";

    private static readonly string VictoryConditionsMessage =
        "VICTOIRE : Aligner 3 symboles X\n" +
        "DEFAITE : L'IA aligne 3 symboles O\n" +
        "EGALITE : Plateau plein sans alignement";

    // Win lines: rows, columns, diagonals
    private static readonly int[][] WinLines =
    {
        new[] {0, 1, 2}, new[] {3, 4, 5}, new[] {6, 7, 8}, // rows
        new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8}, // columns
        new[] {0, 4, 8}, new[] {2, 4, 6}                    // diagonals
    };

    // ── IMiniGame ─────────────────────────────────────────────────────────────

    /// <summary>Called by MiniGameManager to start the game.</summary>
    public void StartMiniGame(BoardTile sourceTile)
    {
        Reset();

        if (uiRoot != null)
            uiRoot.SetActive(true);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // Show victory/defeat conditions
        if (conditionsText != null)
            conditionsText.text = VictoryConditionsMessage;

        WireButtons();
        SetStatus("A toi de jouer !");
        isPlayerTurn = true;
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void WireButtons()
    {
        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (cellButtons[i] == null)
                continue;

            int index = i; // capture for lambda
            cellButtons[i].onClick.RemoveAllListeners();
            cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
            cellButtons[i].interactable = true;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void Reset()
    {
        for (int i = 0; i < 9; i++)
            board[i] = 0;

        gameOver = false;

        if (cellButtons == null || cellTexts == null)
            return;

        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (i < cellTexts.Length && cellTexts[i] != null) cellTexts[i].text = "";
            if (cellButtons[i] != null) cellButtons[i].interactable = true;
        }
    }

    // ── Player input ──────────────────────────────────────────────────────────

    private void OnCellClicked(int index)
    {
        if (!isPlayerTurn || gameOver || board[index] != 0)
            return;

        PlayAt(index, 1, PlayerSymbol);
        isPlayerTurn = false;

        TicTacToeOutcome outcome = CheckOutcome();
        if (outcome != TicTacToeOutcome.None)
        {
            EndGame(outcome);
            return;
        }

        SetStatus("L'IA reflechit...");
        DisableBoard();
        StartCoroutine(AIPlayCoroutine());
    }

    // ── AI ────────────────────────────────────────────────────────────────────

    private IEnumerator AIPlayCoroutine()
    {
        yield return new WaitForSeconds(aiPlayDelay);

        int cell = PickAICell();
        if (cell == -1)
        {
            EndGame(TicTacToeOutcome.Draw);
            yield break;
        }

        PlayAt(cell, 2, AISymbol);
        isPlayerTurn = true;
        EnableBoard();

        TicTacToeOutcome outcome = CheckOutcome();
        if (outcome != TicTacToeOutcome.None)
        {
            EndGame(outcome);
            yield break;
        }

        SetStatus("A toi de jouer !");
    }

    /// <summary>
    /// Simple but competitive AI:
    /// 1. Win — complete own line if possible
    /// 2. Block — prevent player from winning
    /// 3. Center — take center if free
    /// 4. Random — pick any empty cell
    /// </summary>
    private int PickAICell()
    {
        const int ai = 2;
        const int player = 1;

        int winCell = FindCompletingCell(ai);
        if (winCell != -1) return winCell;

        int blockCell = FindCompletingCell(player);
        if (blockCell != -1) return blockCell;

        if (board[4] == 0) return 4;

        return PickRandomEmptyCell();
    }

    private int FindCompletingCell(int side)
    {
        foreach (int[] line in WinLines)
        {
            int sideCount = 0;
            int emptyIndex = -1;

            for (int i = 0; i < 3; i++)
            {
                int cell = board[line[i]];
                if (cell == side) sideCount++;
                else if (cell == 0) emptyIndex = line[i];
            }

            if (sideCount == 2 && emptyIndex != -1)
                return emptyIndex;
        }

        return -1;
    }

    private int PickRandomEmptyCell()
    {
        int empty = 0;
        for (int i = 0; i < 9; i++)
            if (board[i] == 0) empty++;

        if (empty == 0) return -1;

        int target = UnityEngine.Random.Range(0, empty);
        int seen = 0;
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0)
            {
                if (seen == target) return i;
                seen++;
            }
        }

        return -1;
    }

    // ── Board helpers ─────────────────────────────────────────────────────────

    private void PlayAt(int index, int player, string symbol)
    {
        board[index] = player;
        if (cellTexts[index] != null)
            cellTexts[index].text = symbol;
        if (cellButtons[index] != null)
            cellButtons[index].interactable = false;
    }

    private void DisableBoard()
    {
        foreach (Button btn in cellButtons)
            if (btn != null) btn.interactable = false;
    }

    private void EnableBoard()
    {
        for (int i = 0; i < 9; i++)
            if (cellButtons[i] != null && board[i] == 0)
                cellButtons[i].interactable = true;
    }

    // ── Win detection ─────────────────────────────────────────────────────────

    private TicTacToeOutcome CheckOutcome()
    {
        foreach (int[] line in WinLines)
        {
            int a = board[line[0]], b = board[line[1]], c = board[line[2]];
            if (a != 0 && a == b && b == c)
                return a == 1 ? TicTacToeOutcome.PlayerWin : TicTacToeOutcome.AIWin;
        }

        bool boardFull = true;
        foreach (int cell in board)
            if (cell == 0) { boardFull = false; break; }

        return boardFull ? TicTacToeOutcome.Draw : TicTacToeOutcome.None;
    }

    // ── End game ──────────────────────────────────────────────────────────────

    private void EndGame(TicTacToeOutcome outcome)
    {
        gameOver = true;
        DisableBoard();

        string message;
        MiniGameResult result;

        switch (outcome)
        {
            case TicTacToeOutcome.PlayerWin:
                message = $"VICTOIRE !\n+{resourceRewardOnWin} Gold, +{xpRewardOnWin} XP";
                result  = MiniGameResult.Win(resourceRewardOnWin, xpRewardOnWin, NarrativeFlags.AltarMiniGameWon);
                SetStatus("Victoire !");
                break;

            case TicTacToeOutcome.AIWin:
                message = $"DEFAITE...\n-{resourcePenaltyOnLose} Gold";
                result  = MiniGameResult.Lose(-resourcePenaltyOnLose);
                SetStatus("Defaite !");
                break;

            default: // Draw
                message = $"EGALITE !\n+{xpRewardOnDraw} XP";
                result  = MiniGameResult.Win(0, xpRewardOnDraw);
                SetStatus("Egalite !");
                break;
        }

        ShowResult(message);
        StartCoroutine(CloseAfterDelay(1.8f, result));
    }

    private IEnumerator CloseAfterDelay(float delay, MiniGameResult result)
    {
        yield return new WaitForSeconds(delay);
        FireResult(result);
    }

    private void OnCloseClicked()
    {
        if (!gameOver)
            return;

        StopAllCoroutines();
        TicTacToeOutcome outcome = CheckOutcome();
        MiniGameResult result = outcome switch
        {
            TicTacToeOutcome.PlayerWin => MiniGameResult.Win(resourceRewardOnWin, xpRewardOnWin, NarrativeFlags.AltarMiniGameWon),
            TicTacToeOutcome.AIWin     => MiniGameResult.Lose(-resourcePenaltyOnLose),
            _                          => MiniGameResult.Win(0, xpRewardOnDraw)
        };

        FireResult(result);
    }

    private void FireResult(MiniGameResult result)
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);

        OnMiniGameEnded?.Invoke(result);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ShowResult(string message)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = message;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (cellButtons != null)
        {
            foreach (Button btn in cellButtons)
                if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }
}

public enum TicTacToeOutcome
{
    None,
    PlayerWin,
    AIWin,
    Draw
}
