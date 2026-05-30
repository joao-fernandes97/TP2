// GameManager.cs
// Place in: Assets/Scripts/Core/GameManager.cs

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Game Config (tweak in Inspector) ────────────────────────────────────
    [Header("Win / Loss Conditions")]
    [SerializeField] private int intelToWin = 100;
    [SerializeField] private int maxDays    = 15;

    [Header("Starting Conditions")]
    [SerializeField] private int startingExplorers = 4;

    // ─── Game State ───────────────────────────────────────────────────────────
    public int  CurrentDay      { get; private set; } = 1;
    public int  IntelCollected  { get; private set; } = 0;
    public bool GameOver        { get; private set; } = false;
    public bool PlayerWon       { get; private set; } = false;

    public int IntelToWin  => intelToWin;
    public int MaxDays     => maxDays;

    // Explorer roster — other systems read this list
    public List<Explorer> AllExplorers { get; private set; } = new();

    // ─── Events (UI and other systems subscribe to these) ────────────────────
    public event Action<int>      OnIntelChanged;    // new total
    public event Action<int>      OnDayChanged;      // new day number
    public event Action<Explorer> OnExplorerStatusChanged;
    public event Action<Explorer> OnExplorerStatsChanged;
    public event Action<bool>     OnGameOver;        // true = win, false = loss

    // ─── Init ─────────────────────────────────────────────────────────────────
    IEnumerator Start()
    {
        // Generate starting explorers
        for (int i = 0; i < startingExplorers; i++)
            AllExplorers.Add(ExplorerGenerator.Generate());

        yield return null;

        // Init systems that depend on other singletons
        DayManager.Instance.Init();
        
        // Enter briefing for day 1 — player picks who to send
        DayManager.Instance?.BeginDay();

        // Future: notify UI to refresh roster here
    }

    // ─── Intel ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when explorers return to camp. Adds their carried intel to the total.
    /// </summary>
    public void BankIntel(int amount)
    {
        if (amount <= 0) return;
        IntelCollected = Mathf.Min(IntelCollected + amount, intelToWin);
        OnIntelChanged?.Invoke(IntelCollected);

        if (IntelCollected >= intelToWin)
            TriggerGameOver(won: true);
    }

    // ─── Explorer Status ──────────────────────────────────────────────────────

    public void SetExplorerStatus(Explorer explorer, ExplorerStatus newStatus)
    {
        if (explorer == null) return;
        explorer.Status = newStatus;
        OnExplorerStatusChanged?.Invoke(explorer);
        CheckLossCondition();
    }

    public void NotifyExplorerStatsChanged(Explorer explorer)
    {
        OnExplorerStatsChanged?.Invoke(explorer);
    }

    // ─── Day Cycle ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by DayManager when a new day begins.
    /// Resets per-day state on all explorers who are alive and in camp.
    /// </summary>
    public void StartNewDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);

        foreach (var e in AllExplorers)
        {
            if (e.Status == ExplorerStatus.InCamp)
                e.ResetForNewDay();
        }

        if (CurrentDay > maxDays)
        {
            TriggerGameOver(won: false);
            return;
        }

        // Signal DayManager to start the day loop (DayManager listens to this or is called directly)
        DayManager.Instance?.BeginDay();
    }

    /// <summary>
    /// Called by DayManager at nightfall or when all explorers are resolved.
    /// Banks intel from returned explorers and advances the day.
    /// </summary>
    public void EndDay()
    {
        // Bank intel from all explorers who made it back in time
        foreach (var e in AllExplorers)
        {
            if (e.Status == ExplorerStatus.InCamp && e.CarriedIntel > 0)
            {
                BankIntel(e.CarriedIntel);
                e.CarriedIntel = 0;
            }

            // Explorers still Exploring at nightfall become Lost
            if (e.Status == ExplorerStatus.Exploring)
                SetExplorerStatus(e, ExplorerStatus.Lost);

            // Was returning but night fell before they reached camp — also lost
            if (e.Status == ExplorerStatus.Returning)
            {
                Debug.Log($"{e.Name} was still on the way back when night fell — Lost!");
                SetExplorerStatus(e, ExplorerStatus.Lost);
            }
        }

        if (!GameOver)
            StartNewDay();
    }

    // ─── Win / Loss ───────────────────────────────────────────────────────────

    private void CheckLossCondition()
    {
        bool anyAlive = AllExplorers.Exists(e =>
            e.Status == ExplorerStatus.InCamp ||
            e.Status == ExplorerStatus.Exploring ||
            e.Status == ExplorerStatus.Returning);

        if (!anyAlive)
            TriggerGameOver(won: false);
    }

    private void TriggerGameOver(bool won)
    {
        if (GameOver) return;
        GameOver  = true;
        PlayerWon = won;
        OnGameOver?.Invoke(won);
        Debug.Log(won ? "You Win!" : "Game Over.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    public List<Explorer> GetExplorersByStatus(ExplorerStatus status) =>
        AllExplorers.FindAll(e => e.Status == status);

    public float IntelProgress => (float)IntelCollected / intelToWin; // 0–1, useful for UI slider
}