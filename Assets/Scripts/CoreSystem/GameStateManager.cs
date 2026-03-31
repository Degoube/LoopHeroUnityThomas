using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Win Condition")]
    public string victoryFlag = NarrativeFlags.Victory;

    [Header("Lose Conditions")]
    public bool checkResourceDepletion = true;
    public int minResourcesThreshold = 5;

    [Header("Settings")]
    public bool checkConditionsEveryTurn = true;

    public GameState CurrentGameState { get; private set; }

    public event Action OnVictory;
    public event Action OnDefeat;
    public event Action<GameState> OnGameStateChanged;

    // Guard: do not evaluate win/lose until the player has actually taken a turn.
    // Prevents false defeat triggers during scene initialisation order issues.
    private bool hasGameStarted;

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
        CurrentGameState = GameState.Playing;
        hasGameStarted = false;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesDepleted += HandleResourcesDepleted;

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnStarted += HandleTurnStarted;
            PlayerLoopController.Instance.OnTurnEnded   += HandleTurnEnded;
        }

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
    }

    // First real turn marks the game as started — safe to check conditions from here on.
    private void HandleTurnStarted(int turn)
    {
        hasGameStarted = true;
    }

    private void HandleTurnEnded(int turn)
    {
        if (!hasGameStarted || !checkConditionsEveryTurn)
            return;

        CheckWinConditions();
        CheckLoseConditions();
    }

    private void HandleDialogueEnded()
    {
        if (!hasGameStarted)
            return;

        CheckWinConditions();
        CheckLoseConditions();
    }

    private void HandleResourcesDepleted()
    {
        if (!hasGameStarted)
            return;

        if (checkResourceDepletion)
            TriggerDefeat();
    }

    private void CheckWinConditions()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        if (GameManager.Instance != null && GameManager.Instance.HasFlag(victoryFlag))
            TriggerVictory();
    }

    private void CheckLoseConditions()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        if (checkResourceDepletion
            && ResourceManager.Instance != null
            && ResourceManager.Instance.CurrentResources < minResourcesThreshold)
        {
            TriggerDefeat();
        }
    }

    /// <summary>Triggers the victory state and fires OnVictory.</summary>
    public void TriggerVictory()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        Debug.Log("<color=green>============ VICTORY ACHIEVED! ============</color>");
        ChangeGameState(GameState.Victory);
        OnVictory?.Invoke();

        GameManager.Instance?.AddFlag(NarrativeFlags.GameWon);

        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = false;
    }

    /// <summary>Triggers the defeat state and fires OnDefeat.</summary>
    public void TriggerDefeat()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        Debug.Log("<color=red>============ DEFEAT ============</color>");
        ChangeGameState(GameState.Defeat);
        OnDefeat?.Invoke();

        GameManager.Instance?.AddFlag(NarrativeFlags.GameLost);

        if (PlayerLoopController.Instance != null)
            PlayerLoopController.Instance.enabled = false;
    }

    private void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState)
            return;

        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>Resets resources and reloads the active scene.</summary>
    public void RestartGame()
    {
        GameManager.Instance?.ResetGame();
        ResourceManager.Instance?.ResetResources();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourcesDepleted -= HandleResourcesDepleted;

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnStarted -= HandleTurnStarted;
            PlayerLoopController.Instance.OnTurnEnded   -= HandleTurnEnded;
        }

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }
}
