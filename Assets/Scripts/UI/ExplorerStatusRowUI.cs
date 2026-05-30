// ExplorerStatusRowUI.cs
// Assets/Scripts/UI/ExplorerStatusRowUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExplorerStatusRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text staminaLabel;
    [SerializeField] private TMP_Text intelLabel;
    [SerializeField] private TMP_Text statsLabel;   // NEW — wire up in Inspector
    [SerializeField] private Image    rowBackground;

    private Explorer _explorer;

    private static readonly Color AliveColor    = new(0.2f, 0.2f, 0.2f);
    private static readonly Color ReturningColor = new(0.15f, 0.35f, 0.5f);
    private static readonly Color CollapseColor = new(0.5f, 0.1f, 0.1f);

    public void Setup(Explorer explorer)
    {
        _explorer = explorer;
        Refresh();
    }

    public void Refresh()
    {
        if (_explorer == null) return;

        nameLabel.text       = _explorer.Name;
        staminaLabel.text = $"{_explorer.Stamina}/{_explorer.MaxStamina} STM";
        intelLabel.text      = $"{_explorer.CarriedIntel} intel";
        rowBackground.color  = _explorer.Status switch
        {
            ExplorerStatus.Dead      => CollapseColor,
            ExplorerStatus.Returning => ReturningColor,
            _                        => AliveColor
        };

        // Stats are static after generation, but displayed here for context
        if (statsLabel != null)
            statsLabel.text =   $"END {_explorer.Endurance} \n" + 
                                $"STR {_explorer.Strength} \n" +
                                $"SPD {_explorer.Speed} \n" +
                                $"LCK {_explorer.Luck}";
    }
}