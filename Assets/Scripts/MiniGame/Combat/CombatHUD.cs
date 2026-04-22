using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements for the turn-based combat mini-game.
/// Handles HP bars, action buttons, combat log, and result screen.
/// </summary>
public class CombatHUD : MonoBehaviour
{
    // ── Player UI ─────────────────────────────────────────────────────────────
    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerHPBar;
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private Image playerPortrait;

    // ── Enemy UI ──────────────────────────────────────────────────────────────
    [Header("Enemy UI")]
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image enemyHPBar;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private Image enemyPortrait;

    // ── Action Buttons ────────────────────────────────────────────────────────
    [Header("Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private GameObject actionPanel;

    // ── Skill Info ────────────────────────────────────────────────────────────
    [Header("Skill")]
    [SerializeField] private TextMeshProUGUI skillButtonText;
    [SerializeField] private TextMeshProUGUI skillCooldownText;

    // ── Combat Log ────────────────────────────────────────────────────────────
    [Header("Combat Log")]
    [SerializeField] private TextMeshProUGUI combatLogText;
    [SerializeField] private ScrollRect combatLogScroll;

    // ── Status Text ───────────────────────────────────────────────────────────
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // ── Feedback ──────────────────────────────────────────────────────────────
    [Header("Feedback")]
    [SerializeField] private RectTransform playerShakeTarget;
    [SerializeField] private RectTransform enemyShakeTarget;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 10f;

    // ── Result ────────────────────────────────────────────────────────────────
    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultDetailsText;
    [SerializeField] private Button resultCloseButton;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired when the player clicks an action button.</summary>
    public event Action<CombatActionType> OnActionSelected;

    /// <summary>Fired when the player clicks the result close button.</summary>
    public event Action OnResultClosed;

    private string combatLog = "";
    private const int MaxLogLines = 20;

    /// <summary>
    /// Initializes the HUD with fighter data and wires button events.
    /// </summary>
    public void Initialize(CombatFighter player, CombatFighter enemy, string skillName, bool fleeAllowed)
    {
        // Names
        if (playerNameText != null) playerNameText.text = player.Name;
        if (enemyNameText != null) enemyNameText.text = enemy.Name;

        // HP bars
        UpdatePlayerHP(player.CurrentHP, player.BaseStats.MaxHP);
        UpdateEnemyHP(enemy.CurrentHP, enemy.BaseStats.MaxHP);

        // Skill button text
        if (skillButtonText != null) skillButtonText.text = skillName;

        // Flee button
        if (fleeButton != null) fleeButton.gameObject.SetActive(fleeAllowed);

        // Item button (disabled for now — future feature)
        if (itemButton != null) itemButton.interactable = false;

        // Wire buttons
        WireButtons();

        // Clear log
        combatLog = "";
        if (combatLogText != null) combatLogText.text = "";

        // Hide result
        if (resultPanel != null) resultPanel.SetActive(false);

        // Hide action panel initially
        SetActionsVisible(false);
    }

    private void WireButtons()
    {
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => OnActionSelected?.Invoke(CombatActionType.Attack));
        }

        if (defendButton != null)
        {
            defendButton.onClick.RemoveAllListeners();
            defendButton.onClick.AddListener(() => OnActionSelected?.Invoke(CombatActionType.Defend));
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(() => OnActionSelected?.Invoke(CombatActionType.Skill));
        }

        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => OnActionSelected?.Invoke(CombatActionType.Item));
        }

        if (fleeButton != null)
        {
            fleeButton.onClick.RemoveAllListeners();
            fleeButton.onClick.AddListener(() => OnActionSelected?.Invoke(CombatActionType.Flee));
        }

        if (resultCloseButton != null)
        {
            resultCloseButton.onClick.RemoveAllListeners();
            resultCloseButton.onClick.AddListener(() => OnResultClosed?.Invoke());
        }
    }

    // ── HP Updates ────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the player HP bar and text.
    /// </summary>
    public void UpdatePlayerHP(int current, int max)
    {
        if (playerHPBar != null)
            playerHPBar.fillAmount = max > 0 ? (float)current / max : 0f;

        if (playerHPText != null)
            playerHPText.text = $"{current} / {max}";
    }

    /// <summary>
    /// Updates the enemy HP bar and text.
    /// </summary>
    public void UpdateEnemyHP(int current, int max)
    {
        if (enemyHPBar != null)
            enemyHPBar.fillAmount = max > 0 ? (float)current / max : 0f;

        if (enemyHPText != null)
            enemyHPText.text = $"{current} / {max}";
    }

    // ── Action Panel ──────────────────────────────────────────────────────────

    /// <summary>
    /// Shows or hides the action buttons.
    /// </summary>
    public void SetActionsVisible(bool visible)
    {
        if (actionPanel != null)
            actionPanel.SetActive(visible);
    }

    /// <summary>
    /// Updates the skill button state based on cooldown.
    /// </summary>
    public void UpdateSkillCooldown(int remainingTurns)
    {
        if (skillButton != null)
            skillButton.interactable = remainingTurns <= 0;

        if (skillCooldownText != null)
            skillCooldownText.text = remainingTurns > 0 ? $"({remainingTurns})" : "";
    }

    // ── Turn Indicator ────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the turn indicator text.
    /// </summary>
    public void SetTurnIndicator(string text)
    {
        if (turnIndicatorText != null)
            turnIndicatorText.text = text;
    }

    // ── Combat Log ────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a line to the combat log.
    /// </summary>
    public void LogMessage(string message)
    {
        combatLog += message + "\n";

        // Trim old lines
        string[] lines = combatLog.Split('\n');
        if (lines.Length > MaxLogLines)
        {
            int start = lines.Length - MaxLogLines;
            combatLog = string.Join("\n", lines, start, MaxLogLines);
        }

        if (combatLogText != null)
            combatLogText.text = combatLog;

        // Auto-scroll to bottom
        if (combatLogScroll != null)
            StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (combatLogScroll != null)
            combatLogScroll.verticalNormalizedPosition = 0f;
    }

    // ── Feedback ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers a shake effect on the specified target.
    /// </summary>
    public void ShakeTarget(bool isPlayer)
    {
        RectTransform target = isPlayer ? playerShakeTarget : enemyShakeTarget;
        if (target != null)
            StartCoroutine(ShakeCoroutine(target));
    }

    private IEnumerator ShakeCoroutine(RectTransform target)
    {
        Vector2 originalPos = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            float y = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            target.anchoredPosition = originalPos + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.anchoredPosition = originalPos;
    }

    // ── Result Screen ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the result panel with victory/defeat info and rewards.
    /// </summary>
    public void ShowResult(bool victory, string details)
    {
        SetActionsVisible(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = victory ? "VICTOIRE !" : "DEFAITE...";

        if (resultDetailsText != null)
            resultDetailsText.text = details;
    }

    private void OnDestroy()
    {
        if (attackButton != null) attackButton.onClick.RemoveAllListeners();
        if (defendButton != null) defendButton.onClick.RemoveAllListeners();
        if (skillButton != null) skillButton.onClick.RemoveAllListeners();
        if (itemButton != null) itemButton.onClick.RemoveAllListeners();
        if (fleeButton != null) fleeButton.onClick.RemoveAllListeners();
        if (resultCloseButton != null) resultCloseButton.onClick.RemoveAllListeners();
    }
}
