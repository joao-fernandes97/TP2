// BriefingPanelUI.cs
// Assets/Scripts/UI/BriefingPanelUI.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BriefingPanelUI : MonoBehaviour
{
    [SerializeField] private Transform   explorerGrid;
    [SerializeField] private GameObject  explorerCardPrefab;
    [SerializeField] private Button      dispatchButton;
    [SerializeField] private TMP_Text    dispatchLabel;

    [Header("Dispatch Hint")]
    [Tooltip("Small text beneath the dispatch button — used to explain invalid states.")]
    [SerializeField] private TMP_Text    dispatchHint;

    private readonly List<ExplorerCardUI> _cards = new();

    void Awake()
    {
        DayManager.Instance.OnBriefingStarted += RefreshRoster;
    }
    
    void Start()
    {
        dispatchButton.onClick.AddListener(OnDispatch);
        dispatchButton.interactable = false;
    }

    void RefreshRoster()
    {
        // Clear old cards
        foreach (var c in _cards) Destroy(c.gameObject);
        _cards.Clear();

        foreach (var explorer in GameManager.Instance.AllExplorers)
        {
            if (explorer.Status == ExplorerStatus.Dead) continue;

            var go   = Instantiate(explorerCardPrefab, explorerGrid);
            var card = go.GetComponent<ExplorerCardUI>();
            card.Setup(explorer, OnCardToggled);
            _cards.Add(card);
        }

        UpdateDispatchButton();
    }

    void OnCardToggled(Explorer explorer)
    {
        DayManager.Instance.ToggleExplorerSelection(explorer);
        UpdateDispatchButton();

        // Refresh all card visuals to reflect selection state
        foreach (var c in _cards)
            c.RefreshSelection();
    }

    void UpdateDispatchButton()
    {
        int count = DayManager.Instance.SelectedCount;

        if (count == 0)
        {
            dispatchButton.interactable = false;
            dispatchLabel.text          = "Select Explorers";
            SetHint("");
            return;
        }

        if (DayManager.Instance.IsMixedSelection)
        {
            dispatchButton.interactable = false;
            dispatchLabel.text          = "Invalid Selection";
            SetHint("A rescue mission must be solo —\ndeselect other explorers first.");
            return;
        }

        if (DayManager.Instance.IsRescueMission)
        {
            dispatchButton.interactable = true;
            // Find the lost explorer's name for the label
            string lostName = GetSingleSelectedName();
            dispatchLabel.text = $"Rescue {lostName}";
            SetHint("The whole day will be spent on the rescue.\nThe explorer will return weakened (−1 all stats).");
            return;
        }

        // Normal dispatch
        dispatchButton.interactable = true;
        dispatchLabel.text          = $"Dispatch ({count})";
        SetHint("");
    }

    void SetHint(string text)
    {
        if (dispatchHint == null) return;
        dispatchHint.text = text;
        dispatchHint.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    string GetSingleSelectedName()
    {
        // Walk the card list — only one explorer is selected at this point
        foreach (var card in _cards)
        {
            // We ask DayManager rather than peek at card internals
        }
        // Fall back: ask GameManager for Lost explorers and match
        foreach (var explorer in GameManager.Instance.AllExplorers)
            if (DayManager.Instance.IsSelected(explorer))
                return explorer.Name;
        return "Explorer";
    }

    void OnDispatch()
    {
        DayManager.Instance.DispatchSelected();
    }
}