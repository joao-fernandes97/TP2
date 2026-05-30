// EventManager.cs — full replacement
// Assets/Scripts/Events/EventManager.cs

using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [SerializeField] private List<ExplorerEvent> eventPool = new();

    // ─── Active event state ───────────────────────────────────────────────────
    public ExplorerEvent    ActiveEvent      { get; private set; }
    public EventTarget      ActiveTargeting  { get; private set; }

    // For RandomExplorer and PlayerChooses — the single subject
    public Explorer         ActiveExplorer   { get; private set; }

    // For WholeParty — all affected explorers
    public List<Explorer>   ActiveParty      { get; private set; } = new();

    // For PlayerChooses — waiting on UI to call AssignExplorer()
    public bool             AwaitingTarget   { get; private set; } = false;

    // ─── Events ───────────────────────────────────────────────────────────────

    // (event, primary explorer or null if WholeParty, party list or empty)
    public event System.Action<ExplorerEvent, Explorer, List<Explorer>> OnEventTriggered;
    public event System.Action<string>                                  OnEventResolved;

    // ─── Trigger ──────────────────────────────────────────────────────────────

    public void TriggerRandomEvent(List<Explorer> activeExplorers)
    {
        if (eventPool.Count == 0 || ActiveEvent != null) return;

        ActiveEvent     = eventPool[Random.Range(0, eventPool.Count)];
        ActiveTargeting = ActiveEvent.Targeting;
        ActiveParty.Clear();

        switch (ActiveTargeting)
        {
            case EventTarget.RandomExplorer:
                ActiveExplorer  = activeExplorers[Random.Range(0, activeExplorers.Count)];
                AwaitingTarget  = false;
                break;

            case EventTarget.PlayerChooses:
                ActiveExplorer  = null;   // player must call AssignExplorer()
                AwaitingTarget  = true;
                break;

            case EventTarget.WholeParty:
                ActiveExplorer  = null;
                ActiveParty.AddRange(activeExplorers);
                AwaitingTarget  = false;
                break;
        }

        Debug.Log($"[EVENT] '{ActiveEvent.Title}' | Target: {ActiveTargeting}");
        OnEventTriggered?.Invoke(ActiveEvent, ActiveExplorer, ActiveParty);

        // Auto-resolve for testing if no UI is connected
        if (OnEventTriggered == null || OnEventTriggered.GetInvocationList().Length == 0)
        {
            if (ActiveTargeting == EventTarget.PlayerChooses)
                AssignExplorer(activeExplorers[0]);
            ResolveChoice(0);
        }
    }

    /// <summary>
    /// For PlayerChooses events — UI calls this when the player taps an explorer.
    /// </summary>
    public void AssignExplorer(Explorer explorer)
    {
        if (!AwaitingTarget || ActiveEvent == null) return;
        ActiveExplorer = explorer;
        AwaitingTarget = false;
        Debug.Log($"[EVENT] {explorer.Name} assigned to '{ActiveEvent.Title}'");
    }

    // ─── Resolve ──────────────────────────────────────────────────────────────

    public void ResolveChoice(int choiceIndex)
    {
        if (ActiveEvent == null)                                    return;
        if (AwaitingTarget)                                         return; // still waiting for player to assign
        if (choiceIndex < 0 || choiceIndex >= ActiveEvent.Choices.Count) return;

        var choice = ActiveEvent.Choices[choiceIndex];
        float timeCost = ActiveEvent.BaseTimeCost;

        string outcomeLog = "";

        if (ActiveTargeting == EventTarget.WholeParty)
            outcomeLog = ResolveForParty(choice, ref timeCost);
        else
            outcomeLog = ResolveForExplorer(ActiveExplorer, choice, ref timeCost);

        // Apply time cost to the day
        if (timeCost > 0f)
        {
            DayManager.Instance?.ConsumeTime(timeCost);
            Debug.Log($"[TIME] Event consumed {timeCost:P0} of the day.");
        }

        OnEventResolved?.Invoke(outcomeLog);

        ActiveEvent    = null;
        ActiveExplorer = null;
        ActiveParty.Clear();
        AwaitingTarget = false;
    }

    // ─── Resolution helpers ───────────────────────────────────────────────────

    private string ResolveForExplorer(Explorer explorer, EventChoice choice, ref float timeCost)
    {
        bool success = RollChoice(choice, explorer);
        ApplyOutcome(explorer, choice, success, out string msg, out float extraTime);
        timeCost += extraTime;
        return $"{explorer.Name}: {msg}";
    }

    private string ResolveForParty(EventChoice choice, ref float timeCost)
    {
        var lines = new System.Text.StringBuilder();
        float extraTimeTotal = 0f;

        foreach (var explorer in ActiveParty)
        {
            bool success = RollChoice(choice, explorer);
            ApplyOutcome(explorer, choice, success, out string msg, out float extraTime);
            extraTimeTotal += extraTime;
            lines.AppendLine($"  {explorer.Name}: {msg}");
        }

        // Average extra time across party so party events don't warp the day too hard
        timeCost += ActiveParty.Count > 0 ? extraTimeTotal / ActiveParty.Count : 0f;
        return lines.ToString();
    }

    private void ApplyOutcome(Explorer explorer, EventChoice choice, bool success,
                              out string message, out float extraTimeCost)
    {
        if (success)
        {
            explorer.CarriedIntel += choice.IntelGain;
            explorer.Stamina      -= choice.StaminaCost;
            extraTimeCost          = choice.ExtraTimeCostOnSuccess;
            message = $"✅ {choice.SuccessMessage} (+{choice.IntelGain} intel, -{choice.StaminaCost} stamina)";
        }
        else
        {
            explorer.CarriedIntel += choice.IntelGainOnFail;
            explorer.Stamina      -= choice.StaminaCostOnFail;
            extraTimeCost          = choice.ExtraTimeCostOnFail;
            message = $"❌ {choice.FailureMessage} (+{choice.IntelGainOnFail} intel, -{choice.StaminaCostOnFail} stamina)";
        }

        Debug.Log($"[RESULT] {message} | {explorer.Name} stamina: {explorer.Stamina}/{explorer.MaxStamina}");

        if (explorer.Stamina <= 0)
        {
            explorer.Stamina = 0;
            Debug.Log($"💀 {explorer.Name} has collapsed!");
            GameManager.Instance.SetExplorerStatus(explorer, ExplorerStatus.Dead);
        }

        GameManager.Instance.NotifyExplorerStatsChanged(explorer);
    }

    private bool RollChoice(EventChoice choice, Explorer explorer)
    {
        if (!choice.RequiresCheck) return Random.value < 0.75f;

        int stat = choice.StatCheck switch
        {
            StatType.Endurance => explorer.Endurance,
            StatType.Strength  => explorer.Strength,
            StatType.Speed     => explorer.Speed,
            StatType.Luck      => explorer.Luck,
            _                  => 5
        };
        return explorer.RollStatCheck(stat);
    }
}