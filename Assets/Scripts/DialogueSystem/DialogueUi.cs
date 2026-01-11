using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("Panel References")]
    public CanvasGroup dialogueCanvasGroup;
    public GameObject dialoguePanel;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;

    private void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += ShowDialogue;
            DialogueManager.Instance.OnDialogueEnded += HideDialogue;
        }

        HideDialogueImmediate();
    }

    private void ShowDialogue()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 0f, 1f, fadeInDuration));
    }

    private void HideDialogue()
    {
        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(dialogueCanvasGroup, 1f, 0f, fadeOutDuration, () =>
        {
            gameObject.SetActive(false);
        }));
    }

    private void HideDialogueImmediate()
    {
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= ShowDialogue;
            DialogueManager.Instance.OnDialogueEnded -= HideDialogue;
        }
    }
}
