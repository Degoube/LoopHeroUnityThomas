using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoopUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI loopText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI diceResultText;
    public TextMeshProUGUI movesRemainingText;
    public Button rollButton;

    [Header("Dice Visual")]
    public Image diceImage;
    public Sprite[] diceFaces;

    private void Start()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnStateChanged += UpdateStateDisplay;
            PlayerLoopController.Instance.OnTurnStarted += UpdateTurnDisplay;
            PlayerLoopController.Instance.OnLoopCompleted += UpdateLoopDisplay;
        }

        if (PlayerLoopController.Instance.diceRoller != null)
        {
            PlayerLoopController.Instance.diceRoller.OnRollComplete += UpdateDiceDisplay;
        }

        if (rollButton != null)
        {
            rollButton.onClick.AddListener(OnRollButtonClicked);
        }

        UpdateAllDisplays();
    }

    private void Update()
    {
        UpdateMovesDisplay();
        UpdateButtonState();
    }

    private void UpdateAllDisplays()
    {
        if (PlayerLoopController.Instance == null)
            return;

        UpdateTurnDisplay(PlayerLoopController.Instance.CurrentTurn);
        UpdateLoopDisplay(PlayerLoopController.Instance.TotalLoops);
        UpdateStateDisplay(PlayerLoopController.Instance.CurrentState);
    }

    private void UpdateTurnDisplay(int turn)
    {
        if (turnText != null)
        {
            turnText.text = $"Turn: {turn}";
        }
    }

    private void UpdateLoopDisplay(int loop)
    {
        if (loopText != null)
        {
            loopText.text = $"Loop: {loop}";
        }
    }

    private void UpdateStateDisplay(LoopState state)
    {
        if (stateText != null)
        {
            stateText.text = $"State: {state}";
        }
    }

    private void UpdateDiceDisplay(int result)
    {
        if (diceResultText != null)
        {
            diceResultText.text = $"Rolled: {result}";
        }

        if (diceImage != null && diceFaces != null && result > 0 && result <= diceFaces.Length)
        {
            diceImage.sprite = diceFaces[result - 1];
        }
    }

    private void UpdateMovesDisplay()
    {
        if (movesRemainingText != null && PlayerLoopController.Instance != null)
        {
            int remaining = PlayerLoopController.Instance.GetRemainingMoves();
            movesRemainingText.text = $"Moves: {remaining}";
        }
    }

    private void UpdateButtonState()
    {
        if (rollButton != null && PlayerLoopController.Instance != null)
        {
            rollButton.interactable = PlayerLoopController.Instance.CanRollDice();
        }
    }

    private void OnRollButtonClicked()
    {
        if (PlayerLoopController.Instance != null && PlayerLoopController.Instance.CanRollDice())
        {
            PlayerLoopController.Instance.StartTurn();
        }
    }

    private void OnDestroy()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnStateChanged -= UpdateStateDisplay;
            PlayerLoopController.Instance.OnTurnStarted -= UpdateTurnDisplay;
            PlayerLoopController.Instance.OnLoopCompleted -= UpdateLoopDisplay;

            if (PlayerLoopController.Instance.diceRoller != null)
            {
                PlayerLoopController.Instance.diceRoller.OnRollComplete -= UpdateDiceDisplay;
            }
        }

        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(OnRollButtonClicked);
        }
    }
}
