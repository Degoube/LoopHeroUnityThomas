using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DialogueChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public TextMeshProUGUI choiceText;
    public Image background;

    [Header("Visual Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.5f, 1f);
    public Color pressedColor = new Color(0.1f, 0.1f, 0.3f, 1f);

    [Header("Animation")]
    public float scaleAmount = 1.05f;
    public float animationSpeed = 10f;

    private Button button;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        targetScale = originalScale;

        if (background != null)
        {
            background.color = normalColor;
        }

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void SetChoiceText(string text)
    {
        if (choiceText != null)
        {
            choiceText.text = text;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleAmount;

        if (background != null)
        {
            background.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;

        if (background != null)
        {
            background.color = normalColor;
        }
    }

    private void OnButtonClicked()
    {
        if (background != null)
        {
            background.color = pressedColor;
        }
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
