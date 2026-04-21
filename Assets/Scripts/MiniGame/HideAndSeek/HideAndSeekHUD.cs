using UnityEngine;
using TMPro;

/// <summary>
/// Manages the UI overlay for the Hide & Seek mini-game:
/// timer, controls hint, and result screen.
/// </summary>
public class HideAndSeekHUD : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Text displayed at the top-center showing remaining time.")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Controls Hint")]
    [Tooltip("Panel showing ZQSD + E controls at game start.")]
    [SerializeField] private GameObject controlsHintPanel;

    [Tooltip("Formatted text inside the controls hint panel.")]
    [SerializeField] private TextMeshProUGUI controlsHintText;

    [Header("Result Screen")]
    [Tooltip("Panel shown at the end of the mini-game with the outcome.")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("Title text: 'Victoire !' or 'Defaite !'")]
    [SerializeField] private TextMeshProUGUI resultTitleText;

    [Tooltip("Details text: gold and XP gained/lost.")]
    [SerializeField] private TextMeshProUGUI resultDetailsText;

    /// <summary>Initializes HUD elements and shows the controls hint.</summary>
    public void Initialize()
    {
        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(true);

        if (controlsHintText != null)
            controlsHintText.text = "<b>ZQSD</b> — Se deplacer\n<b>E</b> — Se cacher / Sortir";

        if (resultPanel != null)
            resultPanel.SetActive(false);

        UpdateTimer(0f);
    }

    /// <summary>Hides the controls hint after the intro period.</summary>
    public void HideControlsHint()
    {
        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(false);
    }

    /// <summary>Updates the centered timer display.</summary>
    public void UpdateTimer(float remainingSeconds)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingSeconds);
        timerText.text = $"{seconds}s";

        // Color feedback: red when < 5s
        timerText.color = remainingSeconds < 5f ? Color.red : Color.white;
    }

    /// <summary>Shows the result screen with victory or defeat info.</summary>
    public void ShowResult(bool victory, int goldDelta, int xpDelta)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = victory ? "Victoire !" : "Defaite !";

        if (resultDetailsText != null)
        {
            string goldSign = goldDelta >= 0 ? "+" : "";
            string goldColor = goldDelta >= 0 ? "#FFD700" : "#FF4444";
            string xpColor = "#00FF00";

            resultDetailsText.text =
                $"Or : <color={goldColor}>{goldSign}{goldDelta}</color>\n" +
                $"XP : <color={xpColor}>+{xpDelta}</color>";
        }
    }

    /// <summary>Hides all HUD elements.</summary>
    public void HideAll()
    {
        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}
