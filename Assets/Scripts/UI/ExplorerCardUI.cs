// ExplorerCardUI.cs
// Assets/Scripts/UI/ExplorerCardUI.cs

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExplorerCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text statsLabel;
    [SerializeField] private Image    cardBackground;
    [SerializeField] private Button   selectButton;

    [Header("Lost State")]
    [Tooltip("Optional overlay image shown on Lost explorer cards (e.g. a semi-transparent amber panel).")]
    [SerializeField] private GameObject lostOverlay;
    [Tooltip("Optional label that displays 'LOST' over the card.")]
    [SerializeField] private TMP_Text   lostLabel;
    
    private Explorer          _explorer;
    private Action<Explorer>  _onToggle;

    private static readonly Color SelectedColor   = new(0.2f, 0.6f, 0.2f);
    private static readonly Color DeselectedColor = new(0.25f, 0.25f, 0.25f);
    private static readonly Color UnavailableColor = new(0.15f, 0.15f, 0.15f);
    // Amber tint for Lost explorers — distinct from InCamp grey and the unavailable dark
    private static readonly Color LostColor         = new(0.55f, 0.30f, 0.05f);
    private static readonly Color LostSelectedColor = new(0.75f, 0.45f, 0.05f);

    // ─── Setup ────────────────────────────────────────────────────────────────

    public void Setup(Explorer explorer, Action<Explorer> onToggle)
    {
        _explorer = explorer;
        _onToggle = onToggle;

        nameLabel.text  = explorer.Name;
        RefreshStatsLabel();

        selectButton.onClick.AddListener(() => _onToggle?.Invoke(_explorer));

        ApplyStatusVisuals();
    }

    public void RefreshSelection()
    {
        ApplyStatusVisuals();
    }

    // ─── Internals ────────────────────────────────────────────────────────────

    private void RefreshStatsLabel()
    {
        statsLabel.text = $"END {_explorer.Endurance}  STR {_explorer.Strength} " +
                          $"SPD {_explorer.Speed}  LCK {_explorer.Luck}";
    }

    private void ApplyStatusVisuals()
    {
        bool isLost    = _explorer.Status == ExplorerStatus.Lost;
        bool isInCamp  = _explorer.Status == ExplorerStatus.InCamp;
        bool isSelected = DayManager.Instance.IsSelected(_explorer);

        // Button interactable for InCamp and Lost; disabled for Exploring / Dead
        selectButton.interactable = isInCamp || isLost;

        // Background colour
        if (isLost)
        {
            cardBackground.color = isSelected ? LostSelectedColor : LostColor;
        }
        else if (isInCamp)
        {
            cardBackground.color = isSelected ? SelectedColor : DeselectedColor;
        }
        else
        {
            cardBackground.color = UnavailableColor;
        }

        // Lost overlay / label — show only when the explorer is Lost
        if (lostOverlay != null) lostOverlay.SetActive(isLost);
        if (lostLabel   != null)
        {
            lostLabel.gameObject.SetActive(isLost);
            if (isLost) lostLabel.text = "LOST";
        }

        // Keep stats label current (stats can change after a rescue penalty)
        RefreshStatsLabel();
    }
}