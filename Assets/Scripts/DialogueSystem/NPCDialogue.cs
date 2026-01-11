using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Configuration")]
    public List<DialogueData> dialogues = new List<DialogueData>();
    public bool useProximityDetection = true;
    public float interactionDistance = 3f;

    [Header("Input Settings")]
    public KeyCode interactKey = KeyCode.E;
    public InputActionReference interactAction;

    [Header("Visual Feedback")]
    public GameObject interactionPrompt;
    public string promptText = "Press E to talk";

    [Header("Dialogue Behavior")]
    public bool canRepeatDialogue = true;
    public bool autoStartOnTrigger = false;
    public float dialogueCooldown = 0.5f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private Transform playerTransform;
    private bool playerInRange;
    private bool canInteract = true;
    private float lastInteractionTime;
    private DialogueData currentAvailableDialogue;

    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"NPCDialogue on {gameObject.name}: No GameObject with 'Player' tag found in scene");
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += OnDialogueComplete;
        }

        if (dialogues.Count == 0)
        {
            Debug.LogWarning($"NPCDialogue on {gameObject.name}: No dialogues assigned");
        }
    }

    private void Update()
    {
        if (playerTransform == null || DialogueManager.Instance == null)
            return;

        if (useProximityDetection)
        {
            CheckPlayerProximity();
        }

        if (playerInRange && canInteract && !DialogueManager.Instance.IsDialogueActive())
        {
            currentAvailableDialogue = GetAvailableDialogue();

            if (currentAvailableDialogue != null)
            {
                ShowInteractionPrompt(true);

                if (autoStartOnTrigger)
                {
                    StartDialogue();
                }
                else if (CheckInteractInput())
                {
                    StartDialogue();
                }
            }
            else
            {
                ShowInteractionPrompt(false);
            }
        }
        else
        {
            ShowInteractionPrompt(false);
        }
    }

    private void CheckPlayerProximity()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        playerInRange = distance <= interactionDistance;
    }

    private bool CheckInteractInput()
    {
        bool inputDetected = false;

        if (interactAction != null && interactAction.action != null)
        {
            inputDetected = interactAction.action.WasPressedThisFrame();
        }
        else if (Keyboard.current != null)
        {
            Key key = ConvertKeyCodeToKey(interactKey);
            if (key != Key.None)
            {
                inputDetected = Keyboard.current[key].wasPressedThisFrame;
            }
        }

        return inputDetected;
    }

    private Key ConvertKeyCodeToKey(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.E: return Key.E;
            case KeyCode.Space: return Key.Space;
            case KeyCode.Return: return Key.Enter;
            case KeyCode.F: return Key.F;
            case KeyCode.Q: return Key.Q;
            default: return Key.None;
        }
    }

    private DialogueData GetAvailableDialogue()
    {
        if (dialogues.Count == 0)
            return null;

        for (int i = dialogues.Count - 1; i >= 0; i--)
        {
            DialogueData dialogue = dialogues[i];

            if (dialogue == null)
                continue;

            if (dialogue.CanStart())
            {
                return dialogue;
            }
        }

        if (canRepeatDialogue && dialogues.Count > 0)
        {
            return dialogues[0];
        }

        return null;
    }

    private void StartDialogue()
    {
        if (currentAvailableDialogue == null || !canInteract)
            return;

        if (Time.time - lastInteractionTime < dialogueCooldown)
            return;

        DialogueManager.Instance.StartDialogue(currentAvailableDialogue);
        lastInteractionTime = Time.time;
        canInteract = false;
        ShowInteractionPrompt(false);
    }

    private void OnDialogueComplete()
    {
        canInteract = true;
    }

    private void ShowInteractionPrompt(bool show)
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf != show)
        {
            interactionPrompt.SetActive(show);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useProximityDetection && other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!useProximityDetection && other.CompareTag("Player"))
        {
            playerInRange = false;
            ShowInteractionPrompt(false);
        }
    }

    public void AddDialogue(DialogueData dialogue)
    {
        if (dialogue != null && !dialogues.Contains(dialogue))
        {
            dialogues.Add(dialogue);
        }
    }

    public void RemoveDialogue(DialogueData dialogue)
    {
        dialogues.Remove(dialogue);
    }

    public void ClearDialogues()
    {
        dialogues.Clear();
    }

    public bool HasDialogue(DialogueData dialogue)
    {
        return dialogues.Contains(dialogue);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;

        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (playerInRange && playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueComplete;
        }
    }
}
