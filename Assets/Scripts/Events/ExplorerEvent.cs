// ExplorerEvent.cs — full replacement
// Assets/Scripts/Events/ExplorerEvent.cs

using System.Collections.Generic;
using UnityEngine;

public enum EventTarget
{
    RandomExplorer,   // System picks one explorer at random (injury, personal encounter)
    PlayerChooses,    // Player picks which explorer responds (investigation, opportunity)
    WholeParty        // All exploring explorers are affected (weather, maze-wide event)
}

[CreateAssetMenu(fileName = "New Explorer Event", menuName = "Maze Runner/Explorer Event")]
public class ExplorerEvent : ScriptableObject
{
    [Header("Flavour")]
    public string Title;

    [TextArea(3, 6)]
    public string Description;

    [Header("Targeting")]
    public EventTarget Targeting = EventTarget.RandomExplorer;

    [Header("Time Cost (0 = no time lost, 1 = full day)")]
    [Range(0f, 0.5f)]
    [Tooltip("How much day progress is consumed by this event, applied after resolution")]
    public float BaseTimeCost = 0.05f;

    [Header("Choices")]
    public List<EventChoice> Choices = new();
}