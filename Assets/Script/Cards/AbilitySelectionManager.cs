using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AbilitySelectionManager : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("How many abilities MUST the player sacrifice in this level?")]
    public int requiredSacrifices = 1;

    [Header("References")]
    public PlayerController player;
    public GameObject selectionMenuPanel;
    public AbilityCard[] allCards;

    [Header("UI Text")]
    public TextMeshProUGUI instructionText;

    private List<AbilityCard> sacrificedCards = new List<AbilityCard>();
    private bool isStartingLevel = false;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        // LOCK PLAYER INPUT WHEN MENU OPENS
        if (player != null) player.isInputLocked = true;

        foreach (AbilityCard card in allCards)
        {
            card.Setup(this);
        }

        UpdateInstructionText();
    }

    public void HandleCardClick(AbilityCard clickedCard)
    {
        if (isStartingLevel) return;

        // CEGAH KLIK JIKA SEDANG PAUSE
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        if (clickedCard.IsSelected())
        {
            clickedCard.SetSelected(false);
            sacrificedCards.Remove(clickedCard);
        }
        else
        {
            clickedCard.SetSelected(true);
            sacrificedCards.Add(clickedCard);

            if (sacrificedCards.Count == requiredSacrifices)
            {
                StartCoroutine(ApplySacrificesAndStart());
            }
        }

        UpdateInstructionText();
    }

    private void UpdateInstructionText()
    {
        if (instructionText != null)
        {
            int remaining = requiredSacrifices - sacrificedCards.Count;
            if (remaining > 0)
            {
                // Corrected spelling of abilities
                instructionText.text = $"Sacrifice {remaining} more abilit{(remaining > 1 ? "ies" : "y")}!";
            }
            else
            {
                instructionText.text = "";
            }
        }
    }

    private IEnumerator ApplySacrificesAndStart()
    {
        isStartingLevel = true;
        UpdateInstructionText();

        yield return new WaitForSeconds(0.3f);

        List<string> chosenSacrifices = new List<string>();
        foreach (AbilityCard card in sacrificedCards)
        {
            chosenSacrifices.Add(card.abilityName);
        }

        if (player != null)
        {
            player.ApplySacrifices(chosenSacrifices);
            player.isInputLocked = false;
        }

        if (selectionMenuPanel != null)
        {
            selectionMenuPanel.SetActive(false);
            GameUI.Instance.ShowGamePanel();
        }
    }
}