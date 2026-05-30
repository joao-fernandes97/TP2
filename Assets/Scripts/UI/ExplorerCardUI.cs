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

    private Explorer          _explorer;
    private Action<Explorer>  _onToggle;

    private static readonly Color SelectedColor   = new(0.2f, 0.6f, 0.2f);
    private static readonly Color DeselectedColor = new(0.25f, 0.25f, 0.25f);
    private static readonly Color UnavailableColor = new(0.15f, 0.15f, 0.15f);

    public void Setup(Explorer explorer, Action<Explorer> onToggle)
    {
        _explorer = explorer;
        _onToggle = onToggle;

        nameLabel.text  = explorer.Name;
        statsLabel.text = $"END {explorer.Endurance}  STR {explorer.Strength} " +
                          $"SPD {explorer.Speed}  LCK {explorer.Luck}";

        selectButton.onClick.AddListener(() => _onToggle?.Invoke(_explorer));

        bool available = explorer.Status == ExplorerStatus.InCamp;
        selectButton.interactable = available;
        cardBackground.color      = available ? DeselectedColor : UnavailableColor;
    }

    public void RefreshSelection()
    {
        if (_explorer.Status != ExplorerStatus.InCamp) return;
        bool selected = DayManager.Instance.IsSelected(_explorer);
        cardBackground.color = selected ? SelectedColor : DeselectedColor;
    }
}