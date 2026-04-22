using UnityEngine;

/// <summary>
/// Detects mouse/touch swipe input and raycasts against SlashableObjects.
/// Handles both click-to-slash and drag-to-slash (swipe) input modes.
/// </summary>
public class SlashDetector : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Minimum drag distance (in screen pixels) to register as a swipe.")]
    [SerializeField] private float minSwipeDistance = 10f;

    [Tooltip("Raycast distance for detecting objects.")]
    [SerializeField] private float raycastDistance = 100f;

    [Header("Trail Visual")]
    [Tooltip("Optional trail effect following the mouse/touch during swipes.")]
    [SerializeField] private TrailRenderer slashTrail;

    private Camera gameCamera;
    private Vector3 mouseStartPos;
    private bool isDragging;
    private bool isActive;

    /// <summary>
    /// Sets the camera used for screen-to-world raycasts.
    /// </summary>
    public void Initialize(Camera cam)
    {
        gameCamera = cam;
        isActive = true;

        if (slashTrail != null)
            slashTrail.enabled = false;
    }

    /// <summary>
    /// Enables or disables slash input detection.
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active && slashTrail != null)
            slashTrail.enabled = false;
    }

    private void Update()
    {
        if (!isActive || gameCamera == null) return;

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
            isDragging = true;

            if (slashTrail != null)
            {
                slashTrail.enabled = true;
                slashTrail.Clear();
                UpdateTrailPosition();
            }

            // Immediate click-slash
            TrySlashAtScreenPosition(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            // Continuous swipe slash
            float distance = Vector3.Distance(mouseStartPos, Input.mousePosition);
            if (distance >= minSwipeDistance)
            {
                TrySlashAtScreenPosition(Input.mousePosition);
            }

            UpdateTrailPosition();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            if (slashTrail != null)
                slashTrail.enabled = false;
        }
    }

    private void TrySlashAtScreenPosition(Vector3 screenPos)
    {
        Ray ray = gameCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            SlashableObject slashable = hit.collider.GetComponent<SlashableObject>();
            if (slashable != null)
                slashable.Slash();
        }

        // Also check 2D colliders for sprite-based objects
        Vector3 worldPoint = gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        Collider2D hit2D = Physics2D.OverlapPoint(worldPoint);
        if (hit2D != null)
        {
            SlashableObject slashable2D = hit2D.GetComponent<SlashableObject>();
            if (slashable2D != null)
                slashable2D.Slash();
        }
    }

    private void UpdateTrailPosition()
    {
        if (slashTrail == null || gameCamera == null) return;

        Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f));
        slashTrail.transform.position = mouseWorld;
    }
}
