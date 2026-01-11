using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueTextField : MonoBehaviour
{
    [Header("Text References")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Animation Settings")]
    public bool animateSpeakerName = true;
    public float speakerNameFadeSpeed = 5f;

    [Header("Skip Indicator")]
    public GameObject skipIndicator;
    public float blinkSpeed = 1f;

    private CanvasGroup speakerNameCanvasGroup;
    private bool isTyping;
    private float blinkTimer;

    private void Awake()
    {
        if (speakerNameText != null && animateSpeakerName)
        {
            speakerNameCanvasGroup = speakerNameText.GetComponent<CanvasGroup>();
            if (speakerNameCanvasGroup == null)
            {
                speakerNameCanvasGroup = speakerNameText.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (skipIndicator != null)
        {
            skipIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnNodeChanged += OnNodeChanged;
        }
    }

    private void OnNodeChanged(DialogueNode node)
    {
        if (animateSpeakerName && speakerNameCanvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeSpeakerName());
        }

        isTyping = true;
    }

    private System.Collections.IEnumerator FadeSpeakerName()
    {
        speakerNameCanvasGroup.alpha = 0f;

        while (speakerNameCanvasGroup.alpha < 1f)
        {
            speakerNameCanvasGroup.alpha += Time.deltaTime * speakerNameFadeSpeed;
            yield return null;
        }

        speakerNameCanvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
        {
            bool typing = DialogueManager.Instance.canSkipTypewriter && isTyping;

            if (skipIndicator != null && skipIndicator.activeSelf != typing)
            {
                skipIndicator.SetActive(typing);
            }

            if (typing && skipIndicator != null)
            {
                blinkTimer += Time.deltaTime * blinkSpeed;
                CanvasGroup indicatorGroup = skipIndicator.GetComponent<CanvasGroup>();
                if (indicatorGroup != null)
                {
                    indicatorGroup.alpha = Mathf.PingPong(blinkTimer, 1f);
                }
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                isTyping = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnNodeChanged -= OnNodeChanged;
        }
    }
}
