using System.Collections;
using UnityEngine;
using TMPro;

public class TileNameDisplay : MonoBehaviour
{
    public static TileNameDisplay Instance { get; private set; }
    
    [Header("UI Elements")]
    public TextMeshProUGUI tileNameText;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float displayDuration = 2f;
    
    private Coroutine currentDisplayCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }
    
    public void ShowTileName(string tileName)
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }
        
        currentDisplayCoroutine = StartCoroutine(DisplayTileNameCoroutine(tileName));
    }
    
    private IEnumerator DisplayTileNameCoroutine(string tileName)
    {
        if (tileNameText == null || canvasGroup == null)
            yield break;
        
        tileNameText.text = tileName;
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        yield return new WaitForSeconds(displayDuration);
        
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
