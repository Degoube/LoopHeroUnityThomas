using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefeatScreen : EndScreenBase
{
    [Header("UI References")]
    public TextMeshProUGUI defeatTitleText;
    public TextMeshProUGUI defeatMessageText;
    public TextMeshProUGUI reasonText;
    public Button restartButton;
    public Button quitButton;

    [Header("Defeat Messages")]
    public string defeatTitle = "DEFEAT";

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnDefeat += ShowDefeatScreen;

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void ShowDefeatScreen()
    {
        Show();

        if (defeatTitleText != null)   defeatTitleText.text   = defeatTitle;
        if (defeatMessageText != null) defeatMessageText.text = "The loop continues...";

        UpdateReasonText();
    }

    private void UpdateReasonText()
    {
        if (reasonText == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (ResourceManager.Instance != null && ResourceManager.Instance.CurrentResources < 5)
            sb.AppendLine($"<color=#FF0000>Ressources insuffisantes ({ResourceManager.Instance.CurrentResources} < 5)</color>");
        else
            sb.AppendLine("Vous n'avez pas réussi à découvrir la vérité.");

        if (PlayerLoopController.Instance != null)
            sb.AppendLine($"\n<color=#FFD700>Vous avez survécu {PlayerLoopController.Instance.TotalLoops} boucles.</color>");

        if (GameManager.Instance != null)
        {
            sb.AppendLine("\n<b>PROGRESSION</b>");
            AppendProgressLine(sb, NarrativeFlags.MetWitness,     "Témoin rencontré");
            AppendProgressLine(sb, NarrativeFlags.VisitedRuins,   "Ruines explorées");
            AppendProgressLine(sb, NarrativeFlags.ActivatedAltar, "Autel activé");
            AppendProgressLine(sb, NarrativeFlags.FoundRelic,     "Relique trouvée");

            int combatCount = CountCombatFlags();
            if (combatCount > 0)
                sb.AppendLine($"\n<color=#FF0000>{combatCount} combats survivés</color>");
        }

        reasonText.text = sb.ToString();
    }

    private static void AppendProgressLine(System.Text.StringBuilder sb, string flag, string label)
    {
        bool done = GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
        sb.AppendLine(done ? $"✓ {label}" : $"○ {label}");
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
            GameStateManager.Instance.OnDefeat -= ShowDefeatScreen;

        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
        if (quitButton != null)    quitButton.onClick.RemoveListener(OnQuitClicked);
    }
}
