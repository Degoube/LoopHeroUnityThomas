using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the UI elements for the Relic Slash (Fruit Ninja) mini-game.
/// Displays score, timer, combo, and result screen.
/// </summary>
public class RelicSlashHUD : MonoBehaviour
{
    // ── Score ──────────────────────────────────────────────────────────────────
    [Header("Score")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI targetScoreText;

    // ── Timer ─────────────────────────────────────────────────────────────────
    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image timerBar;

    // ── Combo ─────────────────────────────────────────────────────────────────
    [Header("Combo")]
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float comboFadeDelay = 1f;

    // ── Feedback ──────────────────────────────────────────────────────────────
    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackFadeDuration = 0.5f;

    // ── Result ────────────────────────────────────────────────────────────────
    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private TextMeshProUGUI resultRewardsText;
    [SerializeField] private Button resultCloseButton;

    /// <summary>Fired when the player closes the result screen.</summary>
    public event Action OnResultClosed;

    private float totalDuration;
    private Coroutine comboFadeCoroutine;
    private Coroutine feedbackCoroutine;

    /// <summary>
    /// Initializes the HUD with game parameters.
    /// </summary>
    public void Initialize(float duration, int targetScore)
    {
        totalDuration = duration;

        if (scoreText != null) scoreText.text = "0";
        if (targetScoreText != null) targetScoreText.text = $"Objectif: {targetScore}";
        if (comboText != null) comboText.text = "";
        if (feedbackText != null) feedbackText.text = "";
        if (resultPanel != null) resultPanel.SetActive(false);

        UpdateTimer(duration);

        if (resultCloseButton != null)
        {
            resultCloseButton.onClick.RemoveAllListeners();
            resultCloseButton.onClick.AddListener(() => OnResultClosed?.Invoke());
        }
    }

    /// <summary>
    /// Updates the displayed score.
    /// </summary>
    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    /// <summary>
    /// Updates the timer display.
    /// </summary>
    public void UpdateTimer(float remainingTime)
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, remainingTime)).ToString();

        if (timerBar != null && totalDuration > 0)
            timerBar.fillAmount = Mathf.Clamp01(remainingTime / totalDuration);
    }

    /// <summary>
    /// Shows the current combo multiplier text.
    /// </summary>
    public void ShowCombo(string comboDisplay)
    {
        if (comboText == null) return;

        comboText.text = comboDisplay;
        comboText.color = Color.white;

        if (comboFadeCoroutine != null)
            StopCoroutine(comboFadeCoroutine);

        comboFadeCoroutine = StartCoroutine(FadeText(comboText, comboFadeDelay));
    }

    /// <summary>
    /// Hides the combo text (on combo break).
    /// </summary>
    public void HideCombo()
    {
        if (comboText != null)
            comboText.text = "";
    }

    /// <summary>
    /// Shows floating feedback text ("+10", "PIEGE!", etc.).
    /// </summary>
    public void ShowFeedback(string text, Color color)
    {
        if (feedbackText == null) return;

        feedbackText.text = text;
        feedbackText.color = color;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FadeText(feedbackText, feedbackFadeDuration));
    }

    /// <summary>
    /// Shows the result screen with score and rewards.
    /// </summary>
    public void ShowResult(bool victory, int finalScore, int targetScore, string rewardsText)
    {
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = victory ? "VICTOIRE !" : "TEMPS ECOULE...";

        if (resultScoreText != null)
            resultScoreText.text = $"Score: {finalScore} / {targetScore}";

        if (resultRewardsText != null)
            resultRewardsText.text = rewardsText;
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float delay)
    {
        yield return new WaitForSeconds(delay);

        float duration = 0.3f;
        float elapsed = 0f;
        Color startColor = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = startColor;
            c.a = 1f - (elapsed / duration);
            text.color = c;
            yield return null;
        }

        text.text = "";
    }

    private void OnDestroy()
    {
        if (resultCloseButton != null)
            resultCloseButton.onClick.RemoveAllListeners();
    }
}
