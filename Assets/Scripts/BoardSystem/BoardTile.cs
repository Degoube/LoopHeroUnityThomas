using System;
using UnityEngine;

public class BoardTile : MonoBehaviour
{
    [Header("Tile Configuration")]
    public TileData tileData;
    public Vector2Int gridPosition;
    public int pathIndex;

    [Header("State")]
    public bool isVisited;
    public bool isOccupied;
    public bool isActive = true;

    [Header("Visual Components")]
    public SpriteRenderer tileRenderer;
    public MeshRenderer meshRenderer;
    public GameObject selectionIndicator;
    public ParticleSystem activationEffect;

    public event Action<BoardTile> OnTileEntered;
    public event Action<BoardTile> OnTileExited;
    public event Action<BoardTile> OnTileActivated;

    public TileData TileData => tileData;
    public bool HasBeenVisited => isVisited;

    // Cached material instance — prevents per-call allocation caused by accessing .material
    private Material cachedMaterial;
    private Color originalColor;
    private bool usesMeshRenderer;

    private void Awake()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (tileRenderer == null)
            tileRenderer = GetComponent<SpriteRenderer>();

        if (meshRenderer != null)
        {
            usesMeshRenderer = true;
            cachedMaterial = meshRenderer.material;
            originalColor = cachedMaterial.color;
        }
        else if (tileRenderer != null)
        {
            usesMeshRenderer = false;
            cachedMaterial = tileRenderer.material;
            originalColor = tileRenderer.color;
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    private void Start()
    {
        InitializeTile();
    }

    /// <summary>
    /// Applies visual data from tileData (color, sprite). Called by BoardManager after assigning tileData.
    /// </summary>
    public void InitializeTile()
    {
        if (tileData == null)
            return;

        originalColor = tileData.tileColor;

        if (usesMeshRenderer && cachedMaterial != null)
        {
            cachedMaterial.color = tileData.tileColor;
        }
        else if (!usesMeshRenderer && tileRenderer != null)
        {
            if (tileData.tileIcon != null)
                tileRenderer.sprite = tileData.tileIcon;

            tileRenderer.color = tileData.tileColor;
        }
    }

    /// <summary>Marks the tile as occupied and visited, fires OnTileEntered.</summary>
    public void EnterTile(GameObject entity)
    {
        if (!isActive || tileData == null || !tileData.isPassable)
            return;

        isOccupied = true;
        isVisited = true;
        OnTileEntered?.Invoke(this);
    }

    /// <summary>Clears occupation, fires OnTileExited.</summary>
    public void ExitTile(GameObject entity)
    {
        isOccupied = false;
        OnTileExited?.Invoke(this);
    }

    /// <summary>
    /// Fires OnTileActivated and plays the activation effect.
    /// Tile-type logic is handled externally by TileActionHandler.
    /// </summary>
    public void ActivateTile(GameObject activator)
    {
        if (!isActive)
            return;

        PlayActivationEffect();
        OnTileActivated?.Invoke(this);
    }

    private void PlayActivationEffect()
    {
        if (activationEffect != null)
            activationEffect.Play();

        if (tileData != null && tileData.actionSound != null)
            AudioSource.PlayClipAtPoint(tileData.actionSound, transform.position);
    }

    /// <summary>Highlights or un-highlights the tile visually.</summary>
    public void SetHighlighted(bool highlighted)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(highlighted);

        ApplyColor(highlighted ? Color.Lerp(originalColor, Color.white, 0.5f) : originalColor);
    }

    /// <summary>Dims the tile when inactive.</summary>
    public void SetTileActive(bool active)
    {
        isActive = active;
        ApplyColor(active ? originalColor : originalColor * 0.5f);
    }

    /// <summary>Returns true if the tile can be stepped on.</summary>
    public bool CanBeEntered()
    {
        return isActive && tileData != null && tileData.isPassable;
    }

    public void MarkAsVisited()
    {
        isVisited = true;
    }

    /// <summary>Returns a human-readable summary of this tile.</summary>
    public string GetTileInfo()
    {
        if (tileData == null)
            return $"No tile data at {gridPosition}";

        return $"{tileData.tileName}\n{tileData.description}\nPosition: {gridPosition}";
    }

    private void ApplyColor(Color color)
    {
        if (usesMeshRenderer && cachedMaterial != null)
            cachedMaterial.color = color;
        else if (!usesMeshRenderer && tileRenderer != null)
            tileRenderer.color = color;
    }

    private void OnMouseEnter() => SetHighlighted(true);
    private void OnMouseExit()  => SetHighlighted(false);
    private void OnMouseDown()  => Debug.Log(GetTileInfo());
}
