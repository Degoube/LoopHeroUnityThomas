using UnityEngine;
using UnityEngine.Events;

public class LoopEventHandler : MonoBehaviour
{
    [Header("Loop Events")]
    public UnityEvent<int> onTurnStarted;
    public UnityEvent<int> onTurnEnded;
    public UnityEvent<int> onLoopCompleted;
    public UnityEvent onMovementStarted;
    public UnityEvent onMovementCompleted;

    [Header("State Events")]
    public UnityEvent onWaitingToRoll;
    public UnityEvent onRolling;
    public UnityEvent onMoving;
    public UnityEvent onTileAction;

    [Header("Audio")]
    public AudioClip turnStartSound;
    public AudioClip loopCompleteSound;
    public AudioClip movementSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnStarted += HandleTurnStarted;
            PlayerLoopController.Instance.OnTurnEnded += HandleTurnEnded;
            PlayerLoopController.Instance.OnLoopCompleted += HandleLoopCompleted;
            PlayerLoopController.Instance.OnMovementStarted += HandleMovementStarted;
            PlayerLoopController.Instance.OnMovementCompleted += HandleMovementCompleted;
            PlayerLoopController.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void HandleTurnStarted(int turn)
    {
        onTurnStarted?.Invoke(turn);

        if (audioSource != null && turnStartSound != null)
        {
            audioSource.PlayOneShot(turnStartSound);
        }
    }

    private void HandleTurnEnded(int turn)
    {
        onTurnEnded?.Invoke(turn);
    }

    private void HandleLoopCompleted(int loop)
    {
        onLoopCompleted?.Invoke(loop);

        if (audioSource != null && loopCompleteSound != null)
        {
            audioSource.PlayOneShot(loopCompleteSound);
        }
    }

    private void HandleMovementStarted()
    {
        onMovementStarted?.Invoke();

        if (audioSource != null && movementSound != null)
        {
            audioSource.PlayOneShot(movementSound);
        }
    }

    private void HandleMovementCompleted()
    {
        onMovementCompleted?.Invoke();
    }

    private void HandleStateChanged(LoopState state)
    {
        switch (state)
        {
            case LoopState.WaitingToRoll:
                onWaitingToRoll?.Invoke();
                break;
            case LoopState.Rolling:
                onRolling?.Invoke();
                break;
            case LoopState.Moving:
                onMoving?.Invoke();
                break;
            case LoopState.TileAction:
                onTileAction?.Invoke();
                break;
        }
    }

    private void OnDestroy()
    {
        if (PlayerLoopController.Instance != null)
        {
            PlayerLoopController.Instance.OnTurnStarted -= HandleTurnStarted;
            PlayerLoopController.Instance.OnTurnEnded -= HandleTurnEnded;
            PlayerLoopController.Instance.OnLoopCompleted -= HandleLoopCompleted;
            PlayerLoopController.Instance.OnMovementStarted -= HandleMovementStarted;
            PlayerLoopController.Instance.OnMovementCompleted -= HandleMovementCompleted;
            PlayerLoopController.Instance.OnStateChanged -= HandleStateChanged;
        }
    }
}
