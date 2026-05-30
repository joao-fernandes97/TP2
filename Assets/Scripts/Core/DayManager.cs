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

    [Header("Event Timing")]
    [SerializeField] private float minTimeBetweenEvents = 0.10f;
    [SerializeField] private float eventChancePerTick   = 0.02f;

    public float DayProgress  { get; private set; } = 0f;
    public bool  DayRunning   { get; private set; } = false;
    public bool  InBriefing   { get; private set; } = false;
    public bool  RecallIssued { get; private set; } = false;

    // Explorers the player has toggled for today's run
    private readonly HashSet<Explorer> _selectedForDispatch = new();

    private float     _lastEventTime;
    public bool IsPaused => _eventInProgress;
    private bool _eventInProgress;
    private Coroutine _dayCoroutine;

    // Events
    public event System.Action<float>    OnDayProgressUpdated;
    public event System.Action           OnBriefingStarted;   // UI: show selection screen
    public event System.Action           OnDispatchStarted;   // UI: hide selection, show maze
    public event System.Action           OnRecallWarning;
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
        RecallIssued   = false;
        _lastEventTime = 0f;
        _selectedForDispatch.Clear();

        InBriefing = true;
        OnBriefingStarted?.Invoke();
        Debug.Log($"📋 Briefing — Day {GameManager.Instance.CurrentDay}. Select explorers to dispatch.");
    }

    // ─── Called by UI (toggle buttons on each explorer card) ─────────────────

    public void ToggleExplorerSelection(Explorer explorer)
    {
        if (!InBriefing) return;
        if (explorer.Status != ExplorerStatus.InCamp) return;

        if (_selectedForDispatch.Contains(explorer))
            _selectedForDispatch.Remove(explorer);
        else
            _selectedForDispatch.Add(explorer);

        Debug.Log($"  {(IsSelected(explorer) ? "✅" : "⬜")} {explorer.Name} selected for dispatch");
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

        InBriefing = false;

        foreach (var e in _selectedForDispatch)
            GameManager.Instance.SetExplorerStatus(e, ExplorerStatus.Exploring);

        _selectedForDispatch.Clear();

        OnDispatchStarted?.Invoke();
        Debug.Log($"☀️  Explorers dispatched! Day {GameManager.Instance.CurrentDay} underway.");

        _dayCoroutine = StartCoroutine(DayLoop());
    }

    // ─── Recall ───────────────────────────────────────────────────────────────

    public void IssueRecall()
    {
        if (!DayRunning || RecallIssued) return;

        RecallIssued = true;
        Debug.Log("📣 Recall issued!");

        foreach (var e in GameManager.Instance.AllExplorers)
            if (e.Status == ExplorerStatus.Exploring)
                GameManager.Instance.SetExplorerStatus(e, ExplorerStatus.InCamp);

        StopCoroutine(_dayCoroutine);
        StartCoroutine(EndDayAfterDelay(1.5f));
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

            DayProgress += Time.deltaTime / dayDurationSeconds;
            DayProgress  = Mathf.Clamp01(DayProgress);
            OnDayProgressUpdated?.Invoke(DayProgress);

            if (!RecallIssued && DayProgress >= recallWarningThreshold)
                if (DayProgress - Time.deltaTime / dayDurationSeconds < recallWarningThreshold)
                {
                    OnRecallWarning?.Invoke();
                    Debug.Log("⚠️  Night is approaching!");
                }

            TryFireEvent();
        }

        DayRunning = false;
        OnNightfall?.Invoke();
        Debug.Log("🌑 Nightfall!");

        yield return new WaitForSeconds(2f);
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
    }

    // ─── Event Firing ─────────────────────────────────────────────────────────

    private void TryFireEvent()
    {
        // Double guard — belt and suspenders
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