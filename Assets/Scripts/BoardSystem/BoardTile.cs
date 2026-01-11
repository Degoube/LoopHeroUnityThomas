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

    private Material tileMaterial;
    private Color originalColor;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<SpriteRenderer>();
        }

        if (meshRenderer != null)
        {
            tileMaterial = meshRenderer.material;
            originalColor = meshRenderer.material.color;
        }
        else if (tileRenderer != null)
        {
            tileMaterial = tileRenderer.material;
            originalColor = tileRenderer.color;
        }

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        InitializeTile();
    }

    public void InitializeTile()
    {
        if (tileData == null)
            return;

        if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            mat.color = tileData.tileColor;
            originalColor = tileData.tileColor;
        }
        else if (tileRenderer != null)
        {
            if (tileData.tileIcon != null)
            {
                tileRenderer.sprite = tileData.tileIcon;
            }
            tileRenderer.color = tileData.tileColor;
            originalColor = tileData.tileColor;
        }
    }

    public void EnterTile(GameObject entity)
    {
        if (!isActive || !tileData.isPassable)
            return;

        isOccupied = true;
        isVisited = true;
        OnTileEntered?.Invoke(this);
    }

    public void ExitTile(GameObject entity)
    {
        isOccupied = false;
        OnTileExited?.Invoke(this);
    }

    public void ActivateTile(GameObject activator)
    {
        if (!isActive)
            return;

        ExecuteTileAction(activator);
        PlayActivationEffect();
        OnTileActivated?.Invoke(this);
    }

    private void ExecuteTileAction(GameObject activator)
    {
        switch (tileData.tileType)
        {
            case TileType.Empty:
                HandleEmptyTile(activator);
                break;
            case TileType.Witness:
                HandleWitnessTile(activator);
                break;
            case TileType.Ruins:
                HandleRuinsTile(activator);
                break;
            case TileType.Combat:
                HandleCombatTile(activator);
                break;
            case TileType.Altar:
                HandleAltarTile(activator);
                break;
            case TileType.Relic:
                HandleRelicTile(activator);
                break;
        }
    }

    private void HandleEmptyTile(GameObject activator)
    {
        Debug.Log($"Stepped on empty tile at {gridPosition}");
    }

    private void HandleWitnessTile(GameObject activator)
    {
        Debug.Log($"Encountered Witness at {gridPosition}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"witness_visited_{gridPosition.x}_{gridPosition.y}");
        }
    }

    private void HandleRuinsTile(GameObject activator)
    {
        Debug.Log($"Exploring Ruins at {gridPosition}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"ruins_explored_{gridPosition.x}_{gridPosition.y}");
        }
    }

    private void HandleCombatTile(GameObject activator)
    {
        Debug.Log($"Combat initiated at {gridPosition}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"combat_started_{gridPosition.x}_{gridPosition.y}");
        }
    }

    private void HandleAltarTile(GameObject activator)
    {
        Debug.Log($"Altar activated at {gridPosition}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"altar_used_{gridPosition.x}_{gridPosition.y}");
        }
    }

    private void HandleRelicTile(GameObject activator)
    {
        Debug.Log($"Relic collected at {gridPosition}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFlag($"relic_obtained_{gridPosition.x}_{gridPosition.y}");
        }

        // La tuile reste active pour permettre le passage - la revisitation est gérée par TileActionHandler
    }

    private void PlayActivationEffect()
    {
        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        if (tileData.actionSound != null)
        {
            AudioSource.PlayClipAtPoint(tileData.actionSound, transform.position);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(highlighted);
        }

        Color highlightColor = highlighted ? Color.Lerp(originalColor, Color.white, 0.5f) : originalColor;
        
        if (meshRenderer != null)
        {
            meshRenderer.material.color = highlightColor;
        }
        else if (tileRenderer != null)
        {
            tileRenderer.color = highlightColor;
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;

        Color targetColor = active ? originalColor : originalColor * 0.5f;
        
        if (meshRenderer != null)
        {
            meshRenderer.material.color = targetColor;
        }
        else if (tileRenderer != null)
        {
            tileRenderer.color = targetColor;
        }
    }

    public bool CanBeEntered()
    {
        if (!isActive || tileData == null || !tileData.isPassable)
            return false;

        return true;
    }

    public void MarkAsVisited()
    {
        isVisited = true;
    }

    public string GetTileInfo()
    {
        return $"{tileData.tileName}\n{tileData.description}\nPosition: {gridPosition}";
    }

    private void OnMouseEnter()
    {
        SetHighlighted(true);
    }

    private void OnMouseExit()
    {
        SetHighlighted(false);
    }

    private void OnMouseDown()
    {
        Debug.Log(GetTileInfo());
    }
}
