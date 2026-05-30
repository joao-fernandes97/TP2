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
        dispatchButton.interactable = count > 0;
        dispatchLabel.text = count > 0 ? $"Dispatch ({count})" : "Select Explorers";
    }

    void OnDispatch()
    {
        DayManager.Instance.DispatchSelected();
    }
}