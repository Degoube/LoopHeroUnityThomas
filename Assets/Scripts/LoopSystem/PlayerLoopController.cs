using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLoopController : MonoBehaviour
{
    public static PlayerLoopController Instance { get; private set; }

    [Header("Player Settings")]
    public GameObject playerObject;
    public int startPathIndex = 0;

    [Header("Components")]
    public DiceRoller diceRoller;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float movePauseDuration = 0.2f;
    public bool autoMoveOnRoll = true;

    [Header("Loop Settings")]
    public bool restartLoopAutomatically = true;
    public int resourceCostPerRoll = 5;

    [Header("Debug")]
    public bool showDebugInfo = true;

    public LoopState CurrentState { get; private set; }
    public int CurrentTurn { get; private set; }
    public int TotalLoops { get; private set; }
    public BoardTile CurrentTile { get; private set; }
    public int CurrentPathIndex { get; private set; }

    public event Action<LoopState> OnStateChanged;
    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnEnded;
    public event Action<int> OnLoopCompleted;
    public event Action OnMovementStarted;
    public event Action OnMovementCompleted;

    private int remainingMoves;
    private bool isProcessing;
    private List<BoardTile> currentPath = new List<BoardTile>();

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
        if (diceRoller == null)
        {
            diceRoller = GetComponent<DiceRoller>();
        }

        if (diceRoller != null)
        {
            diceRoller.OnRollComplete += OnDiceRolled;
        }

        StartCoroutine(InitializePlayerAfterBoard());
    }

    private IEnumerator InitializePlayerAfterBoard()
    {
        while (BoardManager.Instance == null)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        InitializePlayer();
        ChangeState(LoopState.WaitingToRoll);
    }

    private void InitializePlayer()
    {
        if (BoardManager.Instance == null)
        {
            Debug.LogError("BoardManager not found. Cannot initialize player.");
            return;
        }

        BoardTile startTile = BoardManager.Instance.GetTileByPathIndex(startPathIndex);
        if (startTile == null)
        {
            Debug.LogError($"Start tile not found at path index {startPathIndex}. Make sure the board is generated.");
            return;
        }

        CurrentTile = startTile;
        CurrentPathIndex = startPathIndex;
        
        if (playerObject != null)
        {
            Vector3 targetPos = startTile.transform.position;
            targetPos.y = playerObject.transform.position.y;
            playerObject.transform.position = targetPos;
            
            Debug.Log($"Player initialized at path index {startPathIndex}, world pos {targetPos}");
        }
        
        startTile.EnterTile(playerObject);

        CurrentTurn = 0;
        TotalLoops = 0;
    }

    public void StartTurn()
    {
        if (isProcessing || CurrentState != LoopState.WaitingToRoll)
            return;

        if (ResourceManager.Instance != null && resourceCostPerRoll > 0)
        {
            if (!ResourceManager.Instance.TrySpendResources(resourceCostPerRoll))
            {
                Debug.LogWarning($"Not enough resources to roll the dice! Need {resourceCostPerRoll}, have {ResourceManager.Instance.CurrentResources}");
                return;
            }
        }

        CurrentTurn++;
        OnTurnStarted?.Invoke(CurrentTurn);

        if (showDebugInfo)
        {
            Debug.Log($"Turn {CurrentTurn} started - Roll the dice!");
        }

        ChangeState(LoopState.Rolling);
        diceRoller.RollDice();
    }

    private void OnDiceRolled(int result)
    {
        remainingMoves = result;

        if (showDebugInfo)
        {
            Debug.Log($"Rolled {result}. Can move {remainingMoves} tiles.");
        }

        if (autoMoveOnRoll)
        {
            StartMovement();
        }
        else
        {
            ChangeState(LoopState.SelectingPath);
        }
    }

    public void StartMovement()
    {
        if (remainingMoves <= 0)
        {
            EndTurn();
            return;
        }

        ChangeState(LoopState.Moving);
        OnMovementStarted?.Invoke();
        StartCoroutine(MovePlayerCoroutine());
    }

    private IEnumerator MovePlayerCoroutine()
    {
        isProcessing = true;
        int stepsToMove = remainingMoves;
        
        Debug.Log($"<color=cyan>Starting movement: Current index={CurrentPathIndex}, Steps to move={stepsToMove}</color>");
        
        for (int i = 0; i < stepsToMove; i++)
        {
            int nextPathIndex = BoardManager.Instance.GetNextPathIndex(CurrentPathIndex, 1);
            BoardTile nextTile = BoardManager.Instance.GetTileByPathIndex(nextPathIndex);

            Debug.Log($"<color=cyan>Step {i+1}/{stepsToMove}: Current={CurrentPathIndex}, Next={nextPathIndex}, Tile={(nextTile != null ? "Found" : "NULL")}, CanEnter={(nextTile != null ? nextTile.CanBeEntered().ToString() : "N/A")}</color>");

            if (nextTile == null || !nextTile.CanBeEntered())
            {
                Debug.LogWarning($"Cannot move to path index {nextPathIndex}. Ending movement. Reason: Tile={(nextTile == null ? "NULL" : "Not enterable")}");
                break;
            }

            yield return MoveToTile(nextTile, nextPathIndex);
            remainingMoves--;

            if (nextPathIndex == 0 && i > 0)
            {
                TotalLoops++;
                OnLoopCompleted?.Invoke(TotalLoops);
                Debug.Log($"Loop completed! Total loops: {TotalLoops}");
            }

            yield return new WaitForSeconds(movePauseDuration);
        }

        isProcessing = false;
        OnMovementCompleted?.Invoke();

        if (CurrentTile != null)
        {
            ActivateCurrentTile();
        }
        else
        {
            EndTurn();
        }
    }

    private IEnumerator MoveToTile(BoardTile targetTile, int newPathIndex)
    {
        if (CurrentTile != null)
        {
            CurrentTile.ExitTile(playerObject);
        }

        Vector3 startPos = playerObject.transform.position;
        Vector3 targetPos = targetTile.transform.position;
        targetPos.y = startPos.y;
        
        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        playerObject.transform.position = targetPos;
        CurrentTile = targetTile;
        CurrentPathIndex = newPathIndex;
        targetTile.EnterTile(playerObject);

        if (showDebugInfo)
        {
            Debug.Log($"Moved to path index {newPathIndex} (grid {targetTile.gridPosition})");
        }
    }

    private void ActivateCurrentTile()
    {
        if (CurrentTile == null)
        {
            EndTurn();
            return;
        }

        ChangeState(LoopState.TileAction);
        CurrentTile.ActivateTile(playerObject);

        StartCoroutine(WaitForTileAction());
    }

    private IEnumerator WaitForTileAction()
    {
        Debug.Log("<color=cyan>[TILE ACTION] Waiting 1 second before checking for dialogue...</color>");
        yield return new WaitForSeconds(1f);

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            Debug.Log("<color=cyan>[TILE ACTION] Dialogue is active - waiting for it to end...</color>");
            yield return new WaitUntil(() => !DialogueManager.Instance.IsDialogueActive());
            Debug.Log("<color=cyan>[TILE ACTION] Dialogue ended - resuming game</color>");
        }
        else
        {
            Debug.Log("<color=cyan>[TILE ACTION] No dialogue active - continuing</color>");
        }

        EndTurn();
    }

    private void EndTurn()
    {
        ChangeState(LoopState.EndingTurn);
        OnTurnEnded?.Invoke(CurrentTurn);

        if (showDebugInfo)
        {
            Debug.Log($"Turn {CurrentTurn} ended.");
        }

        RestartTurn();
    }

    private void CompleteLoop()
    {
        TotalLoops++;
        OnLoopCompleted?.Invoke(TotalLoops);

        if (showDebugInfo)
        {
            Debug.Log($"Loop {TotalLoops} completed!");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"loop_{TotalLoops}_complete");
            GameManager.Instance.SaveFlags();
        }

        if (restartLoopAutomatically)
        {
            RestartLoop();
        }
        else
        {
            ChangeState(LoopState.GameOver);
        }
    }

    public void RestartLoop()
    {
        CurrentTurn = 0;

        if (showDebugInfo)
        {
            Debug.Log("Restarting loop...");
        }

        RestartTurn();
    }

    private void RestartTurn()
    {
        remainingMoves = 0;
        ChangeState(LoopState.WaitingToRoll);
    }

    private void ChangeState(LoopState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);

        if (showDebugInfo)
        {
            Debug.Log($"State changed to: {newState}");
        }
    }

    public bool CanRollDice()
    {
        return CurrentState == LoopState.WaitingToRoll && !isProcessing;
    }

    public bool IsPlayerMoving()
    {
        return CurrentState == LoopState.Moving;
    }

    public int GetRemainingMoves()
    {
        return remainingMoves;
    }

    private void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            return;
        }
        
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && CanRollDice())
        {
            StartTurn();
        }
    }

    private void OnDestroy()
    {
        if (diceRoller != null)
        {
            diceRoller.OnRollComplete -= OnDiceRolled;
        }
    }
}
