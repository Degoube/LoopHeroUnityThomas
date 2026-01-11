using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResourceUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI resourceText;
    public Image resourceBar;
    public Image warningIndicator;

    [Header("Colors")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;

    [Header("Animation")]
    public float updateSpeed = 5f;
    public bool animateWarning = true;
    public float warningBlinkSpeed = 2f;

    private float currentDisplayValue;
    private float targetFillAmount;
    private float warningTimer;

    private void Start()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesChanged += UpdateResourceDisplay;
            ResourceManager.Instance.OnResourcesCritical += OnCritical;
            ResourceManager.Instance.OnResourcesWarning += OnWarning;

            UpdateResourceDisplay(ResourceManager.Instance.CurrentResources);
        }

        if (warningIndicator != null)
        {
            warningIndicator.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (ResourceManager.Instance == null)
            return;

        currentDisplayValue = Mathf.Lerp(currentDisplayValue, ResourceManager.Instance.CurrentResources, Time.deltaTime * updateSpeed);

        if (resourceBar != null)
        {
            resourceBar.fillAmount = Mathf.Lerp(resourceBar.fillAmount, targetFillAmount, Time.deltaTime * updateSpeed);
        }

        UpdateWarningAnimation();
    }

    private void UpdateResourceDisplay(int resources)
    {
        if (resourceText != null)
        {
            resourceText.text = $"Resources: {resources}";
        }

        if (resourceBar != null && ResourceManager.Instance != null)
        {
            targetFillAmount = (float)resources / ResourceManager.Instance.startingResources;
        }

        UpdateBarColor(resources);
    }

    private void UpdateBarColor(int resources)
    {
        if (resourceBar == null || ResourceManager.Instance == null)
            return;

        if (resources <= ResourceManager.Instance.criticalThreshold)
        {
            resourceBar.color = criticalColor;
        }
        else if (resources <= ResourceManager.Instance.warningThreshold)
        {
            resourceBar.color = warningColor;
        }
        else
        {
            resourceBar.color = normalColor;
        }
    }

    private void OnWarning()
    {
        if (warningIndicator != null)
        {
            warningIndicator.gameObject.SetActive(true);
            warningIndicator.color = warningColor;
        }

        Debug.LogWarning("Resources are running low!");
    }

    private void OnCritical()
    {
        if (warningIndicator != null)
        {
            warningIndicator.gameObject.SetActive(true);
            warningIndicator.color = criticalColor;
        }

        Debug.LogWarning("Resources are critically low!");
    }

    private void UpdateWarningAnimation()
    {
        if (!animateWarning || warningIndicator == null || !warningIndicator.gameObject.activeSelf)
            return;

        warningTimer += Time.deltaTime * warningBlinkSpeed;
        float alpha = Mathf.PingPong(warningTimer, 1f);

        Color color = warningIndicator.color;
        color.a = alpha;
        warningIndicator.color = color;
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnResourcesChanged -= UpdateResourceDisplay;
            ResourceManager.Instance.OnResourcesCritical -= OnCritical;
            ResourceManager.Instance.OnResourcesWarning -= OnWarning;
        }
    }
}
