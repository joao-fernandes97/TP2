// DayManager.cs — full replacement

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Day Timing")]
    [SerializeField] private float dayDurationSeconds     = 60f;
    [SerializeField] private float recallWarningThreshold = 0.75f;

    [Header("Return Journey")]
    [Tooltip("How many times faster explorers retrace the known path vs exploring fresh ground. " +
             "2 = half the real-time to return the same distance.")]
    [SerializeField] private float returnSpeedMultiplier = 2f;

    [Header("Event Timing")]
    [SerializeField] private float minTimeBetweenEvents = 0.10f;
    [SerializeField] private float eventChancePerTick   = 0.02f;

    public float DayProgress  { get; private set; } = 0f;
    public float PartyDepth   { get; private set; } = 0f;
    public float MaxDepthMeters  { get; private set; } = 0f;
    public float PartyDepthMeters => PartyDepth * MaxDepthMeters;
    public bool  DayRunning   { get; private set; } = false;
    public bool  InBriefing   { get; private set; } = false;
    public bool  RecallIssued { get; private set; } = false;
    public bool  IsReturning  { get; private set; } = false;

    // Explorers the player has toggled for today's run
    private readonly HashSet<Explorer> _selectedForDispatch = new();

    private float     _lastEventTime;
    public bool IsPaused => _eventInProgress;
    private bool _eventInProgress;
    private Coroutine _dayCoroutine;

    // ─── Selection analysis (read by BriefingPanelUI) ────────────────────────

    /// <summary>True when the only selected explorer is a single Lost explorer.</summary>
    public bool IsRescueMission
    {
        get
        {
            if (_selectedForDispatch.Count != 1) return false;
            foreach (var e in _selectedForDispatch)
                return e.Status == ExplorerStatus.Lost;
            return false;
        }
    }

    /// <summary>True when the selection mixes Lost and InCamp explorers (invalid).</summary>
    public bool IsMixedSelection
    {
        get
        {
            if (_selectedForDispatch.Count < 2) return false;
            bool hasLost   = false;
            bool hasInCamp = false;
            foreach (var e in _selectedForDispatch)
            {
                if (e.Status == ExplorerStatus.Lost)   hasLost   = true;
                if (e.Status == ExplorerStatus.InCamp) hasInCamp = true;
            }
            return hasLost && hasInCamp;
        }
    }
    
    // Events
    public event System.Action<float>    OnDayProgressUpdated;
    public event System.Action           OnBriefingStarted;   // UI: show selection screen
    public event System.Action           OnDispatchStarted;   // UI: hide selection, show maze
    public event System.Action           OnRecallWarning;
    public event System.Action           OnReturnStarted;
    public event System.Action           OnNightfall;

    // ─── Called by GameManager at the top of each day ────────────────────────

    public void Init()
    {
        EventManager.Instance.OnEventTriggered += (_, __, ___) =>
        {
            _eventInProgress = true;
            Debug.Log("[DayManager] Time paused — event in progress.");
        };

        EventManager.Instance.OnEventResolved += _ =>
        {
            _eventInProgress = false;
            _lastEventTime   = DayProgress;
            Debug.Log($"[DayManager] Time resumed — cooldown resets at {DayProgress:F2}.");
        };

        Debug.Log("[DayManager] Init complete.");
    }
    
    /// <summary>
    /// Enters the briefing phase. Explorers are NOT sent out yet.
    /// UI should now show the explorer roster for the player to select from.
    /// </summary>
    public void BeginDay()
    {
        if (DayRunning || InBriefing) return;

        DayProgress    = 0f;
        PartyDepth     = 0f;
        RecallIssued   = false;
        IsReturning    = false;
        _lastEventTime = 0f;
        _selectedForDispatch.Clear();

        InBriefing = true;
        OnBriefingStarted?.Invoke();
        Debug.Log($"Briefing — Day {GameManager.Instance.CurrentDay}. Select explorers to dispatch.");
    }

    // ─── Called by UI (toggle buttons on each explorer card) ─────────────────

    public void ToggleExplorerSelection(Explorer explorer)
    {
        if (!InBriefing) return;
        // Accept InCamp and Lost explorers — Lost can only be rescued solo
        if (explorer.Status != ExplorerStatus.InCamp &&
            explorer.Status != ExplorerStatus.Lost) return;

        if (_selectedForDispatch.Contains(explorer))
        {
            _selectedForDispatch.Remove(explorer);
            Debug.Log($"{explorer.Name} Removed from dispatch");
            return;
        }

        
        
        if (explorer.Status == ExplorerStatus.Lost)
        {
            _selectedForDispatch.Clear();   // rescue must be solo
        }
        else // InCamp
        {
            // Remove any Lost explorer that may be in the set
            _selectedForDispatch.RemoveWhere(e => e.Status == ExplorerStatus.Lost);
        }

        _selectedForDispatch.Add(explorer);
        Debug.Log($"{explorer.Name} Selected for dispatch");
    }

    public bool IsSelected(Explorer explorer) => _selectedForDispatch.Contains(explorer);

    public int SelectedCount => _selectedForDispatch.Count;

    // ─── Called by UI Dispatch button ────────────────────────────────────────

    /// <summary>
    /// Sends selected explorers into the maze and starts the day loop.
    /// Requires at least one explorer selected.
    /// </summary>
    public void DispatchSelected()
    {
        if (!InBriefing) return;
        if (_selectedForDispatch.Count == 0)
        {
            Debug.LogWarning("No explorers selected — select at least one before dispatching.");
            return;
        }

        if (IsMixedSelection)
        {
            Debug.LogWarning("Cannot mix Lost and InCamp explorers in the same mission.");
            return;
        }

        // Hard guard: multiple Lost explorers can never be dispatched together
        int lostCount = 0;
        foreach (var e in _selectedForDispatch)
            if (e.Status == ExplorerStatus.Lost) lostCount++;
        if (lostCount > 1)
        {
            Debug.LogWarning("Only one Lost explorer can be rescued at a time.");
            return;
        }

        if (IsRescueMission)
        {
            foreach (var e in _selectedForDispatch)
                LaunchRescue(e);
            _selectedForDispatch.Clear();
            InBriefing = false;
            return;
        }

        // Normal dispatch

        InBriefing = false;

        // Find the slowest explorer's Speed — the party moves at their pace
        int minSpeed = int.MaxValue;
        foreach (var e in _selectedForDispatch)
        {
            GameManager.Instance.SetExplorerStatus(e, ExplorerStatus.Exploring);
            if (e.Speed < minSpeed) minSpeed = e.Speed;
        }

        // Maze pace in km/h: cautious exploration, gated by the slowest member.
        // Speed 1 → 0.75 km/h, Speed 5 → 1.75 km/h, Speed 9 → 2.75 km/h.
        // Over an 8-hour day, converted to metres.
        float paceKmh     = 0.5f + minSpeed * 0.25f;
        MaxDepthMeters     = paceKmh * 8f * 1000f;

        Debug.Log($"[DayManager] Party min speed: {minSpeed} → pace {paceKmh:F2} km/h → max depth {MaxDepthMeters:F0}m");

        _selectedForDispatch.Clear();

        OnDispatchStarted?.Invoke();
        Debug.Log($"Explorers dispatched! Day {GameManager.Instance.CurrentDay} underway.");

        _dayCoroutine = StartCoroutine(DayLoop());
    }

    // ─── Rescue ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Spends the whole day recovering one Lost explorer.
    /// The explorer returns to camp with -1 to every stat.
    /// No maze events fire; the day is consumed immediately.
    /// </summary>
    private void LaunchRescue(Explorer lost)
    {
        Debug.Log($"Rescue mission launched for {lost.Name}. Day consumed.");
        lost.ApplyRescuePenalty();
        GameManager.Instance.SetExplorerStatus(lost, ExplorerStatus.InCamp);
        GameManager.Instance.NotifyExplorerStatsChanged(lost);

        // Skip the day entirely — go straight to EndDay
        StartCoroutine(EndDayAfterDelay(1.5f));
    }
    
    // ─── Recall ───────────────────────────────────────────────────────────────

    public void IssueRecall()
    {
        if (!DayRunning || RecallIssued || IsReturning) return;

        RecallIssued = true;
        IsReturning  = true;

        foreach (var e in GameManager.Instance.AllExplorers)
            if (e.Status == ExplorerStatus.Exploring)
                GameManager.Instance.SetExplorerStatus(e, ExplorerStatus.Returning);

        OnReturnStarted?.Invoke();
        Debug.Log("Recall issued! Party turning back.");

        // DayLoop is still running and will handle the return leg naturally.
    }

    // ─── Day Loop ─────────────────────────────────────────────────────────────

    private IEnumerator DayLoop()
    {
        DayRunning = true;

        while (DayProgress < 1f)
        {
            yield return null;

            // ← Pause time while an event is being resolved
            if (IsPaused) continue;

            float tickSize = Time.deltaTime / dayDurationSeconds;
            DayProgress += tickSize;
            DayProgress  = Mathf.Clamp01(DayProgress);
            OnDayProgressUpdated?.Invoke(DayProgress);

            // ── Move party depth ───────────────────────────────────────────────
            if (IsReturning)
            {
                PartyDepth = Mathf.Max(PartyDepth - tickSize * returnSpeedMultiplier, 0f);

                if (PartyDepth <= 0f)
                {
                    // Whole party back — flip everyone to InCamp and exit early
                    foreach (var e in GameManager.Instance.AllExplorers)
                        if (e.Status == ExplorerStatus.Returning)
                            GameManager.Instance.SetExplorerStatus(e, ExplorerStatus.InCamp);

                    Debug.Log("Whole party back in camp!");
                    break;
                }
            }
            else
            {
                PartyDepth = Mathf.Min(PartyDepth + tickSize, 1f);
            }
            
            if (!RecallIssued && DayProgress >= recallWarningThreshold)
                if (DayProgress - tickSize < recallWarningThreshold)
                {
                    OnRecallWarning?.Invoke();
                    Debug.Log("Night is approaching!");
                }

            if (!IsReturning)
                TryFireEvent();
        }

        DayRunning = false;

        bool nightfell = DayProgress >= 1f;
        if (nightfell)
        {
            OnNightfall?.Invoke();
            Debug.Log("Nightfall!");
        }

        yield return new WaitForSeconds(nightfell ? 2f : 1f);

        GameManager.Instance.EndDay();
    }

    private IEnumerator EndDayAfterDelay(float delay)
    {
        DayRunning = false;
        yield return new WaitForSeconds(delay);
        GameManager.Instance.EndDay();
    }

    /// <summary>
    /// Called by EventManager to burn day progress (resting, delays, detours).
    /// Clamped so it can't push past nightfall on its own.
    /// </summary>
    public void ConsumeTime(float amount)
    {
        DayProgress = Mathf.Min(DayProgress + amount, 0.99f);
        OnDayProgressUpdated?.Invoke(DayProgress);

        if (IsReturning)
        {
            // A delay on the way back adds distance to retrace
            PartyDepth = Mathf.Min(PartyDepth + amount, 1f);
            Debug.Log($"[TIME] Return delayed — party depth now {PartyDepth:P0}");
        }
    }

    // ─── Event Firing ─────────────────────────────────────────────────────────

    private void TryFireEvent()
    {
        if (_eventInProgress)                                       return;
        if (DayProgress - _lastEventTime < minTimeBetweenEvents)    return;

        var exploring = GameManager.Instance.GetExplorersByStatus(ExplorerStatus.Exploring);
        if (exploring.Count == 0)                                   return;

        if (Random.value < eventChancePerTick)
        {
            _lastEventTime = DayProgress;
            EventManager.Instance.TriggerRandomEvent(exploring);
        }
    }
}