using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Win Condition")]
    public string victoryFlag = "truth_done";

    [Header("Lose Conditions")]
    public bool checkResourceDepletion = true;
    public int minResourcesThreshold = 5;

    [Header("Settings")]
    public bool checkConditionsEveryTurn = true;
    public float checkInterval = 1f;

    public GameState CurrentGameState { get; private set; }

    public event Action OnVictory;
    public event Action OnDefeat;
    public event Action<GameState> OnGameStateChanged;

    private float checkTimer;

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

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesDepleted += HandleResourcesDepleted;
        }

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnEnded += HandleTurnEnded;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void Update()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckWinConditions();
            CheckLoseConditions();
        }
    }

    private void HandleTurnEnded(int turn)
    {
        if (!checkConditionsEveryTurn)
            return;

        CheckWinConditions();
        CheckLoseConditions();
    }

    private void HandleDialogueEnded()
    {
        Debug.Log("Dialogue ended - Checking win/lose conditions immediately");
        CheckWinConditions();
        CheckLoseConditions();
    }

    private void HandleResourcesDepleted()
    {
        if (checkResourceDepletion)
        {
            TriggerDefeat();
        }
    }

    private void CheckWinConditions()
    {
        if (CurrentGameState != GameState.Playing)
        {
            Debug.Log($"<color=orange>[WIN CHECK SKIPPED]</color> CurrentGameState = {CurrentGameState}");
            return;
        }

        bool hasTruthDoneFlag = GameManager.Instance != null && GameManager.Instance.HasFlag(victoryFlag);
        Debug.Log($"<color=yellow>[WIN CHECK]</color> Checking for flag '{victoryFlag}': {(hasTruthDoneFlag ? "FOUND" : "NOT FOUND")}");

        if (hasTruthDoneFlag)
        {
            Debug.Log($"<color=yellow>[WIN CONDITION MET]</color> Flag '{victoryFlag}' detected!");
            TriggerVictory();
        }
    }

    private void CheckLoseConditions()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        if (checkResourceDepletion && ResourceManager.Instance != null)
        {
            if (ResourceManager.Instance.CurrentResources < minResourcesThreshold)
            {
                Debug.Log($"<color=red>[DEFEAT CONDITION MET]</color> Ressources ({ResourceManager.Instance.CurrentResources}) < seuil ({minResourcesThreshold})");
                TriggerDefeat();
            }
        }
    }

    public void TriggerVictory()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        Debug.Log("<color=green>============ VICTORY ACHIEVED! ============</color>");
        ChangeGameState(GameState.Victory);
        OnVictory?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag("game_won");
        }

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.enabled = false;
        }
    }

    public void TriggerDefeat()
    {
        if (CurrentGameState != GameState.Playing)
            return;

        Debug.Log("<color=red>============ DEFEAT ============</color>");
        ChangeGameState(GameState.Defeat);
        OnDefeat?.Invoke();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag("game_lost");
        }

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.enabled = false;
        }
    }

    private void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState)
            return;

        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearAllFlags();
        }

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResetResources();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesDepleted -= HandleResourcesDepleted;
        }

        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnEnded -= HandleTurnEnded;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }
    }
}
