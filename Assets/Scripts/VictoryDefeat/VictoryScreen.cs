using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victoryMessageText;
    public TextMeshProUGUI statsText;
    public Button restartButton;
    public Button quitButton;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 1f;

    [Header("Audio")]
    public AudioClip victoryMusic;
    public AudioClip victorySound;

    [Header("Victory Messages")]
    public string victoryTitle = "VICTORY!";
    public string victoryMessage = "You have uncovered the truth!";

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (canvasGroup == null && victoryPanel != null)
        {
            canvasGroup = victoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = victoryPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVictory += ShowVictoryScreen;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void ShowVictoryScreen()
    {
        Debug.Log("<color=lime>████████████████████████████████████████</color>");
        Debug.Log("<color=lime>████  VICTORY SCREEN DISPLAYING!  ████</color>");
        Debug.Log("<color=lime>████████████████████████████████████████</color>");

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            Debug.Log("<color=lime>Victory panel activated successfully</color>");
        }
        else
        {
            Debug.LogError("<color=red>Victory panel is NULL!</color>");
        }

        UpdateVictoryText();
        UpdateStatsText();

        if (victorySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        if (victoryMusic != null && audioSource != null)
        {
            audioSource.clip = victoryMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        StartCoroutine(FadeIn());
    }

    private void UpdateVictoryText()
    {
        if (victoryTitleText != null)
        {
            victoryTitleText.text = victoryTitle;
        }

        if (victoryMessageText != null)
        {
            victoryMessageText.text = victoryMessage;
        }
    }

    private void UpdateStatsText()
    {
        if (statsText == null)
            return;

        string stats = "<b>STATISTIQUES FINALES</b>\n\n";

        if (PlayerLoopController.Instance != null)
        {
            stats += $"<color=#FFD700>Boucles Complétées:</color> {PlayerLoopController.Instance.TotalLoops}\n";
            stats += $"<color=#FFD700>Tours Joués:</color> {PlayerLoopController.Instance.CurrentTurn}\n";
        }

        if (ResourceManager.Instance != null)
        {
            stats += $"<color=#00FF00>Ressources Restantes:</color> {ResourceManager.Instance.CurrentResources}\n";
        }

        if (GameManager.Instance != null)
        {
            stats += $"\n<b>QUÊTES COMPLÉTÉES</b>\n";
            
            if (GameManager.Instance.HasFlag("met_witness"))
                stats += "✓ <color=#CC00FF>Témoin rencontré</color>\n";
            
            if (GameManager.Instance.HasFlag("visited_ruins"))
                stats += "✓ <color=#654321>Ruines explorées</color>\n";
            
            if (GameManager.Instance.HasFlag("activated_altar"))
                stats += "✓ <color=#0052FF>Autel activé</color>\n";
            
            if (GameManager.Instance.HasFlag("found_relic"))
                stats += "✓ <color=#FFD700>Relique trouvée</color>\n";
            
            if (GameManager.Instance.HasFlag("truth_done"))
                stats += "✓ <color=#00FF00>Vérité découverte</color>\n";
            
            int combatCount = 0;
            while (GameManager.Instance.HasFlag($"combat_{combatCount}"))
            {
                combatCount++;
            }
            
            if (combatCount > 0)
                stats += $"\n<color=#FF0000>Combats survivés:</color> {combatCount}\n";
        }

        statsText.text = stats;
    }

    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void OnRestartClicked()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RestartGame();
        }
    }

    private void OnQuitClicked()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.QuitGame();
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnVictory -= ShowVictoryScreen;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
