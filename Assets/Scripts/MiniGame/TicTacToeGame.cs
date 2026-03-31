using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tic-Tac-Toe mini-game.
/// Player = X, AI = O. AI plays randomly after a short delay.
/// Implements IMiniGame — MiniGameManager owns the lifecycle.
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

    [Header("Result Buttons")]
    public Button closeButton;

    [Header("Rewards")]
    public int resourceRewardOnWin  =  30;
    public int resourcePenaltyOnLose = 10;

    [Header("AI")]
    public float aiPlayDelay = 0.7f;

    // ── Internal state ────────────────────────────────────────────────────────
    // 0 = empty, 1 = player (X), 2 = AI (O)
    private readonly int[] board = new int[9];
    private bool isPlayerTurn;
    private bool gameOver;

    private const string PlayerSymbol = "X";
    private const string AISymbol     = "O";

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

        WireButtons();
        SetStatus("À toi de jouer !");
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
            closeButton.onClick.AddListener(OnCloseClicked);
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

        SetStatus("L'IA réfléchit...");
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
            // No cell available → draw (should be caught earlier, safety net)
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

        SetStatus("À toi de jouer !");
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

        // 1. Can AI win in one move?
        int winCell = FindCompletingCell(ai);
        if (winCell != -1)
            return winCell;

        // 2. Must AI block the player?
        int blockCell = FindCompletingCell(player);
        if (blockCell != -1)
            return blockCell;

        // 3. Take center if free
        if (board[4] == 0)
            return 4;

        // 4. Random empty cell
        return PickRandomEmptyCell();
    }

    /// <summary>
    /// Returns the index of an empty cell that would complete a winning line
    /// for the given side (1=player, 2=AI), or -1 if none.
    /// </summary>
    private int FindCompletingCell(int side)
    {
        foreach (int[] line in WinLines)
        {
            int sideCount = 0;
            int emptyIndex = -1;

            for (int i = 0; i < 3; i++)
            {
                int cell = board[line[i]];
                if (cell == side)
                    sideCount++;
                else if (cell == 0)
                    emptyIndex = line[i];
            }

            // Two of three cells are this side and the third is empty → complete/block
            if (sideCount == 2 && emptyIndex != -1)
                return emptyIndex;
        }

        return -1;
    }

    /// <summary>Picks a random empty cell. Returns -1 if the board is full.</summary>
    private int PickRandomEmptyCell()
    {
        int empty = 0;
        for (int i = 0; i < 9; i++)
            if (board[i] == 0) empty++;

        if (empty == 0)
            return -1;

        int target = UnityEngine.Random.Range(0, empty);
        int seen = 0;
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == 0)
            {
                if (seen == target)
                    return i;
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

        // Draw: no winner and no empty cell
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
                message = "Tu as gagné ! +ressources";
                result  = MiniGameResult.Win(resourceRewardOnWin);
                SetStatus("Victoire !");
                break;

            case TicTacToeOutcome.AIWin:
                message = "L'IA a gagné...";
                result  = MiniGameResult.Lose(-resourcePenaltyOnLose);
                SetStatus("Défaite !");
                break;

            default: // Draw
                message = "Égalité !";
                result  = MiniGameResult.Win(0); // neutral — no penalty on draw
                SetStatus("Égalité !");
                break;
        }

        ShowResult(message);

        // Short delay so the player sees the result before the screen closes
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
        MiniGameResult result = outcome == TicTacToeOutcome.PlayerWin
            ? MiniGameResult.Win(resourceRewardOnWin)
            : outcome == TicTacToeOutcome.AIWin
                ? MiniGameResult.Lose(-resourcePenaltyOnLose)
                : MiniGameResult.Win(0);

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
}

public enum TicTacToeOutcome
{
    None,
    PlayerWin,
    AIWin,
    Draw
}
