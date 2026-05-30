// EventPanelUI.cs
// Assets/Scripts/UI/EventPanelUI.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text   titleLabel;
    [SerializeField] private TMP_Text   descriptionLabel;
    [SerializeField] private TMP_Text   targetLabel;           // NEW — wire up in Inspector
    [SerializeField] private Transform  targetPickerContainer;
    [SerializeField] private GameObject explorerPickButtonPrefab;
    [SerializeField] private Transform  choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    void Awake()
    {
        if (EventManager.Instance == null)
        {
            Debug.LogError("EventPanelUI: EventManager.Instance is null in Awake!");
            return;
        }

        EventManager.Instance.OnEventTriggered += OnEventTriggered;
        Debug.Log("EventPanelUI: Subscribed to OnEventTriggered.");
    }

    void OnEventTriggered(ExplorerEvent ev, Explorer subject, List<Explorer> party)
    {
        titleLabel.text       = ev.Title;
        descriptionLabel.text = ev.Description;

        UpdateTargetLabel(ev, subject, party);
        BuildTargetPicker(ev, party);
        BuildChoices(ev);
    }

    void UpdateTargetLabel(ExplorerEvent ev, Explorer subject, List<Explorer> party)
    {
        if (targetLabel == null) return;

        targetLabel.text = ev.Targeting switch
        {
            EventTarget.RandomExplorer => $"Affecting: {subject.Name}",
            EventTarget.WholeParty     => "Affecting: Whole Party",
            EventTarget.PlayerChooses  => "Affecting: —",   // updated once player picks
            _                          => ""
        };
    }

    void BuildTargetPicker(ExplorerEvent ev, List<Explorer> party)
    {
        // Clear old buttons
        foreach (Transform child in targetPickerContainer)
            Destroy(child.gameObject);

        bool needsPicker = ev.Targeting == EventTarget.PlayerChooses;
        targetPickerContainer.gameObject.SetActive(needsPicker);

        if (!needsPicker) return;

        var exploring = GameManager.Instance.GetExplorersByStatus(ExplorerStatus.Exploring);
        foreach (var explorer in exploring)
        {
            var go     = Instantiate(explorerPickButtonPrefab, targetPickerContainer);
            var button = go.GetComponentInChildren<Button>();
            var label  = go.GetComponentInChildren<TMP_Text>();

            label.text = $"{explorer.Name}\nSTR:{explorer.Strength} SPD:{explorer.Speed}";

            // Capture for lambda
            var captured = explorer;
            button.onClick.AddListener(() =>
            {
                EventManager.Instance.AssignExplorer(captured);

                // Update the target label to show who was chosen
                if (targetLabel != null)
                    targetLabel.text = $"Affecting: {captured.Name}";

                // Disable all pick buttons after selection
                foreach (Transform child in targetPickerContainer)
                    child.GetComponentInChildren<Button>().interactable = false;
            });
        }
    }

    void BuildChoices(ExplorerEvent ev)
    {
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < ev.Choices.Count; i++)
        {
            var go     = Instantiate(choiceButtonPrefab, choicesContainer);
            var button = go.GetComponentInChildren<Button>();
            var label  = go.GetComponentInChildren<TMP_Text>();
            var choice = ev.Choices[i];

            label.text = $"{choice.Label}\n<size=70%><i>{choice.VagueHint}</i></size>";

            var capturedIndex = i;
            button.onClick.AddListener(() =>
                EventManager.Instance.ResolveChoice(capturedIndex));
        }
    }
}