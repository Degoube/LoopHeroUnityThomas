using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choicesContainer;
    public Button choiceButtonPrefab;
    public TextMeshProUGUI continuePromptText;

    [Header("Typewriter Settings")]
    public float letterDelay = 0.05f;
    public bool canSkipTypewriter = true;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<DialogueNode> OnNodeChanged;

    private DialogueData currentDialogue;
    private DialogueNode currentNode;
    private List<Button> activeChoiceButtons = new List<Button>();
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private string fullText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null)
        {
            Debug.LogError("Cannot start dialogue - DialogueData is null");
            return;
        }

        Debug.Log($"<color=cyan>════════ STARTING DIALOGUE: '{dialogue.dialogueName}' ════════</color>");
        Debug.Log($"<color=cyan>Required flag: '{dialogue.requiredFlagToStart}' | Flag to set on complete: '{dialogue.flagToSetOnComplete}'</color>");

        if (!dialogue.CanStart())
        {
            Debug.LogWarning($"Cannot start dialogue '{dialogue.dialogueName}' - missing required flag: {dialogue.requiredFlagToStart}");
            return;
        }

        currentDialogue = dialogue;
        currentNode = dialogue.startNode;

        if (currentNode == null)
        {
            Debug.LogError("Dialogue has no start node");
            return;
        }

        dialoguePanel.SetActive(true);
        HideContinuePrompt();
        OnDialogueStarted?.Invoke();
        DisplayCurrentNode();
    }

    private void DisplayCurrentNode()
    {
        if (currentNode == null || !currentNode.CanDisplay())
        {
            EndDialogue();
            return;
        }

        currentNode.OnNodeVisit();
        OnNodeChanged?.Invoke(currentNode);

        if (speakerNameText != null)
        {
            speakerNameText.text = currentNode.speakerName;
        }

        ClearChoices();

        fullText = currentNode.dialogueText;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(TypewriterEffect());
    }

    private IEnumerator TypewriterEffect()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in fullText)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(letterDelay);
        }

        isTyping = false;
        OnTypewriterComplete();
    }

    private void OnTypewriterComplete()
    {
        List<DialogueChoice> availableChoices = currentNode.GetAvailableChoices();

        Debug.Log($"<color=magenta>[TYPEWRITER COMPLETE]</color> Choices available: {availableChoices.Count}");

        if (availableChoices.Count > 0)
        {
            Debug.Log("<color=magenta>Displaying choices...</color>");
            DisplayChoices(availableChoices);
            HideContinuePrompt();
        }
        else if (currentNode.nextNode != null)
        {
            Debug.Log("<color=magenta>Auto-advancing to next node...</color>");
            currentNode = currentNode.nextNode;
            Invoke(nameof(DisplayCurrentNode), 0.5f);
        }
        else
        {
            Debug.Log("Dialogue node complete - Press SPACE to close");
            ShowContinuePrompt();
        }
    }

    private void ShowContinuePrompt()
    {
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
            continuePromptText.text = "Appuyez sur ESPACE pour continuer";
        }
    }

    private void HideContinuePrompt()
    {
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }
    }

    private void DisplayChoices(List<DialogueChoice> choices)
    {
        ClearChoices();

        if (choiceButtonPrefab == null)
        {
            Debug.LogError("<color=red>[DIALOGUE ERROR]</color> choiceButtonPrefab is NULL! Cannot display choices.");
            return;
        }

        if (choicesContainer == null)
        {
            Debug.LogError("<color=red>[DIALOGUE ERROR]</color> choicesContainer is NULL! Cannot display choices.");
            return;
        }

        Debug.Log($"<color=magenta>Creating {choices.Count} choice buttons...</color>");

        foreach (DialogueChoice choice in choices)
        {
            Button choiceButton = Instantiate(choiceButtonPrefab, choicesContainer.transform);
            choiceButton.gameObject.SetActive(true);
            
            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = choice.choiceText;
            }

            DialogueChoice capturedChoice = choice;
            choiceButton.onClick.AddListener(() => OnChoiceSelected(capturedChoice));

            activeChoiceButtons.Add(choiceButton);
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        Debug.Log($"Choice selected: '{choice.choiceText}', nextNode is null: {choice.nextNode == null}");
        
        choice.ExecuteChoice();
        currentNode = choice.nextNode;
        
        if (currentNode == null)
        {
            Debug.Log("No next node, ending dialogue...");
            EndDialogue();
        }
        else
        {
            DisplayCurrentNode();
        }
    }

    private void ClearChoices()
    {
        foreach (Button button in activeChoiceButtons)
        {
            Destroy(button.gameObject);
        }

        activeChoiceButtons.Clear();
    }

    public void SkipTypewriter()
    {
        if (isTyping && canSkipTypewriter)
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            dialogueText.text = fullText;
            isTyping = false;
            OnTypewriterComplete();
        }
    }

    public void EndDialogue()
    {
        Debug.Log("<color=yellow>====== ENDING DIALOGUE ======</color>");
        
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        ClearChoices();

        if (currentDialogue != null)
        {
            currentDialogue.OnDialogueComplete();
        }

        currentDialogue = null;
        currentNode = null;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        HideContinuePrompt();

        Debug.Log("Dialogue Ended - Game should resume");
        OnDialogueEnded?.Invoke();
    }

    public bool IsDialogueActive()
    {
        return currentDialogue != null;
    }

    private void Update()
    {
        if (IsDialogueActive())
        {
            // Support pour ESC (force close) - Compatible ancien et nouveau Input System
            bool escPressed = false;
            if (Keyboard.current != null)
            {
                escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            else
            {
                escPressed = Input.GetKeyDown(KeyCode.Escape);
            }

            if (escPressed)
            {
                Debug.LogWarning("<color=red>ESC pressed - Force closing dialogue</color>");
                EndDialogue();
                return;
            }

            // Support pour SPACE
            bool spacePressed = false;
            if (Keyboard.current != null)
            {
                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            else
            {
                spacePressed = Input.GetKeyDown(KeyCode.Space);
            }
            
            if (spacePressed)
            {
                if (isTyping && canSkipTypewriter)
                {
                    SkipTypewriter();
                }
                else if (!isTyping)
                {
                    List<DialogueChoice> availableChoices = currentNode?.GetAvailableChoices();
                    if (availableChoices == null || availableChoices.Count == 0)
                    {
                        if (currentNode?.nextNode == null)
                        {
                            Debug.Log("Player pressed SPACE - Closing dialogue");
                            EndDialogue();
                        }
                    }
                }
            }
        }
    }
}
