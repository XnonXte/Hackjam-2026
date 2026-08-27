using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class AbilityCard : MonoBehaviour, IPointerEnterHandler
{
    [Header("Card Data")]
    [Tooltip("Must be exactly 'Jump', 'Dash', or 'WallClimb'")]
    public string abilityName;
    public Image cardVisual;

    [Header("Visual Feedback (Sacrificed State)")]
    public Color unselectedColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.2f, 0.2f); // Reddish tint to indicate loss
    public Vector3 selectedScale = new Vector3(0.95f, 0.95f, 0.95f); // Shrink slightly

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip selectSFX;

    private bool isSelected = false;
    private Button button;
    private AbilitySelectionManager manager;
    private Vector3 originalScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
        button.onClick.AddListener(OnCardClicked);
    }

    public void Setup(AbilitySelectionManager menuManager)
    {
        manager = menuManager;
        UpdateVisuals();
    }

    private void OnCardClicked()
    {
        PlaySound(selectSFX);
        manager.HandleCardClick(this);
    }

    // Triggered automatically when the mouse pointer enters the UI element area
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSFX);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    private void UpdateVisuals()
    {
        if (cardVisual != null) cardVisual.color = isSelected ? selectedColor : unselectedColor;
        transform.localScale = isSelected ? selectedScale : originalScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}