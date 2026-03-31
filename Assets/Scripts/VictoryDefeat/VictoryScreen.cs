using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryScreen : EndScreenBase
{
    [Header("UI References")]
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victoryMessageText;
    public TextMeshProUGUI statsText;
    public Button restartButton;
    public Button quitButton;

    [Header("Victory Messages")]
    public string victoryTitle = "VICTORY!";
    public string victoryMessage = "You have uncovered the truth!";

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnVictory += ShowVictoryScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void ShowVictoryScreen()
    {
        Show();

        if (victoryTitleText != null)   victoryTitleText.text   = victoryTitle;
        if (victoryMessageText != null) victoryMessageText.text = victoryMessage;

        UpdateStatsText();
    }

    private void UpdateStatsText()
    {
        if (statsText == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>STATISTIQUES FINALES</b>\n");

        if (PlayerLoopController.Instance != null)
        {
            sb.AppendLine($"<color=#FFD700>Boucles Complétées:</color> {PlayerLoopController.Instance.TotalLoops}");
            sb.AppendLine($"<color=#FFD700>Tours Joués:</color> {PlayerLoopController.Instance.CurrentTurn}");
        }

        if (ResourceManager.Instance != null)
            sb.AppendLine($"<color=#00FF00>Ressources Restantes:</color> {ResourceManager.Instance.CurrentResources}");

        if (GameManager.Instance != null)
        {
            sb.AppendLine("\n<b>QUÊTES COMPLÉTÉES</b>");
            AppendQuestLine(sb, NarrativeFlags.MetWitness,     "Témoin rencontré",    "#CC00FF");
            AppendQuestLine(sb, NarrativeFlags.VisitedRuins,   "Ruines explorées",    "#654321");
            AppendQuestLine(sb, NarrativeFlags.ActivatedAltar, "Autel activé",        "#0052FF");
            AppendQuestLine(sb, NarrativeFlags.FoundRelic,     "Relique trouvée",     "#FFD700");
            AppendQuestLine(sb, NarrativeFlags.TruthDone,      "Vérité découverte",   "#00FF00");

            int combatCount = CountCombatFlags();
            if (combatCount > 0)
                sb.AppendLine($"\n<color=#FF0000>Combats survivés:</color> {combatCount}");
        }

        statsText.text = sb.ToString();
    }

    private static void AppendQuestLine(System.Text.StringBuilder sb, string flag, string label, string color)
    {
        if (GameManager.Instance != null && GameManager.Instance.HasFlag(flag))
            sb.AppendLine($"✓ <color={color}>{label}</color>");
    }

    private static int CountCombatFlags()
    {
        int count = 0;
        while (GameManager.Instance != null && GameManager.Instance.HasFlag($"combat_{count}"))
            count++;
        return count;
    }

    private void OnRestartClicked() => GameStateManager.Instance?.RestartGame();
    private void OnQuitClicked()    => GameStateManager.Instance?.QuitGame();

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnVictory -= ShowVictoryScreen;

        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (quitButton != null)    quitButton.onClick.RemoveListener(OnQuitClicked);
    }
}
