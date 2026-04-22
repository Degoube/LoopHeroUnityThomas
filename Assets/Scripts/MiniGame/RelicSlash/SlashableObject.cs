using System;
using UnityEngine;

/// <summary>
/// Represents a single flying object that can be slashed by the player.
/// Handles arc physics, slash detection via mouse/touch, and destruction.
/// </summary>
public class SlashableObject : MonoBehaviour
{
    /// <summary>Fired when this object is slashed. Args: (this, isTrap).</summary>
    public event Action<SlashableObject, bool> OnSlashed;

    /// <summary>Fired when this object falls below the destroy threshold without being slashed.</summary>
    public event Action<SlashableObject> OnMissed;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private MeshRenderer meshRenderer;

    /// <summary>True if this object is a trap/bomb.</summary>
    public bool IsTrap { get; private set; }

    /// <summary>Bonus points awarded for slashing this object.</summary>
    public int BonusPoints { get; private set; }

    private Vector3 velocity;
    private float gravity;
    private float destroyBelowY;
    private bool isSlashed;
    private Camera gameCamera;

    /// <summary>
    /// Initializes the object with launch parameters.
    /// </summary>
    public void Initialize(Vector3 launchVelocity, float gravityValue, float destroyY,
        bool isTrap, Color color, float scale, int bonusPoints, Camera cam)
    {
        velocity = launchVelocity;
        gravity = gravityValue;
        destroyBelowY = destroyY;
        IsTrap = isTrap;
        BonusPoints = bonusPoints;
        isSlashed = false;
        gameCamera = cam;

        transform.localScale = Vector3.one * scale;

        // Apply color
        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            mat.color = color;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    private void Update()
    {
        if (isSlashed) return;

        // Arc physics
        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        // Spin for visual flair
        transform.Rotate(Vector3.forward, 200f * Time.deltaTime);

        // Destroy if below threshold
        if (transform.position.y < destroyBelowY)
        {
            if (!IsTrap)
                OnMissed?.Invoke(this);

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the player slashes this object (via SlashDetector).
    /// </summary>
    public void Slash()
    {
        if (isSlashed) return;

        isSlashed = true;
        OnSlashed?.Invoke(this, IsTrap);

        // Visual feedback: scale down and fade
        StartCoroutine(DestroyAnimation());
    }

    private System.Collections.IEnumerator DestroyAnimation()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.5f, t);

            // Fade
            if (meshRenderer != null)
            {
                Color c = meshRenderer.material.color;
                c.a = 1f - t;
                meshRenderer.material.color = c;
            }
            else if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - t;
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnMouseDown()
    {
        Slash();
    }
}
