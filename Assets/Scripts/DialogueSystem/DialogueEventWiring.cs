using UnityEngine;
using UnityEngine.Events;

public class DialogueEventWiring : MonoBehaviour
{
    [Header("Dialogue Events")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueEnded;
    public UnityEvent<string> onNodeChanged;

    [Header("Optional Audio")]
    public AudioClip dialogueStartSound;
    public AudioClip dialogueEndSound;
    public AudioClip nodeChangeSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (dialogueStartSound != null || dialogueEndSound != null || nodeChangeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            DialogueManager.Instance.OnNodeChanged += HandleNodeChanged;
        }
        else
        {
            Debug.LogError("DialogueManager.Instance is null. Make sure DialogueManager exists in the scene.");
        }
    }

    private void HandleDialogueStarted()
    {
        onDialogueStarted?.Invoke();

        if (audioSource != null && dialogueStartSound != null)
        {
            audioSource.PlayOneShot(dialogueStartSound);
        }

        Debug.Log("Dialogue Started");
    }

    private void HandleDialogueEnded()
    {
        onDialogueEnded?.Invoke();

        if (audioSource != null && dialogueEndSound != null)
        {
            audioSource.PlayOneShot(dialogueEndSound);
        }

        Debug.Log("Dialogue Ended");
    }

    private void HandleNodeChanged(DialogueNode node)
    {
        if (node != null)
        {
            onNodeChanged?.Invoke(node.speakerName);

            if (audioSource != null && nodeChangeSound != null)
            {
                audioSource.PlayOneShot(nodeChangeSound);
            }

            Debug.Log($"Node Changed: {node.speakerName}");
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            DialogueManager.Instance.OnNodeChanged -= HandleNodeChanged;
        }
    }
}
