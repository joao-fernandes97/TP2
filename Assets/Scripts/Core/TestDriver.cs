// TestDriver.cs — full replacement

using System.Collections.Generic;
using UnityEngine;

public class TestDriver : MonoBehaviour
{
    private ExplorerEvent _pendingEvent;
    private bool          _awaitingExplorerAssign;

    void Start()
    {
        GameManager.Instance.OnIntelChanged          += i   => Debug.Log($"📡 Intel: {i}");
        GameManager.Instance.OnDayChanged            += d   => Debug.Log($"📅 Day: {d}");
        GameManager.Instance.OnExplorerStatusChanged += e   => Debug.Log($"👤 {e.Name} → {e.Status}");
        GameManager.Instance.OnGameOver              += won => Debug.Log(won ? "🏆 WIN" : "💀 LOSS");

        DayManager.Instance.OnBriefingStarted += PrintRoster;
        DayManager.Instance.OnRecallWarning   += () => Debug.Log("⚠️  RECALL WARNING — press R!");
        DayManager.Instance.OnNightfall       += () => Debug.Log("🌑 NIGHTFALL");

        EventManager.Instance.OnEventTriggered += HandleEventTriggered;
        EventManager.Instance.OnEventResolved  += msg => Debug.Log($"  → {msg}");
    }

    void HandleEventTriggered(ExplorerEvent ev, Explorer subject, List<Explorer> party)
    {
        _pendingEvent = ev;

        Debug.Log($"\n━━━ EVENT: {ev.Title} [{ev.Targeting}] ━━━");
        Debug.Log($"  {ev.Description}");

        if (ev.Targeting == EventTarget.WholeParty)
            Debug.Log($"  Affects entire party ({party.Count} explorers)");
        else if (ev.Targeting == EventTarget.RandomExplorer)
            Debug.Log($"  Affects: {subject.Name}");
        else if (ev.Targeting == EventTarget.PlayerChooses)
        {
            _awaitingExplorerAssign = true;
            var exploring = GameManager.Instance.GetExplorersByStatus(ExplorerStatus.Exploring);
            Debug.Log("  Choose who investigates:");
            for (int i = 0; i < exploring.Count; i++)
                Debug.Log($"    [{i + 1}] {exploring[i].Name}");
            return; // Don't print choices yet — wait for explorer assignment
        }

        PrintChoices(ev);
    }

    void PrintChoices(ExplorerEvent ev)
    {
        Debug.Log("  Choices:");
        for (int i = 0; i < ev.Choices.Count; i++)
            Debug.Log($"    [{i + 1}] {ev.Choices[i].Label} — {ev.Choices[i].VagueHint}");
    }

    void Update()
    {
        // Briefing phase
        if (DayManager.Instance.InBriefing)
        {
            var all = GameManager.Instance.AllExplorers;
            for (int i = 0; i < all.Count && i < 4; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    DayManager.Instance.ToggleExplorerSelection(all[i]);

            if (Input.GetKeyDown(KeyCode.Return))
                DayManager.Instance.DispatchSelected();

            return;
        }

        // Assign explorer for PlayerChooses events
        if (_awaitingExplorerAssign)
        {
            var exploring = GameManager.Instance.GetExplorersByStatus(ExplorerStatus.Exploring);
            for (int i = 0; i < exploring.Count && i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    EventManager.Instance.AssignExplorer(exploring[i]);
                    _awaitingExplorerAssign = false;
                    PrintChoices(_pendingEvent);
                }
            }
            return; // Block choice input until explorer is assigned
        }

        // Resolve event choice
        if (_pendingEvent != null)
        {
            for (int i = 0; i < _pendingEvent.Choices.Count && i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    EventManager.Instance.ResolveChoice(i);
                    _pendingEvent = null;
                }
            }
            return; // Block recall while resolving
        }

        // Recall
        if (DayManager.Instance.DayRunning && Input.GetKeyDown(KeyCode.R))
            DayManager.Instance.IssueRecall();
    }

    void PrintRoster()
    {
        Debug.Log("=== BRIEFING — Press 1-4 to toggle, Enter to dispatch ===");
        var explorers = GameManager.Instance.AllExplorers;
        for (int i = 0; i < explorers.Count; i++)
            Debug.Log($"  [{i + 1}] {explorers[i].Name} | END:{explorers[i].Endurance} STR:{explorers[i].Strength} SPD:{explorers[i].Speed} LCK:{explorers[i].Luck}");
    }
}