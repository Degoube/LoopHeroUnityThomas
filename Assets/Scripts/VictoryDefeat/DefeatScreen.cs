using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefeatScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject defeatPanel;
    public TextMeshProUGUI defeatTitleText;
    public TextMeshProUGUI defeatMessageText;
    public TextMeshProUGUI reasonText;
    public Button restartButton;
    public Button quitButton;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 1f;

    [Header("Audio")]
    public AudioClip defeatMusic;
    public AudioClip defeatSound;

    [Header("Defeat Messages")]
    public string defeatTitle = "DEFEAT";
    public string resourcesDepletedMessage = "Your resources have been depleted...";

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }

        if (canvasGroup == null && defeatPanel != null)
        {
            canvasGroup = defeatPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = defeatPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnDefeat += ShowDefeatScreen;
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

    private void ShowDefeatScreen()
    {
        Debug.Log("<color=red>████████████████████████████████████████</color>");
        Debug.Log("<color=red>████   DEFEAT SCREEN DISPLAYING!   ████</color>");
        Debug.Log("<color=red>████████████████████████████████████████</color>");

        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            Debug.Log("<color=red>Defeat panel activated successfully</color>");
        }
        else
        {
            Debug.LogError("<color=red>Defeat panel is NULL!</color>");
        }

        UpdateDefeatText();
        DetermineDefeatReason();

        if (defeatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(defeatSound);
        }

        if (defeatMusic != null && audioSource != null)
        {
            audioSource.clip = defeatMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        StartCoroutine(FadeIn());
    }

    private void UpdateDefeatText()
    {
        if (defeatTitleText != null)
        {
            defeatTitleText.text = defeatTitle;
        }

        if (defeatMessageText != null)
        {
            defeatMessageText.text = "The loop continues...";
        }
    }

    private void DetermineDefeatReason()
    {
        if (reasonText == null)
            return;

        string reason = "";

        if (ResourceManager.Instance != null && ResourceManager.Instance.CurrentResources < 5)
        {
            reason = $"<color=#FF0000>Ressources insuffisantes ({ResourceManager.Instance.CurrentResources} < 5)</color>";
        }
        else
        {
            reason = "Vous n'avez pas réussi à découvrir la vérité.";
        }

        if (PlayerLoopController.Instance != null)
        {
            reason += $"\n\n<color=#FFD700>Vous avez survécu {PlayerLoopController.Instance.TotalLoops} boucles.</color>";
        }

        if (GameManager.Instance != null)
        {
            reason += "\n\n<b>PROGRESSION</b>\n";
            
            if (GameManager.Instance.HasFlag("met_witness"))
                reason += "✓ Témoin rencontré\n";
            else
                reason += "○ Témoin non rencontré\n";
            
            if (GameManager.Instance.HasFlag("visited_ruins"))
                reason += "✓ Ruines explorées\n";
            else
                reason += "○ Ruines non explorées\n";
            
            if (GameManager.Instance.HasFlag("activated_altar"))
                reason += "✓ Autel activé\n";
            else
                reason += "○ Autel non activé\n";
            
            if (GameManager.Instance.HasFlag("found_relic"))
                reason += "✓ Relique trouvée\n";
            else
                reason += "○ Relique non trouvée\n";
            
            int combatCount = 0;
            while (GameManager.Instance.HasFlag($"combat_{combatCount}"))
            {
                combatCount++;
            }
            
            if (combatCount > 0)
                reason += $"\n<color=#FF0000>{combatCount} combats survivés</color>";
        }

        reasonText.text = reason;
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
            GameStateManager.Instance.OnDefeat -= ShowDefeatScreen;
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
