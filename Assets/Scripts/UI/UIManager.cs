// UIManager.cs
// Assets/Scripts/UI/UIManager.cs

using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Subscribe to game-level events
        DayManager.Instance.OnBriefingStarted += ShowBriefing;
        DayManager.Instance.OnDispatchStarted += ShowMaze;
        DayManager.Instance.OnNightfall       += () => { /* MazePanel stays up until EndDay */ };

        EventManager.Instance.OnEventTriggered += (ev, explorer, party) => ShowEvent();
        EventManager.Instance.OnEventResolved  += _ => HideEvent();

        GameManager.Instance.OnGameOver += ShowGameOver;
    }

    [Header("Panels")]
    [SerializeField] private GameObject briefingPanel;
    [SerializeField] private GameObject mazePanel;
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private GameObject gameOverPanel;
    
    void Start()
    {
        // Start on briefing
        ShowBriefing();
    }

    public void ShowBriefing()
    {
        briefingPanel.SetActive(true);
        mazePanel    .SetActive(false);
        eventPanel   .SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void ShowMaze()
    {
        briefingPanel.SetActive(false);
        mazePanel    .SetActive(true);
        eventPanel   .SetActive(false);
        gameOverPanel.SetActive(false);
    }

    public void ShowEvent()
    {
        // Maze stays behind — event overlays it
        eventPanel.SetActive(true);
    }

    public void HideEvent()
    {
        eventPanel.SetActive(false);
    }

    public void ShowGameOver(bool won)
    {
        briefingPanel.SetActive(false);
        mazePanel    .SetActive(false);
        eventPanel   .SetActive(false);
        gameOverPanel.SetActive(true);
    }
}