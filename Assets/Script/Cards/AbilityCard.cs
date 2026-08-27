using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AbilityCard : MonoBehaviour
{
    [Header("Card Data")]
    [Tooltip("Must be exactly 'Jump', 'Dash', or 'WallClimb'")]
    public string abilityName;
    public Image cardVisual;
    
    [Header("Visual Feedback (Sacrificed State)")]
    public Color unselectedColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.2f, 0.2f); // Reddish tint to indicate loss
    public Vector3 selectedScale = new Vector3(0.95f, 0.95f, 0.95f); // Shrink slightly
    
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
        manager.HandleCardClick(this);
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
}