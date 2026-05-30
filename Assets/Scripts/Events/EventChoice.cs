// EventChoice.cs — full replacement
// Assets/Scripts/Events/EventChoice.cs

using System;
using UnityEngine;

[Serializable]
public class EventChoice
{
    [TextArea(1, 2)]
    public string Label;

    [TextArea(2, 4)]
    public string VagueHint;

    public StatType StatCheck;
    public bool     RequiresCheck;

    [Header("Time Cost Modifier (added on top of event BaseTimeCost)")]
    [Range(-0.2f, 0.3f)]
    [Tooltip("Negative = faster, Positive = slower. E.g. 0.15 means this choice burns extra time.")]
    public float ExtraTimeCostOnSuccess = 0f;
    public float ExtraTimeCostOnFail    = 0f;

    [Header("Success Outcome")]
    public int   IntelGain;
    public int   StaminaCost;
    [TextArea(1, 3)]
    public string SuccessMessage;

    [Header("Failure Outcome")]
    public int   IntelGainOnFail;
    public int   StaminaCostOnFail;
    [TextArea(1, 3)]
    public string FailureMessage;
}