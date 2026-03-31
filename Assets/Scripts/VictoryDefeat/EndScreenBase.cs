using System.Collections;
using UnityEngine;

/// <summary>
/// Shared base for VictoryScreen and DefeatScreen.
/// Handles panel activation, audio playback, and the fade-in animation.
/// </summary>
public abstract class EndScreenBase : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;

    [Header("Animation")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 1f;

    [Header("Audio")]
    public AudioClip stingerSound;
    public AudioClip loopMusic;

    protected AudioSource audioSource;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (panel != null)
            panel.SetActive(false);

        if (canvasGroup == null && panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panel.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>Activates the panel, plays audio, and fades in.</summary>
    protected void Show()
    {
        if (panel != null)
            panel.SetActive(true);

        if (audioSource != null)
        {
            if (stingerSound != null)
                audioSource.PlayOneShot(stingerSound);

            if (loopMusic != null)
            {
                audioSource.clip = loopMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
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
}
