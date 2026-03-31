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
        PlayerLoopController loop = PlayerLoopController.Instance;
        if (loop != null)
        {
            loop.OnStateChanged       += UpdateStateDisplay;
            loop.OnStateChanged       += _ => UpdateButtonState();
            loop.OnTurnStarted        += UpdateTurnDisplay;
            loop.OnLoopCompleted      += UpdateLoopDisplay;
            loop.OnMovementCompleted  += UpdateMovesDisplay;

            if (loop.diceRoller != null)
            {
                loop.diceRoller.OnRollComplete += UpdateDiceDisplay;
                loop.diceRoller.OnRollComplete += _ => UpdateMovesDisplay();
            }
        }

        if (rollButton != null)
            rollButton.onClick.AddListener(OnRollButtonClicked);

        UpdateAllDisplays();
    }

    private void UpdateAllDisplays()
    {
        PlayerLoopController loop = PlayerLoopController.Instance;
        if (loop == null)
            return;

        UpdateTurnDisplay(loop.CurrentTurn);
        UpdateLoopDisplay(loop.TotalLoops);
        UpdateStateDisplay(loop.CurrentState);
        UpdateMovesDisplay();
        UpdateButtonState();
    }

    private void UpdateTurnDisplay(int turn)
    {
        if (turnText != null)
            turnText.text = $"Turn: {turn}";
    }

    private void UpdateLoopDisplay(int loop)
    {
        if (loopText != null)
            loopText.text = $"Loop: {loop}";
    }

    private void UpdateStateDisplay(LoopState state)
    {
        if (stateText != null)
            stateText.text = $"State: {state}";
    }

    private void UpdateDiceDisplay(int result)
    {
        if (diceResultText != null)
            diceResultText.text = $"Rolled: {result}";

        if (diceImage != null && diceFaces != null && result > 0 && result <= diceFaces.Length)
            diceImage.sprite = diceFaces[result - 1];
    }

    private void UpdateMovesDisplay()
    {
        if (movesRemainingText != null && PlayerLoopController.Instance != null)
            movesRemainingText.text = $"Moves: {PlayerLoopController.Instance.GetRemainingMoves()}";
    }

    private void UpdateButtonState()
    {
        if (rollButton != null && PlayerLoopController.Instance != null)
            rollButton.interactable = PlayerLoopController.Instance.CanRollDice();
    }

    private void OnRollButtonClicked()
    {
        if (PlayerLoopController.Instance != null && PlayerLoopController.Instance.CanRollDice())
            PlayerLoopController.Instance.StartTurn();
    }

    private void OnDestroy()
    {
        PlayerLoopController loop = PlayerLoopController.Instance;
        if (loop != null)
        {
            loop.OnStateChanged      -= UpdateStateDisplay;
            loop.OnTurnStarted       -= UpdateTurnDisplay;
            loop.OnLoopCompleted     -= UpdateLoopDisplay;
            loop.OnMovementCompleted -= UpdateMovesDisplay;

            if (loop.diceRoller != null)
                loop.diceRoller.OnRollComplete -= UpdateDiceDisplay;
        }

        if (rollButton != null)
            rollButton.onClick.RemoveListener(OnRollButtonClicked);
    }
}
