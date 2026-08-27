using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for hover events

[RequireComponent(typeof(Button))]
public class AbilityCard : MonoBehaviour, IPointerEnterHandler
{
    [Header("Card Data")]
    [Tooltip("Must be exactly 'Jump', 'Dash', or 'WallClimb'")]
    public string abilityName; //
    public Image cardVisual; //[cite: 1]
    
    [Header("Visual Feedback (Sacrificed State)")]
    public Color unselectedColor = Color.white; //[cite: 1]
    public Color selectedColor = new Color(0.8f, 0.2f, 0.2f); //[cite: 1] Reddish tint to indicate loss
    public Vector3 selectedScale = new Vector3(0.95f, 0.95f, 0.95f); //[cite: 1] Shrink slightly

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSFX;
    [SerializeField] private AudioClip selectSFX;
    
    private bool isSelected = false; //[cite: 1]
    private Button button; //[cite: 1]
    private AbilitySelectionManager manager; //[cite: 1]
    private Vector3 originalScale; //[cite: 1]
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>(); //[cite: 1]
        originalScale = transform.localScale; //[cite: 1]
        button.onClick.AddListener(OnCardClicked); //[cite: 1]

        // Ensure and configure a dedicated AudioSource on this GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Recommended default settings for UI SFX
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for UI
    }

    public void Setup(AbilitySelectionManager menuManager)
    {
        manager = menuManager; //[cite: 1]
        UpdateVisuals(); //[cite: 1]
    }

    private void OnCardClicked()
    {
        PlaySound(selectSFX);
        manager.HandleCardClick(this); //[cite: 1]
    }

    // Triggered automatically when the mouse pointer enters the UI element area
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSFX);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected; //[cite: 1]
        UpdateVisuals(); //[cite: 1]
    }

    public bool IsSelected()
    {
        return isSelected; //[cite: 1]
    }

    private void UpdateVisuals()
    {
        if (cardVisual != null) cardVisual.color = isSelected ? selectedColor : unselectedColor; //[cite: 1]
        transform.localScale = isSelected ? selectedScale : originalScale; //[cite: 1]
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}